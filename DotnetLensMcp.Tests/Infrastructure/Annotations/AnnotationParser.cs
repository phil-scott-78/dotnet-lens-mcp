using System.Text.RegularExpressions;

namespace DotnetLensMcp.Tests.Infrastructure.Annotations
{
    /// <summary>
    /// Parses @test:type annotations from C# source files
    /// </summary>
    public partial class AnnotationParser(AnnotationParseOptions? options = null)
    {
        private readonly AnnotationParseOptions _options = options ?? new AnnotationParseOptions();

        // Regex to match @test:type annotations with key="value" pairs
        private static readonly Regex AnnotationRegex = AnnotationRegexDefinition();

        // Regex to extract key="value" pairs from annotation
        private static readonly Regex AttributeRegex = AttributeRegexDefinition();

        // Regex to detect if we're inside a string literal
        private static readonly Regex StringLiteralRegex = StringLiteralRegexDefinition();

        /// <summary>
        /// Parses annotations from a single source file
        /// </summary>
        public async Task<AnnotationParseResult> ParseFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Source file not found: {filePath}");
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            return ParseLines(lines, filePath);
        }

        /// <summary>
        /// Parses annotations from source text
        /// </summary>
        public AnnotationParseResult ParseText(string sourceText, string filePath = "")
        {
            var lines = sourceText.ReplaceLineEndings("\n").Split('\n');
            return ParseLines(lines, filePath);
        }

        /// <summary>
        /// Parses annotations from an array of source lines
        /// </summary>
        private AnnotationParseResult ParseLines(string[] lines, string filePath = "")
        {
            var annotations = new List<TypeTestAnnotation>();
            var errors = new List<AnnotationParseError>();

            for (int i = 0; i < lines.Length; i++)
            {
                var lineNumber = i + 1;
                var line = lines[i];

                try
                {
                    var annotation = ParseLine(line, lineNumber, filePath);
                    if (annotation != null)
                    {
                        if (_options.IncludeInvalidAnnotations || annotation.IsValid)
                        {
                            annotations.Add(annotation);
                        }
                        else if (!annotation.IsValid)
                        {
                            errors.Add(new AnnotationParseError
                            {
                                LineNumber = lineNumber,
                                SourceLine = line,
                                Message = "Invalid annotation: missing required properties",
                                RawAnnotation = ExtractRawAnnotation(line)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new AnnotationParseError
                    {
                        LineNumber = lineNumber,
                        SourceLine = line,
                        Message = $"Parse error: {ex.Message}",
                        RawAnnotation = ExtractRawAnnotation(line)
                    });
                }

                if (errors.Count >= _options.MaxErrors)
                {
                    break;
                }
            }

            return new AnnotationParseResult
            {
                Annotations = annotations,
                Errors = errors
            };
        }

        /// <summary>
        /// Parses a single line for annotations
        /// </summary>
        private TypeTestAnnotation? ParseLine(string line, int lineNumber, string filePath)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            // Skip if annotation is inside a string literal and option is enabled
            if (_options.IgnoreInStringLiterals && IsInsideStringLiteral(line))
                return null;

            var match = AnnotationRegex.Match(line);
            if (!match.Success)
                return null;

            var attributeText = match.Groups[1].Value;
            var attributes = ParseAttributes(attributeText);

            return new TypeTestAnnotation
            {
                Target = GetAttribute(attributes, "target") ?? "",
                Expect = GetAttribute(attributes, "expect") ?? "",
                Kind = GetAttribute(attributes, "kind") ?? "",
                Generic = ParseBoolAttribute(attributes, "generic"),
                Args = ParseArrayAttribute(attributes, "args"),
                Occurrence = ParseIntAttribute(attributes, "occurrence") ?? 1,
                TestName = GetAttribute(attributes, "name"),
                FilePath = filePath,
                LineNumber = lineNumber,
                SourceLine = line
            };
        }

        /// <summary>
        /// Parses key="value" attributes from annotation text
        /// </summary>
        private Dictionary<string, string> ParseAttributes(string attributeText)
        {
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var matches = AttributeRegex.Matches(attributeText);

            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value;
                var value = UnescapeValue(match.Groups[2].Value);
                attributes[key] = value;
            }

            return attributes;
        }

        /// <summary>
        /// Unescapes special characters in attribute values
        /// </summary>
        private string UnescapeValue(string value)
        {
            return value
                .Replace("\\\"", "\"")
                .Replace(@"\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }

        /// <summary>
        /// Gets a string attribute value
        /// </summary>
        private string? GetAttribute(Dictionary<string, string> attributes, string key)
        {
            return attributes.GetValueOrDefault(key);
        }

        /// <summary>
        /// Parses a boolean attribute value
        /// </summary>
        private bool? ParseBoolAttribute(Dictionary<string, string> attributes, string key)
        {
            var value = GetAttribute(attributes, key);
            if (value == null) return null;

            return bool.TryParse(value, out var result) ? result : null;
        }

        /// <summary>
        /// Parses an integer attribute value
        /// </summary>
        private int? ParseIntAttribute(Dictionary<string, string> attributes, string key)
        {
            var value = GetAttribute(attributes, key);
            if (value == null) return null;

            return int.TryParse(value, out var result) ? result : null;
        }

        /// <summary>
        /// Parses a comma-separated array attribute value
        /// </summary>
        private string[]? ParseArrayAttribute(Dictionary<string, string> attributes, string key)
        {
            var value = GetAttribute(attributes, key);

            return value?.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }

        /// <summary>
        /// Checks if the annotation appears to be inside a string literal
        /// </summary>
        private bool IsInsideStringLiteral(string line)
        {
            var annotationIndex = line.IndexOf(_options.AnnotationPrefix, StringComparison.OrdinalIgnoreCase);
            if (annotationIndex == -1) return false;

            // Find all string literals in the line
            var matches = StringLiteralRegex.Matches(line);
            foreach (Match match in matches)
            {
                if (annotationIndex >= match.Index && annotationIndex < match.Index + match.Length)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Extracts the raw annotation text for error reporting
        /// </summary>
        private static string? ExtractRawAnnotation(string line)
        {
            var match = AnnotationRegex.Match(line);
            return match.Success ? match.Value : null;
        }

        [GeneratedRegex("""
                        //\s*@test:type\s+(.+)$
                        """, RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
        private static partial Regex AnnotationRegexDefinition();
        
        [GeneratedRegex("""
                        (\w+)="([^"\\]*(?:\\.[^"\\]*)*)"
                        """, RegexOptions.Compiled)]
        private static partial Regex AttributeRegexDefinition();
        
        [GeneratedRegex("""
                        "[^"\\]*(?:\\.[^"\\]*)*"
                        """, RegexOptions.Compiled)]
        private static partial Regex StringLiteralRegexDefinition();
    }
}