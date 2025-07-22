namespace RoslynMcp.Models;

public class MemberInfo
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string DeclaringType { get; set; } = string.Empty;
    public string Accessibility { get; set; } = string.Empty;
    public string? Documentation { get; set; }
    public List<ParameterInfo>? Parameters { get; set; }
    public bool IsExtension { get; set; }
    public bool IsStatic { get; set; }
    public bool IsAsync { get; set; }
}

public class ParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Documentation { get; set; }
}