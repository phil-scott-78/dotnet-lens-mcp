namespace RoslynMcp.Models.Responses;

public class ErrorResponse
{
    public bool Success => false;
    public ErrorInfo Error { get; set; } = null!;
}

public class ErrorInfo
{
    public string Code { get; set; } = null!;
    public string Message { get; set; } = null!;
    public object? Details { get; set; }
}

public class InitializeWorkspaceResponse
{
    public bool Success { get; set; }
    public InitializeWorkspaceData? Data { get; set; }
}

public class InitializeWorkspaceData
{
    public SolutionInfo? PrimarySolution { get; set; }
    public IEnumerable<object> AllSolutions { get; set; } = Enumerable.Empty<object>();
    public List<string>? FrameworkVersions { get; set; }
    public string? WorkspaceRoot { get; set; }
}

public class SolutionInfo
{
    public string Path { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int ProjectCount { get; set; }
}

public class GetTypeAtPositionResponse
{
    public bool Success { get; set; }
    public TypeAtPositionData? Data { get; set; }
}

public class TypeAtPositionData
{
    public string? SymbolName { get; set; }
    public string? FullTypeName { get; set; }
    public string? Kind { get; set; }
    public string? Assembly { get; set; }
    public string? Namespace { get; set; }
    public string? Documentation { get; set; }
    public bool IsGeneric { get; set; }
    public List<string>? TypeArguments { get; set; }
    public string? BaseType { get; set; }
    public List<string>? Interfaces { get; set; }
    public string? ResolvedFromSolution { get; set; }
}

public class GetAvailableMembersResponse
{
    public bool Success { get; set; }
    public AvailableMembersData? Data { get; set; }
}

public class AvailableMembersData
{
    public IEnumerable<MemberData> Members { get; set; } = Enumerable.Empty<MemberData>();
    public int TotalCount { get; set; }
    public int FilteredCount { get; set; }
}

public class MemberData
{
    public string Name { get; set; } = null!;
    public string Kind { get; set; } = null!;
    public string? Signature { get; set; }
    public string? DeclaringType { get; set; }
    public string? Accessibility { get; set; }
    public string? Documentation { get; set; }
    public IEnumerable<ParameterData>? Parameters { get; set; }
    public bool IsExtension { get; set; }
    public bool IsStatic { get; set; }
    public bool IsAsync { get; set; }
}

public class ParameterData
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Documentation { get; set; }
}

public class FindSymbolDefinitionResponse
{
    public bool Success { get; set; }
    public SymbolDefinitionData? Data { get; set; }
}

public class SymbolDefinitionData
{
    public LocationData DefinitionLocation { get; set; } = null!;
    public SymbolInfoData SymbolInfo { get; set; } = null!;
    public string? SourceText { get; set; }
}

public class LocationData
{
    public string FilePath { get; set; } = null!;
    public int Line { get; set; }
    public int Column { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
}

public class SymbolInfoData
{
    public string Name { get; set; } = null!;
    public string Kind { get; set; } = null!;
    public string? ContainingType { get; set; }
}

public class FindReferencesResponse
{
    public bool Success { get; set; }
    public ReferencesData? Data { get; set; }
}

public class ReferencesData
{
    public IEnumerable<ReferenceData> References { get; set; } = Enumerable.Empty<ReferenceData>();
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
}

public class ReferenceData
{
    public string FilePath { get; set; } = null!;
    public int Line { get; set; }
    public int Column { get; set; }
    public string? LineText { get; set; }
    public string? Kind { get; set; }
}

public class FindImplementationsResponse
{
    public bool Success { get; set; }
    public ImplementationsData? Data { get; set; }
}

public class ImplementationsData
{
    public IEnumerable<ImplementationData> Implementations { get; set; } = Enumerable.Empty<ImplementationData>();
}

public class ImplementationData
{
    public string TypeName { get; set; } = null!;
    public string FullTypeName { get; set; } = null!;
    public string? FilePath { get; set; }
    public int Line { get; set; }
    public string? Kind { get; set; }
    public bool ImplementsDirectly { get; set; }
}

public class GetTypeHierarchyResponse
{
    public bool Success { get; set; }
    public TypeHierarchyData? Data { get; set; }
}

public class TypeHierarchyData
{
    public IEnumerable<TypeInfoData> BaseTypes { get; set; } = Enumerable.Empty<TypeInfoData>();
    public IEnumerable<TypeInfoData> DerivedTypes { get; set; } = Enumerable.Empty<TypeInfoData>();
    public IEnumerable<TypeInfoData> Interfaces { get; set; } = Enumerable.Empty<TypeInfoData>();
}

public class TypeInfoData
{
    public string TypeName { get; set; } = null!;
    public string FullTypeName { get; set; } = null!;
    public string? Assembly { get; set; }
}

public class AnalyzeCodeBlockResponse
{
    public bool Success { get; set; }
    public CodeBlockAnalysisData? Data { get; set; }
}

public class CodeBlockAnalysisData
{
    public IEnumerable<DiagnosticData> Diagnostics { get; set; } = Enumerable.Empty<DiagnosticData>();
    public IEnumerable<DeclaredSymbolData> DeclaredSymbols { get; set; } = Enumerable.Empty<DeclaredSymbolData>();
    public IEnumerable<ReferencedSymbolData> ReferencedSymbols { get; set; } = Enumerable.Empty<ReferencedSymbolData>();
    public int CyclomaticComplexity { get; set; }
    public int LinesOfCode { get; set; }
}

public class DiagnosticData
{
    public string Id { get; set; } = null!;
    public string Severity { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? FilePath { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public string? Category { get; set; }
}

public class DeclaredSymbolData
{
    public string? SymbolName { get; set; }
    public string? FullTypeName { get; set; }
    public string? Kind { get; set; }
    public string? Namespace { get; set; }
}

public class ReferencedSymbolData
{
    public string? SymbolName { get; set; }
    public string? FullTypeName { get; set; }
    public string? Kind { get; set; }
    public string? Namespace { get; set; }
}

public class GetCompilationDiagnosticsResponse
{
    public bool Success { get; set; }
    public CompilationDiagnosticsData? Data { get; set; }
}

public class CompilationDiagnosticsData
{
    public IEnumerable<DiagnosticData> Diagnostics { get; set; } = Enumerable.Empty<DiagnosticData>();
    public DiagnosticsSummary Summary { get; set; } = null!;
}

public class DiagnosticsSummary
{
    public int TotalCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
}