using DotnetLensMcp.Services;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DotnetLensMcp.Tests.Fixtures;

public class RoslynServiceFixture : IDisposable
{
    public RoslynService RoslynService { get; }
    public WorkspaceResolver WorkspaceResolver { get; }
    public SolutionCache SolutionCache { get; }
    public string SolutionPath { get; }
    public MSBuildWorkspace Workspace { get; }

    public RoslynServiceFixture()
    {
        // Use the project's own solution file for testing
        var currentDirectory = Directory.GetCurrentDirectory();
        var searchDirectory = currentDirectory;
        
        // Search up the directory tree for the solution file
        while (!string.IsNullOrEmpty(searchDirectory))
        {
            var solutionFiles = Directory.GetFiles(searchDirectory, "dotnet-lens-mcp.sln");
            if (solutionFiles.Any())
            {
                SolutionPath = solutionFiles.First();
                break;
            }
            searchDirectory = Directory.GetParent(searchDirectory)?.FullName;
        }

        if (string.IsNullOrEmpty(SolutionPath))
        {
            throw new InvalidOperationException("Could not find dotnet-lens-mcp.sln");
        }

        // Create mock loggers
        var solutionCacheLogger = Substitute.For<ILogger<SolutionCache>>();
        var workspaceResolverLogger = Substitute.For<ILogger<WorkspaceResolver>>();
        var roslynServiceLogger = Substitute.For<ILogger<RoslynService>>();
        
        SolutionCache = new SolutionCache(solutionCacheLogger);
        WorkspaceResolver = new WorkspaceResolver(workspaceResolverLogger);
        RoslynService = new RoslynService(SolutionCache, WorkspaceResolver, roslynServiceLogger);
        
        // Pre-load the workspace - SolutionCache will handle this internally
        Workspace = MSBuildWorkspace.Create();
    }

    public void Dispose()
    {
        Workspace.Dispose();
    }
}

[CollectionDefinition("RoslynService Collection")]
public class RoslynServiceCollection : ICollectionFixture<RoslynServiceFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}