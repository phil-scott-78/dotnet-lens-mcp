using DotnetLensMcp.Services;
using DotnetLensMcp.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace DotnetLensMcp.Tests.Services;

public class WorkspaceResolverTests
{
    private readonly WorkspaceResolver _workspaceResolver;

    public WorkspaceResolverTests()
    {
        var logger = Substitute.For<ILogger<WorkspaceResolver>>();
        _workspaceResolver = new WorkspaceResolver(logger);
    }

    [Fact]
    public async Task InitializeWorkspace_WhenSolutionExists_ShouldReturnWorkspaceInfo()
    {
        // Arrange
        var solutionPath = TestHelpers.GetTestFilePath("dotnet-lens-mcp.sln");
        var testDirectory = Path.GetDirectoryName(solutionPath)!;
        
        // Act
        var result = await _workspaceResolver.InitializeWorkspaceAsync(testDirectory);
        
        // Assert
        result.ShouldNotBeNull();
        result.PrimarySolution.ShouldNotBeNull();
        result.PrimarySolution!.Path.ShouldEndWith("dotnet-lens-mcp.sln");
        File.Exists(result.PrimarySolution.Path).ShouldBeTrue();
    }

    [Fact]
    public async Task InitializeWorkspace_WhenNoSolutionExists_ShouldReturnEmptyWorkspaceInfo()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);
        
        try
        {
            // Act
            var result = await _workspaceResolver.InitializeWorkspaceAsync(tempDirectory);
            
            // Assert
            result.ShouldNotBeNull();
            result.PrimarySolution.ShouldBeNull();
            result.AllSolutions.ShouldBeEmpty();
        }
        finally
        {
            Directory.Delete(tempDirectory);
        }
    }

    [Fact]
    public async Task InitializeWorkspace_ShouldReturnAllSolutionsInWorkspace()
    {
        // Arrange
        var solutionPath = TestHelpers.GetTestFilePath("dotnet-lens-mcp.sln");
        var testDirectory = Path.GetDirectoryName(solutionPath)!;
        
        // Act
        var result = await _workspaceResolver.InitializeWorkspaceAsync(testDirectory);
        
        // Assert
        result.ShouldNotBeNull();
        result.AllSolutions.ShouldNotBeEmpty();
        result.AllSolutions.Any(s => s.Path.EndsWith("dotnet-lens-mcp.sln")).ShouldBeTrue();
        result.AllSolutions.All(s => File.Exists(s.Path)).ShouldBeTrue();
    }

    [Fact]
    public async Task InitializeWorkspace_WhenPreferredSolutionProvided_ShouldSelectIt()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);
        
        var solution1 = Path.Combine(tempDirectory, "solution1.sln");
        var solution2 = Path.Combine(tempDirectory, "preferred.sln");
        var solution3 = Path.Combine(tempDirectory, "solution3.sln");
        
        await File.WriteAllTextAsync(solution1, "", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(solution2, "", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(solution3, "", TestContext.Current.CancellationToken);
        
        try
        {
            // Act
            var result = await _workspaceResolver.InitializeWorkspaceAsync(tempDirectory, solution2);
            
            // Assert
            result.ShouldNotBeNull();
            result.PrimarySolution.ShouldNotBeNull();
            result.PrimarySolution!.Path.ShouldEndWith("preferred.sln");
            result.AllSolutions.Count.ShouldBe(3);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public async Task FindSolutionForFile_WithFileInSolution_ShouldReturnSolutionPath()
    {
        // Arrange
        var testFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs");
        
        // Act
        var result = await _workspaceResolver.FindSolutionForFileAsync(testFile);
        
        // Assert
        result.ShouldNotBeNull();
        (result.EndsWith("dotnet-lens-mcp.sln") || result.EndsWith(".csproj")).ShouldBeTrue();
        File.Exists(result).ShouldBeTrue();
    }

    [Fact]
    public async Task FindSolutionForFile_WithFileOutsideSolution_ShouldReturnNullOrFallback()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), "test.cs");
        await File.WriteAllTextAsync(tempFile, "// test", TestContext.Current.CancellationToken);
        
        try
        {
            // Act
            var result = await _workspaceResolver.FindSolutionForFileAsync(tempFile);
            
            // Assert
            // WorkspaceResolver may return a project file when searching upward
            if (result != null)
            {
                (result.EndsWith(".csproj") || result.EndsWith(".sln")).ShouldBeTrue();
            }
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}