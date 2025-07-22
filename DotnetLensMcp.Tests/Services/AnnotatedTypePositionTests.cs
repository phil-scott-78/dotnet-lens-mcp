using DotnetLensMcp.Tests.Fixtures;
using DotnetLensMcp.Tests.Infrastructure.Annotations;
using Shouldly;

// using Xunit.Abstractions; // Not needed for basic tests

namespace DotnetLensMcp.Tests.Services
{
    /// <summary>
    /// Annotation-based tests for GetTypeAtPosition using dynamically discovered test cases
    /// </summary>
    [Collection("RoslynService Collection")]
    public class AnnotatedTypePositionTests : IAsyncLifetime
    {
        private readonly RoslynServiceFixture _fixture;
        private readonly string _playgroundBasePath;
        private readonly AnnotatedTestDiscovery _testDiscovery;
        public AnnotatedTypePositionTests(RoslynServiceFixture fixture)
        {
            _fixture = fixture;
            _playgroundBasePath = Path.GetFullPath(Path.Combine(TestHelpers.GetProjectDirectory(), "DotnetLensMcp.Playground"));
            _testDiscovery = new AnnotatedTestDiscovery();
        }

        public async ValueTask InitializeAsync()
        {
            // Initialize workspace with the solution
            await _fixture.RoslynService.InitializeWorkspaceAsync(TestHelpers.GetProjectDirectory());
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        /// <summary>
        /// Gets all annotation-based test cases from playground files
        /// </summary>
        public static async Task<IEnumerable<object[]>> GetAnnotationTestCases()
        {
            var playgroundPath = Path.GetFullPath(Path.Combine(TestHelpers.GetProjectDirectory(), "DotnetLensMcp.Playground"));
            var discovery = new AnnotatedTestDiscovery();
            
            try
            {
                return await discovery.GetXUnitTestDataAsync(playgroundPath);
            }
            catch (Exception ex)
            {
                // Return empty test data if discovery fails
                // This prevents the test class from failing to load
                Console.WriteLine($"Warning: Failed to discover annotation test cases: {ex.Message}");
                return new List<object[]>();
            }
        }

        [Theory]
        [MemberData(nameof(GetAnnotationTestCases))]
        public async Task GetTypeAtPosition_AnnotationDriven_ShouldMatchExpectations(TypeTestCase testCase)
        {
            // Arrange
            var annotation = testCase.Annotation;
            var position = testCase.Position;

            Console.WriteLine($"Testing: {annotation.DisplayName}");
            Console.WriteLine($"Target: '{annotation.Target}' at {annotation.FilePath}:{position.Line}:{position.Column}");
            Console.WriteLine($"Expected: {annotation.Expect} (Kind: {annotation.Kind})");

            // Act
            var typeInfo = await _fixture.RoslynService.GetTypeAtPositionAsync(
                annotation.FilePath, 
                position.Line, 
                position.Column, 
                _fixture.SolutionPath, 
                TestContext.Current.CancellationToken);

            // Assert
            typeInfo.ShouldNotBeNull($"Expected to find type information for '{annotation.Target}' but got null");

            // Validate FullTypeName
            typeInfo.FullTypeName.ShouldBe(annotation.Expect, 
                $"Type name mismatch for '{annotation.Target}' at {annotation.FilePath}:{position.Line}:{position.Column}");

            // Validate Kind
            typeInfo.Kind.ShouldBe(annotation.Kind,
                $"Symbol kind mismatch for '{annotation.Target}' at {annotation.FilePath}:{position.Line}:{position.Column}");

            // Validate generic properties if specified
            if (annotation.Generic.HasValue)
            {
                typeInfo.IsGeneric.ShouldBe(annotation.Generic.Value,
                    $"Generic flag mismatch for '{annotation.Target}' - expected {annotation.Generic.Value} but got {typeInfo.IsGeneric}");
            }

            // Validate type arguments if specified
            if (annotation.Args != null && annotation.Args.Length > 0)
            {
                typeInfo.TypeArguments.ShouldNotBeNull("Expected type arguments but got null");
                typeInfo.TypeArguments.Count.ShouldBe(annotation.Args.Length,
                    $"Type argument count mismatch for '{annotation.Target}' - expected {annotation.Args.Length} but got {typeInfo.TypeArguments.Count}");

                for (int i = 0; i < annotation.Args.Length; i++)
                {
                    typeInfo.TypeArguments[i].ShouldBe(annotation.Args[i],
                        $"Type argument {i} mismatch for '{annotation.Target}' - expected '{annotation.Args[i]}' but got '{typeInfo.TypeArguments[i]}'");
                }
            }

            Console.WriteLine($"✓ Test passed: {annotation.Target} resolved to {typeInfo.FullTypeName} ({typeInfo.Kind})");
        }

        [Fact]
        public async Task Discovery_ShouldFindTestCases()
        {
            // Act
            var testCases = await _testDiscovery.DiscoverTestCasesAsync(_playgroundBasePath);

            // Assert
            testCases.ShouldNotBeEmpty("Should discover at least some annotation test cases");
            
            Console.WriteLine($"Discovered {testCases.Count} annotation test cases");
            
            // Validate that all test cases have valid data
            foreach (var testCase in testCases)
            {
                testCase.Annotation.ShouldNotBeNull();
                testCase.Annotation.IsValid.ShouldBeTrue($"Invalid annotation: {testCase.Annotation.DisplayName}");
                testCase.Position.ShouldNotBeNull();
                testCase.Position.IsValid.ShouldBeTrue($"Invalid position for: {testCase.Annotation.DisplayName}");
            }
        }

        [Fact]
        public async Task Discovery_ShouldProvideValidationResults()
        {
            // Act
            var validation = await _testDiscovery.ValidateDiscoveryAsync(_playgroundBasePath);

            // Assert
            validation.ShouldNotBeNull();
            validation.TotalTestCases.ShouldBeGreaterThan(0, "Should discover at least some test cases");
            validation.ValidTestCases.ShouldBe(validation.TotalTestCases, 
                $"All test cases should be valid. Errors: {string.Join(", ", validation.Errors)}");

            Console.WriteLine($"Validation: {validation.Summary}");
            
            if (validation.Warnings.Any())
            {
                Console.WriteLine($"Warnings: {string.Join("; ", validation.Warnings)}");
            }
        }

        [Fact]
        public async Task Discovery_ShouldProvideStatistics()
        {
            // Act
            var stats = await _testDiscovery.GetStatisticsAsync(_playgroundBasePath);

            // Assert
            stats.ShouldNotBeNull();
            stats.TotalTestCases.ShouldBeGreaterThan(0);
            stats.FilesCovered.ShouldBeGreaterThan(0);
            stats.KindsCovered.ShouldNotBeEmpty();

            Console.WriteLine($"Statistics: {stats}");
            Console.WriteLine($"Files with test cases:");
            foreach (var file in stats.TestCasesByFile.OrderByDescending(kvp => kvp.Value))
            {
                Console.WriteLine($"  {file.Key}: {file.Value} cases");
            }
        }

        /// <summary>
        /// Manual test to validate specific annotation parsing
        /// </summary>
        [Fact]
        public void AnnotationParser_ShouldParseBasicAnnotations()
        {
            // Arrange
            var parser = new AnnotationParser();
            var testLine = @"var simpleString = ""Hello""; // @test:type target=""simpleString"" expect=""string"" kind=""Local""";

            // Act
            var result = parser.ParseText(testLine, "test.cs");

            // Assert
            result.IsSuccess.ShouldBeTrue($"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
            result.Annotations.Count.ShouldBe(1);
            
            var annotation = result.Annotations[0];
            annotation.Target.ShouldBe("simpleString");
            annotation.Expect.ShouldBe("string");
            annotation.Kind.ShouldBe("Local");
            annotation.IsValid.ShouldBeTrue();
        }

        /// <summary>
        /// Manual test to validate position resolution
        /// </summary>
        [Fact]
        public void PositionResolver_ShouldFindTargetText()
        {
            // Arrange
            var resolver = new TextPositionResolver();
            var testLine = @"var simpleString = ""Hello, World!"";";

            // Act
            var column = resolver.FindTargetColumn(testLine, "simpleString");

            // Assert
            column.ShouldBeGreaterThan(0, "Should find the target text");
            
            // Verify the found position points to the start of "simpleString"
            var foundText = testLine.Substring(column - 1, "simpleString".Length);
            foundText.ShouldBe("simpleString");
        }

        /// <summary>
        /// Test that validates edge cases in position resolution
        /// </summary>
        [Fact]
        public void PositionResolver_ShouldHandleEdgeCases()
        {
            // Arrange
            var resolver = new TextPositionResolver();

            // Test multiple occurrences
            var line1 = "var test = test + test;";
            resolver.FindTargetColumn(line1, "test").ShouldBe(5);  // First "test"
            resolver.FindTargetColumn(line1, "test", 2).ShouldBe(12); // Second "test"
            resolver.FindTargetColumn(line1, "test", 3).ShouldBe(19); // Third "test"
            resolver.FindTargetColumn(line1, "test", 4).ShouldBe(-1); // Fourth doesn't exist

            // Test word boundaries
            var line2 = "testing test tested";
            resolver.FindTargetColumn(line2, "test").ShouldBe(9); // Should find "test", not "testing" or "tested"

            // Test not found
            resolver.FindTargetColumn("var x = 42;", "notfound").ShouldBe(-1);
        }
    }
}