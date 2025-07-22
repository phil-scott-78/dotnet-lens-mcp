namespace DotnetLensMcp.Models;

public class LocationInfo
{
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public int? EndLine { get; set; }
    public int? EndColumn { get; set; }
}

public class WorkspaceInfo
{
    public SolutionInfo? PrimarySolution { get; set; }
    public List<SolutionInfo> AllSolutions { get; set; } = new();
    public List<string> FrameworkVersions { get; set; } = new();
    public string? WorkspaceRoot { get; set; }
}

public class SolutionInfo
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ProjectCount { get; set; }
    public List<string>? Projects { get; set; }
}