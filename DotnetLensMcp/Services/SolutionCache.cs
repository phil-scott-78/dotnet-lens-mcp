using System.Collections.Concurrent;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace DotnetLensMcp.Services;

public class SolutionCache : IDisposable
{
    private readonly ConcurrentDictionary<string, CachedSolution> _cache = new();
    private readonly ILogger<SolutionCache> _logger;
    private static bool _msBuildInitialized;
    private static readonly object MsBuildLock = new();

    public class CachedSolution : IDisposable
    {
        public required Solution Solution { get; init; }
        public required MSBuildWorkspace Workspace { get; init; }
        public DateTime LastAccessed { get; set; }
        public FileSystemWatcher? Watcher { get; set; }

        public void Dispose()
        {
            Watcher?.Dispose();
            Workspace.Dispose();
        }
    }

    public SolutionCache(ILogger<SolutionCache> logger)
    {
        _logger = logger;
        InitializeMsBuild();
    }

    private void InitializeMsBuild()
    {
        lock (MsBuildLock)
        {
            if (_msBuildInitialized) return;
            try
            {
                MSBuildLocator.RegisterDefaults();
                _msBuildInitialized = true;
                _logger.LogInformation("MSBuild initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MSBuild");
                throw;
            }
        }
    }

    public async Task<Solution> GetSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(solutionPath);

        if (!_cache.TryGetValue(fullPath, out var cached))
        {
            return await LoadSolutionAsync(fullPath, cancellationToken);
        }
        
        cached.LastAccessed = DateTime.UtcNow;
        _logger.LogDebug("Retrieved cached solution: {Path}", fullPath);
        return cached.Solution;

    }

    private async Task<Solution> LoadSolutionAsync(string solutionPath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading solution: {Path}", solutionPath);
        
        var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, args) =>
        {
            _logger.LogWarning("Workspace failure: {Diagnostic}", args.Diagnostic.Message);
        };

        try
        {
            Solution solution;
            
            if (solutionPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);
            }
            else if (solutionPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                var project = await workspace.OpenProjectAsync(solutionPath, cancellationToken: cancellationToken);
                solution = project.Solution;
            }
            else
            {
                throw new ArgumentException($"Unsupported file type: {solutionPath}");
            }

            var cachedSolution = new CachedSolution
            {
                Solution = solution,
                Workspace = workspace,
                LastAccessed = DateTime.UtcNow
            };

            _cache.TryAdd(solutionPath, cachedSolution);
            StartFileWatching(solutionPath);
            
            _logger.LogInformation("Solution loaded successfully: {Path}", solutionPath);
            return solution;
        }
        catch (Exception ex)
        {
            workspace.Dispose();
            _logger.LogError(ex, "Failed to load solution: {Path}", solutionPath);
            throw;
        }
    }

    public void InvalidateSolution(string solutionPath)
    {
        var fullPath = Path.GetFullPath(solutionPath);
        
        if (_cache.TryRemove(fullPath, out var cached))
        {
            cached.Dispose();
            _logger.LogInformation("Invalidated cached solution: {Path}", fullPath);
        }
    }

    public void StartFileWatching(string solutionPath)
    {
        var fullPath = Path.GetFullPath(solutionPath);
        
        if (!_cache.TryGetValue(fullPath, out var cached))
            return;

        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory))
            return;

        var watcher = new FileSystemWatcher(directory)
        {
            Filter = "*.cs",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };

        watcher.Changed += (_, _) => OnFileChanged(fullPath);
        watcher.Created += (_, _) => OnFileChanged(fullPath);
        watcher.Deleted += (_, _) => OnFileChanged(fullPath);
        watcher.Renamed += (_, _) => OnFileChanged(fullPath);

        watcher.EnableRaisingEvents = true;
        cached.Watcher = watcher;
        
        _logger.LogDebug("Started file watching for: {Path}", fullPath);
    }

    private void OnFileChanged(string solutionPath)
    {
        _logger.LogDebug("File change detected, invalidating solution: {Path}", solutionPath);
        InvalidateSolution(solutionPath);
    }

    public void Dispose()
    {
        foreach (var cached in _cache.Values)
        {
            cached.Dispose();
        }
        _cache.Clear();
    }
}