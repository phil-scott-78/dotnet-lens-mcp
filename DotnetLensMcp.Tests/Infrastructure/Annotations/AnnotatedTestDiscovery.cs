namespace DotnetLensMcp.Tests.Infrastructure.Annotations
{
    /// <summary>
    /// Discovers and generates test cases from annotated source files
    /// </summary>
    public class AnnotatedTestDiscovery
    {
        private readonly AnnotationParser _parser;
        private readonly TextPositionResolver _positionResolver;

        public AnnotatedTestDiscovery(
            AnnotationParser? parser = null,
            TextPositionResolver? positionResolver = null)
        {
            _parser = parser ?? new AnnotationParser();
            _positionResolver = positionResolver ?? new TextPositionResolver();
        }

        /// <summary>
        /// Discovers all type test cases from the playground directory
        /// </summary>
        public async Task<IReadOnlyList<TypeTestCase>> DiscoverTestCasesAsync(string playgroundBasePath)
        {
            if (!Directory.Exists(playgroundBasePath))
            {
                throw new DirectoryNotFoundException($"Playground directory not found: {playgroundBasePath}");
            }

            var testCases = new List<TypeTestCase>();
            var sourceFiles = FindSourceFiles(playgroundBasePath);

            foreach (var filePath in sourceFiles)
            {
                try
                {
                    var fileCases = await DiscoverTestCasesFromFileAsync(filePath);
                    testCases.AddRange(fileCases);
                }
                catch (Exception ex)
                {
                    // ignored
                }
            }

            return testCases;
        }

        /// <summary>
        /// Discovers test cases from a specific source file
        /// </summary>
        private async Task<IReadOnlyList<TypeTestCase>> DiscoverTestCasesFromFileAsync(string filePath)
        {
            var parseResult = await _parser.ParseFileAsync(filePath);
            
            if (!parseResult.IsSuccess)
            {
                // Log parsing errors
                foreach (var error in parseResult.Errors)
                {
                    Console.WriteLine($"Warning: Parse error in {filePath}: {error.FormattedMessage}");
                }
            }

            var validAnnotations = parseResult.Annotations.Where(a => a.IsValid).ToList();
            if (validAnnotations.Count == 0)
            {
                return new List<TypeTestCase>();
            }

            // Resolve positions for all annotations in batch
            var positions = await _positionResolver.ResolvePositionsAsync(validAnnotations);

            var testCases = new List<TypeTestCase>();
            foreach (var annotation in validAnnotations)
            {
                if (positions.TryGetValue(annotation, out var position) && position != null)
                {
                    testCases.Add(new TypeTestCase
                    {
                        Annotation = annotation,
                        Position = position
                    });
                }
            }

            return testCases;
        }

        /// <summary>
        /// Finds all C# source files in the playground directory
        /// </summary>
        private IEnumerable<string> FindSourceFiles(string basePath)
        {
            return Directory.EnumerateFiles(basePath, "*.cs", SearchOption.AllDirectories)
                .Where(file => !IsExcludedFile(file))
                .OrderBy(file => file);
        }

        /// <summary>
        /// Checks if a file should be excluded from discovery
        /// </summary>
        private static bool IsExcludedFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            
            // Exclude common non-source files
            var excludedPatterns = new[]
            {
                "AssemblyInfo.cs",
                "GlobalAssemblyInfo.cs",
                ".Designer.cs",
                ".generated.cs",
                "TemporaryGeneratedFile_"
            };

            return excludedPatterns.Any(pattern => 
                fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates all discovered test cases
        /// </summary>
        public async Task<TestDiscoveryValidationResult> ValidateDiscoveryAsync(string playgroundBasePath)
        {
            var result = new TestDiscoveryValidationResult();
            
            try
            {
                var testCases = await DiscoverTestCasesAsync(playgroundBasePath);
                result.TotalTestCases = testCases.Count;
                result.ValidTestCases = testCases.Count(tc => tc.Position.IsValid);
                result.TestCases = testCases;

                // Check for duplicates
                var duplicateGroups = testCases
                    .GroupBy(tc => $"{tc.Annotation.FilePath}:{tc.Position.Line}:{tc.Position.Column}")
                    .Where(g => g.Count() > 1)
                    .ToList();

                if (duplicateGroups.Any())
                {
                    result.Warnings.AddRange(duplicateGroups.Select(g =>
                        $"Duplicate test cases found at {g.Key}: {string.Join(", ", g.Select(tc => tc.Annotation.Target))}"));
                }

                // Check for missing required test scenarios
                var missingScenarios = FindMissingTestScenarios(testCases);
                result.Warnings.AddRange(missingScenarios.Select(scenario => 
                    $"Missing test scenario: {scenario}"));

                result.IsValid = result.ValidTestCases == result.TotalTestCases && !result.Errors.Any();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Discovery failed: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        /// <summary>
        /// Identifies missing test scenarios that should be covered
        /// </summary>
        private static List<string> FindMissingTestScenarios(IReadOnlyList<TypeTestCase> testCases)
        {
            var missing = new List<string>();
            var coveredKinds = testCases.Select(tc => tc.Annotation.Kind).Distinct().ToHashSet();
            
            // Expected symbol kinds that should be tested
            var expectedKinds = new[]
            {
                "Local", "NamedType", "Property", "Parameter", "Method", 
                "Field", "TypeParameter", "ArrayType"
            };

            foreach (var expectedKind in expectedKinds)
            {
                if (!coveredKinds.Contains(expectedKind))
                {
                    missing.Add($"Symbol kind '{expectedKind}' not covered");
                }
            }

            // Check for generic types coverage
            var hasGenericTests = testCases.Any(tc => tc.Annotation.Generic == true);
            if (!hasGenericTests)
            {
                missing.Add("Generic types not covered");
            }

            return missing;
        }

        /// <summary>
        /// Gets statistics about discovered test cases
        /// </summary>
        public async Task<TestDiscoveryStatistics> GetStatisticsAsync(string playgroundBasePath)
        {
            var testCases = await DiscoverTestCasesAsync(playgroundBasePath);
            
            return new TestDiscoveryStatistics
            {
                TotalTestCases = testCases.Count,
                FilesCovered = testCases.Select(tc => tc.Annotation.FilePath).Distinct().Count(),
                KindsCovered = testCases.Select(tc => tc.Annotation.Kind).Distinct().ToList(),
                GenericTypesCount = testCases.Count(tc => tc.Annotation.Generic == true),
                TestCasesByFile = testCases.GroupBy(tc => Path.GetFileName(tc.Annotation.FilePath))
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        /// <summary>
        /// Creates test data for xUnit MemberData attributes
        /// </summary>
        public async Task<IEnumerable<object[]>> GetXUnitTestDataAsync(string playgroundBasePath)
        {
            var testCases = await DiscoverTestCasesAsync(playgroundBasePath);
            return testCases.Select(tc => tc.ToTestData());
        }
    }

    /// <summary>
    /// Results of test discovery validation
    /// </summary>
    public class TestDiscoveryValidationResult
    {
        public bool IsValid { get; set; }
        public int TotalTestCases { get; set; }
        public int ValidTestCases { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public IReadOnlyList<TypeTestCase> TestCases { get; set; } = new List<TypeTestCase>();

        public string Summary => IsValid 
            ? $"✓ Discovery successful: {ValidTestCases}/{TotalTestCases} valid test cases"
            : $"✗ Discovery failed: {Errors.Count} errors, {Warnings.Count} warnings";
    }

    /// <summary>
    /// Statistics about discovered test cases
    /// </summary>
    public class TestDiscoveryStatistics
    {
        public int TotalTestCases { get; set; }
        public int FilesCovered { get; set; }
        public List<string> KindsCovered { get; set; } = new();
        public int GenericTypesCount { get; set; }
        public Dictionary<string, int> TestCasesByFile { get; set; } = new();

        public override string ToString()
        {
            return $"Test Cases: {TotalTestCases}, Files: {FilesCovered}, Kinds: [{string.Join(", ", KindsCovered)}], Generics: {GenericTypesCount}";
        }
    }
}