using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using RoslynMcp.Models;
using RoslynMcp.Services;
using System.ComponentModel;

namespace RoslynMcp.Tools;

public class RoslynTools(
    RoslynService roslynService, 
    ILogger<RoslynTools> logger)
{
    [McpServerTool]
    [Description("Initialize or detect the workspace and solution. Should be called once at the start of a session.")]
    public async Task<object> InitializeWorkspace(
        [Description("Directory to search from (defaults to current working directory)")] string? workingDirectory = null,
        [Description("Preferred solution if multiple are found")] string? preferredSolution = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workspace = await roslynService.InitializeWorkspaceAsync(workingDirectory, preferredSolution);

            return new
            {
                success = true,
                data = new
                {
                    primarySolution = workspace.PrimarySolution != null ? new
                    {
                        path = workspace.PrimarySolution.Path,
                        name = workspace.PrimarySolution.Name,
                        projectCount = workspace.PrimarySolution.ProjectCount
                    } : null,
                    allSolutions = workspace.AllSolutions.Select(s => new
                    {
                        path = s.Path,
                        projects = s.Projects
                    }),
                    frameworkVersions = workspace.FrameworkVersions,
                    workspaceRoot = workspace.WorkspaceRoot
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize workspace");
            
            return new
            {
                success = false,
                error = new
                {
                    code = "INITIALIZATION_FAILED",
                    message = ex.Message,
                    details = new { exception = ex.GetType().Name }
                }
            };
        }
    }

    [McpServerTool]
    [Description("Get type information at a specific position in the code.")]
    public async Task<object> GetTypeAtPosition(
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
                return new
                {
                    success = false,
                    error = new
                    {
                        code = "SYMBOL_NOT_FOUND",
                        message = $"No symbol found at position {line}:{column} in {filePath}"
                    }
                };
            }

            return new
            {
                success = true,
                data = new
                {
                    symbolName = symbolInfo.SymbolName,
                    fullTypeName = symbolInfo.FullTypeName,
                    kind = symbolInfo.Kind,
                    assembly = symbolInfo.Assembly,
                    @namespace = symbolInfo.Namespace,
                    documentation = symbolInfo.Documentation,
                    isGeneric = symbolInfo.IsGeneric,
                    typeArguments = symbolInfo.TypeArguments,
                    baseType = symbolInfo.BaseType,
                    interfaces = symbolInfo.Interfaces,
                    resolvedFromSolution = symbolInfo.ResolvedFromSolution
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get type at position");
            
            return new
            {
                success = false,
                error = new
                {
                    code = DetermineErrorCode(ex),
                    message = ex.Message,
                    details = new { exception = ex.GetType().Name }
                }
            };
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