namespace DotnetLensMcp.Models;


public class CodeBlockAnalysis
{
    public List<DiagnosticInfo> Diagnostics { get; set; } = new();
    public List<SymbolInfo> DeclaredSymbols { get; set; } = new();
    public List<SymbolInfo> ReferencedSymbols { get; set; } = new();
    public int CyclomaticComplexity { get; set; }
    public int LinesOfCode { get; set; }
}