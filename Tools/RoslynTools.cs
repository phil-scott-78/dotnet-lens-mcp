using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using RoslynMcp.Services;
using System.ComponentModel;
using RoslynMcp.Services.SerializerExtensions;
using RoslynMcp.Models.Responses;

namespace RoslynMcp.Tools;

public class RoslynTools(
    RoslynService roslynService,
    ILogger<RoslynTools> logger)
{
    [McpServerTool]
    [Description("Initialize or detect the workspace and solution. Should be called once at the start of a session.")]
    public async Task<string> InitializeWorkspace(
        [Description("Directory to search from (defaults to current working directory)")] string? workingDirectory = null,
        [Description("Preferred solution if multiple are found")] string? preferredSolution = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workspace = await roslynService.InitializeWorkspaceAsync(workingDirectory, preferredSolution);

            
            return new InitializeWorkspaceResponse
            {
                Success = true,
                Data = new InitializeWorkspaceData
                {
                    PrimarySolution = workspace.PrimarySolution != null ? new SolutionInfo
                    {
                        Path = workspace.PrimarySolution.Path,
                        Name = workspace.PrimarySolution.Name,
                        ProjectCount = workspace.PrimarySolution.ProjectCount
                    } : null,
                    AllSolutions = workspace.AllSolutions.Select(s => new
                    {
                        path = s.Path,
                        projects = s.Projects
                    }),
                    FrameworkVersions = workspace.FrameworkVersions,
                    WorkspaceRoot = workspace.WorkspaceRoot
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize workspace");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = "INITIALIZATION_FAILED",
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("Get type information at a specific position in the code.")]
    public async Task<string> GetTypeAtPosition(
        [Description("Path to source file (absolute or relative to workspace)")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Override solution path (uses initialized workspace if not provided)")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var symbolInfo = await roslynService.GetTypeAtPositionAsync(
                filePath, 
                line, 
                column, 
                solutionPath, 
                cancellationToken);

            if (symbolInfo == null)
            {
                return new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Code = "SYMBOL_NOT_FOUND",
                        Message = $"No symbol found at position {line}:{column} in {filePath}"
                    }
                }.ToSerialized();
            }

            return new GetTypeAtPositionResponse
            {
                Success = true,
                Data = new TypeAtPositionData
                {
                    SymbolName = symbolInfo.SymbolName,
                    FullTypeName = symbolInfo.FullTypeName,
                    Kind = symbolInfo.Kind,
                    Assembly = symbolInfo.Assembly,
                    Namespace = symbolInfo.Namespace,
                    Documentation = symbolInfo.Documentation,
                    IsGeneric = symbolInfo.IsGeneric,
                    TypeArguments = symbolInfo.TypeArguments,
                    BaseType = symbolInfo.BaseType,
                    Interfaces = symbolInfo.Interfaces,
                    ResolvedFromSolution = symbolInfo.ResolvedFromSolution
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get type at position");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("Get all accessible members at a specific code position.")]
    public async Task<string> GetAvailableMembers(
        [Description("Path to source file")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Include extension methods")] bool includeExtensionMethods = true,
        [Description("Include static members")] bool includeStatic = true,
        [Description("Filter by name prefix")] string? filter = null,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var members = await roslynService.GetAvailableMembersAsync(
                filePath,
                line,
                column,
                includeExtensionMethods,
                includeStatic,
                filter,
                solutionPath,
                cancellationToken);

            var membersList = members.ToList();

            return new GetAvailableMembersResponse
            {
                Success = true,
                Data = new AvailableMembersData
                {
                    Members = membersList.Select(m => new MemberData
                    {
                        Name = m.Name,
                        Kind = m.Kind,
                        Signature = m.Signature,
                        DeclaringType = m.DeclaringType,
                        Accessibility = m.Accessibility,
                        Documentation = m.Documentation,
                        Parameters = m.Parameters?.Select(p => new ParameterData
                        {
                            Name = p.Name,
                            Type = p.Type,
                            Documentation = p.Documentation
                        }),
                        IsExtension = m.IsExtension,
                        IsStatic = m.IsStatic,
                        IsAsync = m.IsAsync
                    }),
                    TotalCount = membersList.Count,
                    FilteredCount = membersList.Count
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get available members");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("Find the definition location of a symbol.")]
    public async Task<string> FindSymbolDefinition(
        [Description("Path to source file")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (location, symbolInfo, sourceText) = await roslynService.FindSymbolDefinitionAsync(
                filePath,
                line,
                column,
                solutionPath,
                cancellationToken);

            if (location == null || symbolInfo == null)
            {
                return new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Code = "SYMBOL_NOT_FOUND",
                        Message = $"No symbol found at position {line}:{column} in {filePath}"
                    }
                }.ToSerialized();
            }

            return new FindSymbolDefinitionResponse
            {
                Success = true,
                Data = new SymbolDefinitionData
                {
                    DefinitionLocation = new LocationData
                    {
                        FilePath = location.FilePath,
                        Line = location.Line,
                        Column = location.Column,
                        EndLine = location.EndLine,
                        EndColumn = location.EndColumn
                    },
                    SymbolInfo = new SymbolInfoData
                    {
                        Name = symbolInfo.SymbolName,
                        Kind = symbolInfo.Kind,
                        ContainingType = symbolInfo.FullTypeName
                    },
                    SourceText = sourceText
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to find symbol definition");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("Find all references to a symbol.")]
    public async Task<string> FindReferences(
        [Description("Path to source file")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Include declaration")] bool includeDeclaration = false,
        [Description("Maximum results to return")] int maxResults = 100,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (references, totalCount, hasMore) = await roslynService.FindReferencesAsync(
                filePath,
                line,
                column,
                includeDeclaration,
                maxResults,
                solutionPath,
                cancellationToken);

            return new FindReferencesResponse
            {
                Success = true,
                Data = new ReferencesData
                {
                    References = references.Select(r => new ReferenceData
                    {
                        FilePath = r.FilePath,
                        Line = r.Line,
                        Column = r.Column,
                        LineText = r.LineText,
                        Kind = r.Kind
                    }),
                    TotalCount = totalCount,
                    HasMore = hasMore
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to find references");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("Find all implementations of an interface or derived types.")]
    public async Task<string> FindImplementations(
        [Description("Fully qualified type name")] string typeName,
        [Description("Find derived types")] bool findDerivedTypes = true,
        [Description("Find interface implementations")] bool findInterfaceImplementations = true,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var implementations = await roslynService.FindImplementationsAsync(
                typeName,
                findDerivedTypes,
                findInterfaceImplementations,
                solutionPath,
                cancellationToken);

            return new FindImplementationsResponse
            {
                Success = true,
                Data = new ImplementationsData
                {
                    Implementations = implementations.Select(i => new ImplementationData
                    {
                        TypeName = i.TypeName,
                        FullTypeName = i.FullTypeName,
                        FilePath = i.FilePath,
                        Line = i.Line,
                        Kind = i.Kind,
                        ImplementsDirectly = i.ImplementsDirectly
                    })
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to find implementations");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("Get inheritance hierarchy for a type.")]
    public async Task<string> GetTypeHierarchy(
        [Description("Fully qualified type name")] string typeName,
        [Description("Direction: Base, Derived, or Both")] string direction = "Both",
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var hierarchy = await roslynService.GetTypeHierarchyAsync(
                typeName,
                direction,
                solutionPath,
                cancellationToken);

            return new GetTypeHierarchyResponse
            {
                Success = true,
                Data = new TypeHierarchyData
                {
                    BaseTypes = hierarchy.BaseTypes.Select(t => new TypeInfoData
                    {
                        TypeName = t.TypeName,
                        FullTypeName = t.FullTypeName,
                        Assembly = t.Assembly
                    }),
                    DerivedTypes = hierarchy.DerivedTypes.Select(t => new TypeInfoData
                    {
                        TypeName = t.TypeName,
                        FullTypeName = t.FullTypeName,
                        Assembly = t.Assembly
                    }),
                    Interfaces = hierarchy.Interfaces.Select(t => new TypeInfoData
                    {
                        TypeName = t.TypeName,
                        FullTypeName = t.FullTypeName,
                        Assembly = t.Assembly
                    })
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get type hierarchy");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("Analyze a code block for diagnostics, symbols, and complexity.")]
    public async Task<string> AnalyzeCodeBlock(
        [Description("Path to source file")] string filePath,
        [Description("Start line (1-based)")] int startLine,
        [Description("End line (1-based)")] int endLine,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var analysis = await roslynService.AnalyzeCodeBlockAsync(
                filePath,
                startLine,
                endLine,
                solutionPath,
                cancellationToken);

            return new AnalyzeCodeBlockResponse
            {
                Success = true,
                Data = new CodeBlockAnalysisData
                {
                    Diagnostics = analysis.Diagnostics.Select(d => new DiagnosticData
                    {
                        Id = d.Id,
                        Severity = d.Severity,
                        Message = d.Message,
                        FilePath = d.FilePath,
                        Line = d.Line,
                        Column = d.Column,
                        EndLine = d.EndLine,
                        EndColumn = d.EndColumn,
                        Category = d.Category
                    }),
                    DeclaredSymbols = analysis.DeclaredSymbols.Select(s => new DeclaredSymbolData
                    {
                        SymbolName = s.SymbolName,
                        FullTypeName = s.FullTypeName,
                        Kind = s.Kind,
                        Namespace = s.Namespace
                    }),
                    ReferencedSymbols = analysis.ReferencedSymbols.Select(s => new ReferencedSymbolData
                    {
                        SymbolName = s.SymbolName,
                        FullTypeName = s.FullTypeName,
                        Kind = s.Kind,
                        Namespace = s.Namespace
                    }),
                    CyclomaticComplexity = analysis.CyclomaticComplexity,
                    LinesOfCode = analysis.LinesOfCode
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to analyze code block");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("Get compilation diagnostics for a file or entire solution.")]
    public async Task<string> GetCompilationDiagnostics(
        [Description("Path to source file (optional - omit for entire solution)")] string? filePath = null,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var diagnostics = await roslynService.GetCompilationDiagnosticsAsync(
                filePath,
                solutionPath,
                cancellationToken);

            var diagnosticsList = diagnostics.ToList();
            var errorCount = diagnosticsList.Count(d => d.Severity == "Error");
            var warningCount = diagnosticsList.Count(d => d.Severity == "Warning");

            return new GetCompilationDiagnosticsResponse
            {
                Success = true,
                Data = new CompilationDiagnosticsData
                {
                    Diagnostics = diagnosticsList.Select(d => new DiagnosticData
                    {
                        Id = d.Id,
                        Severity = d.Severity,
                        Message = d.Message,
                        FilePath = d.FilePath,
                        Line = d.Line,
                        Column = d.Column,
                        EndLine = d.EndLine,
                        EndColumn = d.EndColumn,
                        Category = d.Category
                    }),
                    Summary = new DiagnosticsSummary
                    {
                        TotalCount = diagnosticsList.Count,
                        ErrorCount = errorCount,
                        WarningCount = warningCount
                    }
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get compilation diagnostics");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    private string DetermineErrorCode(Exception ex)
    {
        return ex switch
        {
            InvalidOperationException => "SOLUTION_NOT_FOUND",
            FileNotFoundException => "FILE_NOT_FOUND",
            ArgumentException => "INVALID_POSITION",
            TaskCanceledException => "TIMEOUT",
            _ => "UNKNOWN_ERROR"
        };
    }
}