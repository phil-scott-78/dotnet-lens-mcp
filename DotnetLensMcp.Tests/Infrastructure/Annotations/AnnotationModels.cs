namespace DotnetLensMcp.Tests.Infrastructure.Annotations
{
    /// <summary>
    /// Represents a parsed type test annotation from source code
    /// </summary>
    public record TypeTestAnnotation
    {
        /// <summary>
        /// The target identifier/text to locate on the line
        /// </summary>
        public string Target { get; init; } = "";

        /// <summary>
        /// Expected FullTypeName value from GetTypeAtPositionAsync
        /// </summary>
        public string Expect { get; init; } = "";

        /// <summary>
        /// Expected Kind value (Local, NamedType, Property, Parameter, etc.)
        /// </summary>
        public string Kind { get; init; } = "";

        /// <summary>
        /// Whether the type is generic (optional)
        /// </summary>
        public bool? Generic { get; init; }

        /// <summary>
        /// Type arguments for generic types (optional)
        /// </summary>
        public string[]? Args { get; init; }

        /// <summary>
        /// Which occurrence of the target text to use (default: 1)
        /// </summary>
        public int Occurrence { get; init; } = 1;

        /// <summary>
        /// Full path to the source file containing this annotation
        /// </summary>
        public string FilePath { get; init; } = "";

        /// <summary>
        /// Line number (1-based) where the annotation was found
        /// </summary>
        public int LineNumber { get; init; }

        /// <summary>
        /// The complete source line containing the annotation
        /// </summary>
        public string SourceLine { get; init; } = "";

        /// <summary>
        /// Optional test name/description for better test identification
        /// </summary>
        public string? TestName { get; init; }

        /// <summary>
        /// Validates that the annotation has all required properties
        /// </summary>
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Target) &&
            !string.IsNullOrWhiteSpace(Expect) &&
            !string.IsNullOrWhiteSpace(Kind) &&
            !string.IsNullOrWhiteSpace(FilePath) &&
            LineNumber > 0 &&
            Occurrence > 0;

        /// <summary>
        /// Gets a display name for test identification
        /// </summary>
        public string DisplayName
        {
            get
            {
                var fileName = Path.GetFileName(FilePath);
                var testId = TestName ?? $"{Target}_{Kind}";
                return $"{fileName}:{LineNumber} - {testId}";
            }
        }
    }

    /// <summary>
    /// Represents a resolved position in source code
    /// </summary>
    public record PositionInfo(int Line, int Column)
    {
        /// <summary>
        /// Validates that the position is valid (positive values)
        /// </summary>
        public bool IsValid => Line > 0 && Column > 0;
    }

    /// <summary>
    /// Contains the results of parsing annotations from a source file
    /// </summary>
    public record AnnotationParseResult
    {
        /// <summary>
        /// Successfully parsed annotations
        /// </summary>
        public IReadOnlyList<TypeTestAnnotation> Annotations { get; init; } = new List<TypeTestAnnotation>();

        /// <summary>
        /// Parsing errors encountered
        /// </summary>
        public IReadOnlyList<AnnotationParseError> Errors { get; init; } = new List<AnnotationParseError>();

        /// <summary>
        /// Whether the parsing was completely successful
        /// </summary>
        public bool IsSuccess => Errors.Count == 0;

        /// <summary>
        /// Number of valid annotations found
        /// </summary>
        public int ValidAnnotationCount => Annotations.Count(a => a.IsValid);
    }

    /// <summary>
    /// Represents an error that occurred while parsing annotations
    /// </summary>
    public record AnnotationParseError
    {
        /// <summary>
        /// The line number where the error occurred
        /// </summary>
        public int LineNumber { get; init; }

        /// <summary>
        /// The source line that caused the error
        /// </summary>
        public string SourceLine { get; init; } = "";

        /// <summary>
        /// Description of the parsing error
        /// </summary>
        public string Message { get; init; } = "";

        /// <summary>
        /// The raw annotation text that failed to parse
        /// </summary>
        public string? RawAnnotation { get; init; }

        /// <summary>
        /// Gets a formatted error message for display
        /// </summary>
        public string FormattedMessage => $"Line {LineNumber}: {Message}";
    }

    /// <summary>
    /// Configuration options for annotation parsing
    /// </summary>
    public record AnnotationParseOptions
    {
        /// <summary>
        /// The annotation prefix to look for (default: "@test:type")
        /// </summary>
        public string AnnotationPrefix { get; init; } = "@test:type";

        /// <summary>
        /// Whether to ignore annotations inside string literals
        /// </summary>
        public bool IgnoreInStringLiterals { get; init; } = true;

        /// <summary>
        /// Whether to ignore annotations inside multi-line comments
        /// </summary>
        public bool IgnoreInComments { get; init; } = false;

        /// <summary>
        /// Maximum number of errors to collect before stopping parsing
        /// </summary>
        public int MaxErrors { get; init; } = 100;

        /// <summary>
        /// Whether to include invalid annotations in the result
        /// </summary>
        public bool IncludeInvalidAnnotations { get; init; } = false;
    }

    /// <summary>
    /// Test case data for xUnit theory tests
    /// </summary>
    public record TypeTestCase
    {
        /// <summary>
        /// The annotation that defines this test case
        /// </summary>
        public TypeTestAnnotation Annotation { get; init; } = null!;

        /// <summary>
        /// The resolved position in the source file
        /// </summary>
        public PositionInfo Position { get; init; } = null!;

        /// <summary>
        /// Display name for the test
        /// </summary>
        public string DisplayName => Annotation.DisplayName;

        /// <summary>
        /// Gets the test case as an object array for xUnit MemberData
        /// </summary>
        public object[] ToTestData() => new object[] { this };
    }
}