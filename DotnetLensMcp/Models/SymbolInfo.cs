namespace DotnetLensMcp.Models;

public class SymbolInfo
{
    public string SymbolName { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? Assembly { get; set; }
    public string? Namespace { get; set; }
    public string? Documentation { get; set; }
    public bool IsGeneric { get; set; }
    public List<string>? TypeArguments { get; set; }
    public string? BaseType { get; set; }
    public List<string>? Interfaces { get; set; }
    public string? ResolvedFromSolution { get; set; }
}