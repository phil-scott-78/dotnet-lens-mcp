namespace DotnetLensMcp.Tests.Infrastructure.Annotations
{
    /// <summary>
    /// Resolves target text in source files to precise line/column positions
    /// </summary>
    public class TextPositionResolver
    {
        private readonly Dictionary<string, string[]> _fileLineCache = new();

        /// <summary>
        /// Resolves the position of target text within a source file
        /// </summary>
        public async Task<PositionInfo?> ResolvePositionAsync(string filePath, string targetText, int lineNumber, int occurrence = 1)
        {
            var lines = await GetFileLinesAsync(filePath);
            if (lineNumber <= 0 || lineNumber > lines.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(lineNumber), $"Line number {lineNumber} is out of range for file {filePath}");
            }

            var line = lines[lineNumber - 1]; // Convert to 0-based index
            var column = FindTargetColumn(line, targetText, occurrence);
            
            return column > 0 ? new PositionInfo(lineNumber, column) : null;
        }

        /// <summary>
        /// Resolves the position of target text for a type test annotation
        /// </summary>
        public async Task<PositionInfo?> ResolveAnnotationPositionAsync(TypeTestAnnotation annotation)
        {
            if (!annotation.IsValid)
            {
                throw new ArgumentException("Annotation is not valid", nameof(annotation));
            }

            return await ResolvePositionAsync(
                annotation.FilePath,
                annotation.Target,
                annotation.LineNumber,
                annotation.Occurrence
            );
        }

        /// <summary>
        /// Resolves positions for multiple annotations in batch
        /// </summary>
        public async Task<Dictionary<TypeTestAnnotation, PositionInfo?>> ResolvePositionsAsync(IEnumerable<TypeTestAnnotation> annotations)
        {
            var results = new Dictionary<TypeTestAnnotation, PositionInfo?>();

            // Group by file to optimize file reading
            var groupedByFile = annotations.GroupBy(a => a.FilePath);

            foreach (var fileGroup in groupedByFile)
            {
                var filePath = fileGroup.Key;
                if (string.IsNullOrEmpty(filePath)) continue;

                try
                {
                    var lines = await GetFileLinesAsync(filePath);

                    foreach (var annotation in fileGroup)
                    {
                        try
                        {
                            if (annotation.LineNumber > 0 && annotation.LineNumber <= lines.Length)
                            {
                                var line = lines[annotation.LineNumber - 1];
                                var column = FindTargetColumn(line, annotation.Target, annotation.Occurrence);
                                results[annotation] = column > 0 ? new PositionInfo(annotation.LineNumber, column) : null;
                            }
                            else
                            {
                                results[annotation] = null;
                            }
                        }
                        catch (Exception)
                        {
                            results[annotation] = null;
                        }
                    }
                }
                catch (Exception)
                {
                    // If we can't read the file, mark all annotations as failed
                    foreach (var annotation in fileGroup)
                    {
                        results[annotation] = null;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Finds the column position of target text within a source line
        /// </summary>
        public int FindTargetColumn(string sourceLine, string targetText, int occurrence = 1)
        {
            if (string.IsNullOrEmpty(sourceLine) || string.IsNullOrEmpty(targetText))
                return -1;

            if (occurrence <= 0)
                return -1;

            int currentOccurrence = 0;
            int searchIndex = 0;

            while (searchIndex < sourceLine.Length)
            {
                int foundIndex = sourceLine.IndexOf(targetText, searchIndex, StringComparison.Ordinal);
                if (foundIndex == -1)
                    break;

                // Check if this is a word boundary match (not part of a larger identifier)
                if (IsWordBoundaryMatch(sourceLine, targetText, foundIndex))
                {
                    currentOccurrence++;
                    if (currentOccurrence == occurrence)
                    {
                        // Return 1-based column position
                        return foundIndex + 1;
                    }
                }

                searchIndex = foundIndex + 1;
            }

            return -1;
        }

        /// <summary>
        /// Checks if the found text is at a word boundary (not part of a larger identifier)
        /// </summary>
        private bool IsWordBoundaryMatch(string sourceLine, string targetText, int foundIndex)
        {
            // Check character before
            if (foundIndex > 0)
            {
                char charBefore = sourceLine[foundIndex - 1];
                if (IsIdentifierChar(charBefore))
                    return false;
            }

            // Check character after
            int endIndex = foundIndex + targetText.Length;
            if (endIndex < sourceLine.Length)
            {
                char charAfter = sourceLine[endIndex];
                if (IsIdentifierChar(charAfter))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a character can be part of a C# identifier
        /// </summary>
        private bool IsIdentifierChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        /// <summary>
        /// Gets the lines of a file, using cache if available
        /// </summary>
        private async Task<string[]> GetFileLinesAsync(string filePath)
        {
            if (_fileLineCache.TryGetValue(filePath, out var cachedLines))
            {
                return cachedLines;
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Source file not found: {filePath}");
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            _fileLineCache[filePath] = lines;
            return lines;
        }

        /// <summary>
        /// Clears the file cache
        /// </summary>
        public void ClearCache()
        {
            _fileLineCache.Clear();
        }

        /// <summary>
        /// Validates that a target can be found in the specified line
        /// </summary>
        public async Task<bool> ValidateTargetAsync(string filePath, string targetText, int lineNumber, int occurrence = 1)
        {
            try
            {
                var position = await ResolvePositionAsync(filePath, targetText, lineNumber, occurrence);
                return position != null && position.IsValid;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Finds all occurrences of target text in a source line
        /// </summary>
        public List<int> FindAllOccurrences(string sourceLine, string targetText)
        {
            var positions = new List<int>();
            
            if (string.IsNullOrEmpty(sourceLine) || string.IsNullOrEmpty(targetText))
                return positions;

            int searchIndex = 0;
            while (searchIndex < sourceLine.Length)
            {
                int foundIndex = sourceLine.IndexOf(targetText, searchIndex, StringComparison.Ordinal);
                if (foundIndex == -1)
                    break;

                if (IsWordBoundaryMatch(sourceLine, targetText, foundIndex))
                {
                    positions.Add(foundIndex + 1); // Convert to 1-based
                }

                searchIndex = foundIndex + 1;
            }

            return positions;
        }

        /// <summary>
        /// Gets diagnostic information about target resolution
        /// </summary>
        public async Task<TargetResolutionDiagnostic> GetDiagnosticAsync(string filePath, string targetText, int lineNumber, int occurrence = 1)
        {
            var diagnostic = new TargetResolutionDiagnostic
            {
                FilePath = filePath,
                TargetText = targetText,
                LineNumber = lineNumber,
                RequestedOccurrence = occurrence
            };

            try
            {
                var lines = await GetFileLinesAsync(filePath);
                if (lineNumber > 0 && lineNumber <= lines.Length)
                {
                    var sourceLine = lines[lineNumber - 1];
                    diagnostic.SourceLine = sourceLine;
                    diagnostic.AllOccurrences = FindAllOccurrences(sourceLine, targetText);
                    diagnostic.ResolvedColumn = FindTargetColumn(sourceLine, targetText, occurrence);
                    diagnostic.IsValid = diagnostic.ResolvedColumn > 0;
                }
                else
                {
                    diagnostic.Error = $"Line number {lineNumber} is out of range (file has {lines.Length} lines)";
                }
            }
            catch (Exception ex)
            {
                diagnostic.Error = ex.Message;
            }

            return diagnostic;
        }
    }

    /// <summary>
    /// Diagnostic information about target text resolution
    /// </summary>
    public class TargetResolutionDiagnostic
    {
        public string FilePath { get; set; } = "";
        public string TargetText { get; set; } = "";
        public int LineNumber { get; set; }
        public int RequestedOccurrence { get; set; }
        public string SourceLine { get; set; } = "";
        public List<int> AllOccurrences { get; set; } = new();
        public int ResolvedColumn { get; set; } = -1;
        public bool IsValid { get; set; }
        public string? Error { get; set; }

        public string Summary => IsValid 
            ? $"Found '{TargetText}' at column {ResolvedColumn} (occurrence {RequestedOccurrence} of {AllOccurrences.Count})"
            : $"Failed to find '{TargetText}'" + (Error != null ? $": {Error}" : "");
    }
}