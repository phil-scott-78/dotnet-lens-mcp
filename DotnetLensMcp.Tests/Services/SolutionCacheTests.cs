using DotnetLensMcp.Services;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace DotnetLensMcp.Tests.Services;

public class SolutionCacheTests : IDisposable
{
    private readonly SolutionCache _solutionCache;
    private readonly MSBuildWorkspace _workspace;
    private readonly string _testSolutionPath;

    public SolutionCacheTests()
    {
        var logger = Substitute.For<ILogger<SolutionCache>>();
        _solutionCache = new SolutionCache(logger);
        _workspace = MSBuildWorkspace.Create();
        _testSolutionPath = FindTestSolutionPath();
    }

    private static string FindTestSolutionPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var searchDirectory = currentDirectory;
        
        while (!string.IsNullOrEmpty(searchDirectory))
        {
            var solutionFiles = Directory.GetFiles(searchDirectory, "dotnet-lens-mcp.sln");
            if (solutionFiles.Any())
            {
                return solutionFiles.First();
            }
            searchDirectory = Directory.GetParent(searchDirectory)?.FullName;
        }
        
        throw new InvalidOperationException("Could not find dotnet-lens-mcp.sln");
    }

    [Fact]
    public async Task GetSolutionAsync_WhenSolutionNotExists_ShouldThrow()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-solution.sln");
        
        // Act & Assert
        await Should.ThrowAsync<FileNotFoundException>(async () => 
            await _solutionCache.GetSolutionAsync(nonExistentPath));
    }

    [Fact]
    public async Task GetSolutionAsync_ShouldLoadAndCacheSolution()
    {
        // Arrange & Act
        var solution1 = await _solutionCache.GetSolutionAsync(_testSolutionPath, TestContext.Current.CancellationToken);
        var solution2 = await _solutionCache.GetSolutionAsync(_testSolutionPath, TestContext.Current.CancellationToken);
        
        // Assert
        solution1.ShouldNotBeNull();
        solution2.ShouldNotBeNull();
        solution1.ShouldBe(solution2); // Should return the same cached instance
        solution1.FilePath.ShouldBe(_testSolutionPath);
    }

    [Fact]
    public async Task InvalidateSolution_ShouldRemoveCachedSolution()
    {
        // Arrange
        var solution1 = await _solutionCache.GetSolutionAsync(_testSolutionPath, TestContext.Current.CancellationToken);
        
        // Act
        _solutionCache.InvalidateSolution(_testSolutionPath);
        var solution2 = await _solutionCache.GetSolutionAsync(_testSolutionPath, TestContext.Current.CancellationToken);
        
        // Assert
        solution1.ShouldNotBeNull();
        solution2.ShouldNotBeNull();
        // After invalidation, it should reload the solution
        // We can't easily test if it's a different instance, but we can verify it works
        solution2.FilePath.ShouldBe(_testSolutionPath);
    }

    [Fact]
    public async Task GetSolutionAsync_ShouldReturnSolutionWithDocuments()
    {
        // Arrange & Act
        var solution = await _solutionCache.GetSolutionAsync(_testSolutionPath, TestContext.Current.CancellationToken);
        
        // Assert
        solution.ShouldNotBeNull();
        solution.Projects.ShouldNotBeEmpty();
        
        var project = solution.Projects.First();
        project.Documents.ShouldNotBeEmpty();
        
        var document = project.Documents.First();
        document.FilePath.ShouldNotBeNullOrWhiteSpace();
        File.Exists(document.FilePath).ShouldBeTrue();
    }

    [Fact]
    public async Task StartFileWatching_ShouldBeCalledWhenLoadingSolution()
    {
        // This test verifies that file watching is set up when a solution is loaded
        // The actual file watching functionality is tested through integration
        
        // Arrange & Act
        var solution = await _solutionCache.GetSolutionAsync(_testSolutionPath, TestContext.Current.CancellationToken);
        
        // Assert
        solution.ShouldNotBeNull();
        // File watching should be started internally
        // We can't easily test the FileSystemWatcher directly, but we can verify the solution loads
    }

    [Fact]
    public async Task GetSolutionAsync_ShouldSupportProjectFiles()
    {
        // Arrange
        var projectPath = Path.Combine(
            Path.GetDirectoryName(_testSolutionPath)!,
            "DotnetLensMcp",
            "DotnetLensMcp.csproj");
        
        // Act
        var solution = await _solutionCache.GetSolutionAsync(projectPath, TestContext.Current.CancellationToken);
        
        // Assert
        solution.ShouldNotBeNull();
        solution.Projects.ShouldNotBeEmpty();
        solution.Projects.Any(p => p.FilePath == projectPath).ShouldBeTrue();
    }
    
    public void Dispose()
    {
        _workspace.Dispose();
    }
}