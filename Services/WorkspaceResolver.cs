using Microsoft.Extensions.Logging;
using RoslynMcp.Models;

namespace RoslynMcp.Services;

public class WorkspaceResolver
{
    private WorkspaceInfo? _currentWorkspace;
    private readonly Dictionary<string, string> _fileToSolutionCache = new();
    private readonly ILogger<WorkspaceResolver> _logger;

    public WorkspaceResolver(ILogger<WorkspaceResolver> logger)
    {
        _logger = logger;
    }

    public async Task<WorkspaceInfo> InitializeWorkspaceAsync(string? workingDirectory = null, string? preferredSolution = null)
    {
        workingDirectory ??= Directory.GetCurrentDirectory();
        _logger.LogInformation("Initializing workspace from: {Directory}", workingDirectory);

        var solutions = await FindSolutionsAsync(workingDirectory);
        
        SolutionInfo? primarySolution = null;

        if (!string.IsNullOrEmpty(preferredSolution))
        {
            primarySolution = solutions.FirstOrDefault(s => s.Path.Equals(preferredSolution, StringComparison.OrdinalIgnoreCase));
        }

        if (primarySolution == null && solutions.Count == 1)
        {
            primarySolution = solutions[0];
        }

        _currentWorkspace = new WorkspaceInfo
        {
            PrimarySolution = primarySolution,
            AllSolutions = solutions,
            WorkspaceRoot = workingDirectory,
            FrameworkVersions = await GetFrameworkVersionsAsync(solutions)
        };

        _logger.LogInformation("Workspace initialized with {Count} solutions", solutions.Count);
        return _currentWorkspace;
    }

    public async Task<string?> FindSolutionForFileAsync(string filePath)
    {
        filePath = Path.GetFullPath(filePath);
        
        if (_fileToSolutionCache.TryGetValue(filePath, out var cached))
        {
            _logger.LogDebug("Using cached solution for file: {File}", filePath);
            return cached;
        }

        var solution = await SearchUpwardForSolution(filePath);
        if (solution != null)
        {
            _fileToSolutionCache[filePath] = solution;
            return solution;
        }

        if (_currentWorkspace?.PrimarySolution != null)
        {
            _logger.LogDebug("Using primary solution for file: {File}", filePath);
            return _currentWorkspace.PrimarySolution.Path;
        }

        solution = await SearchFromCurrentDirectory();
        if (solution != null)
        {
            _fileToSolutionCache[filePath] = solution;
        }

        return solution;
    }

    private async Task<string?> SearchUpwardForSolution(string filePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        
        while (!string.IsNullOrEmpty(directory))
        {
            var slnFiles = Directory.GetFiles(directory, "*.sln");
            if (slnFiles.Length == 1)
            {
                _logger.LogDebug("Found solution: {Solution}", slnFiles[0]);
                return slnFiles[0];
            }
            
            if (slnFiles.Length == 0)
            {
                var csprojFiles = Directory.GetFiles(directory, "*.csproj");
                if (csprojFiles.Length == 1)
                {
                    _logger.LogDebug("Found project: {Project}", csprojFiles[0]);
                    return csprojFiles[0];
                }
            }
            
            directory = Directory.GetParent(directory)?.FullName;
        }
        
        return null;
    }

    private async Task<string?> SearchFromCurrentDirectory()
    {
        var currentDir = Directory.GetCurrentDirectory();
        return await SearchUpwardForSolution(currentDir);
    }

    private async Task<List<SolutionInfo>> FindSolutionsAsync(string directory)
    {
        var solutions = new List<SolutionInfo>();
        
        var slnFiles = Directory.GetFiles(directory, "*.sln", SearchOption.AllDirectories);
        foreach (var slnFile in slnFiles)
        {
            var info = new SolutionInfo
            {
                Path = slnFile,
                Name = Path.GetFileNameWithoutExtension(slnFile),
                Projects = await GetProjectNamesAsync(slnFile)
            };
            info.ProjectCount = info.Projects?.Count ?? 0;
            solutions.Add(info);
        }

        if (solutions.Count == 0)
        {
            var csprojFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories);
            foreach (var csprojFile in csprojFiles)
            {
                var info = new SolutionInfo
                {
                    Path = csprojFile,
                    Name = Path.GetFileNameWithoutExtension(csprojFile),
                    ProjectCount = 1,
                    Projects = new List<string> { Path.GetFileNameWithoutExtension(csprojFile) }
                };
                solutions.Add(info);
            }
        }

        return solutions;
    }

    private async Task<List<string>> GetProjectNamesAsync(string solutionPath)
    {
        var projects = new List<string>();
        
        try
        {
            var content = await File.ReadAllTextAsync(solutionPath);
            var lines = content.Split('\n');
            
            foreach (var line in lines)
            {
                if (line.StartsWith("Project("))
                {
                    var parts = line.Split('"');
                    if (parts.Length >= 4)
                    {
                        var projectName = parts[3];
                        if (!string.IsNullOrWhiteSpace(projectName))
                        {
                            projects.Add(projectName);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse solution file: {Path}", solutionPath);
        }

        return projects;
    }

    private async Task<List<string>> GetFrameworkVersionsAsync(List<SolutionInfo> solutions)
    {
        var frameworks = new HashSet<string>();
        
        foreach (var solution in solutions)
        {
            if (solution.Path.EndsWith(".csproj"))
            {
                try
                {
                    var content = await File.ReadAllTextAsync(solution.Path);
                    if (content.Contains("<TargetFramework>"))
                    {
                        var start = content.IndexOf("<TargetFramework>") + 17;
                        var end = content.IndexOf("</TargetFramework>", start);
                        if (end > start)
                        {
                            var framework = content.Substring(start, end - start);
                            frameworks.Add(framework);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read project file: {Path}", solution.Path);
                }
            }
        }

        return frameworks.ToList();
    }

    public WorkspaceInfo? CurrentWorkspace => _currentWorkspace;
}