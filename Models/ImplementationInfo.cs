namespace RoslynMcp.Models;

public class ImplementationInfo
{
    public string TypeName { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public string Kind { get; set; } = string.Empty;
    public bool ImplementsDirectly { get; set; }
}