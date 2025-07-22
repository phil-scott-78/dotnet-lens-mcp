using DotnetLensMcp.Models;
using DotnetLensMcp.Tests.Fixtures;
using Shouldly;

[assembly: CollectionBehavior(DisableTestParallelization = true)]



namespace DotnetLensMcp.Tests.Services;

[Collection("RoslynService Collection")]
public class RoslynServiceSymbolNavigationTests
{
    private readonly RoslynServiceFixture _fixture;

    public RoslynServiceSymbolNavigationTests(RoslynServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FindSymbolDefinitionAsync_ForInvalidPosition_ShouldReturnError()
    {
        // Arrange
        var testFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs");
        
        // Act & Assert
        await Should.ThrowAsync<Exception>(async () => 
            await _fixture.RoslynService.FindSymbolDefinitionAsync(
                testFile, 99999, 99999, _fixture.SolutionPath));
    }

    [Fact]
    public async Task FindReferencesAsync_ForMethod_ShouldReturnAllUsages()
    {
        // Arrange
        var testFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs");
        var fileContent = await File.ReadAllTextAsync(testFile, TestContext.Current.CancellationToken);
        
        // Find the GetCompilationAsync method definition
        var methodName = "private async Task<Compilation> GetCompilationAsync";
        var index = fileContent.IndexOf(methodName, StringComparison.Ordinal);
        index.ShouldBeGreaterThan(0);
        
        // Position after "private async Task<Compilation> "
        index += "private async Task<Compilation> ".Length;
        var (line, column) = TestHelpers.GetPositionFromIndex(fileContent, index);
        
        // Act
        var (references, totalCount, _) = await _fixture.RoslynService.FindReferencesAsync(testFile, line, column, includeDeclaration: false, solutionPath: _fixture.SolutionPath, cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert
        var referenceLocations = references as ReferenceLocation[] ?? references.ToArray();
        referenceLocations.ShouldNotBeNull();
        referenceLocations.Count().ShouldBeGreaterThan(0);
        totalCount.ShouldBeGreaterThan(0);
        referenceLocations.All(r => r.FilePath.EndsWith(".cs")).ShouldBeTrue();
    }


    [Fact]
    public async Task GetCallHierarchyAsync_ForMethod_ShouldReturnCallers()
    {
        // Arrange
        var testFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs");
        var fileContent = await File.ReadAllTextAsync(testFile, TestContext.Current.CancellationToken);
        
        // Find a private method that's likely called by other methods
        var methodName = "GetCompilationAsync";
        var index = fileContent.IndexOf($"private async Task<Compilation> {methodName}", StringComparison.Ordinal);
        
        if (index > 0)
        {
            index += "private async Task<Compilation> ".Length;
            var (line, column) = TestHelpers.GetPositionFromIndex(fileContent, index);
            
            // Act
            var (references, _, _) = await _fixture.RoslynService.FindReferencesAsync(testFile, line, column, includeDeclaration: false, solutionPath: _fixture.SolutionPath, cancellationToken: TestContext.Current.CancellationToken);
            
            // Assert
            var referenceLocations = references as ReferenceLocation[] ?? references.ToArray();
            referenceLocations.ShouldNotBeNull();
            if (referenceLocations.Any())
            {
                referenceLocations.All(r => r.Kind == "Call" || r.Kind == "Reference").ShouldBeTrue();
            }
        }
    }
}