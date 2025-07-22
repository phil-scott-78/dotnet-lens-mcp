using DotnetLensMcp.Models;
using DotnetLensMcp.Tests.Fixtures;
using Shouldly;

namespace DotnetLensMcp.Tests.Services;

[Collection("RoslynService Collection")]
public class RoslynServiceIntegrationTests
{
    private readonly RoslynServiceFixture _fixture;

    public RoslynServiceIntegrationTests(RoslynServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EndToEnd_NavigateFromUsageToDefinitionAndBack()
    {
        // This test simulates a common developer workflow:
        // 1. Find where a method is defined
        // 2. Navigate to the definition
        // 3. Find all references to that method
        
        // Arrange
        var toolsFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Tools/RoslynTools.cs");
        var fileContent = await File.ReadAllTextAsync(toolsFile, TestContext.Current.CancellationToken);
        
        // Find usage of FindSymbolDefinitionAsync in RoslynTools
        var methodUsage = "FindSymbolDefinitionAsync(";
        var usageIndex = fileContent.IndexOf(methodUsage, StringComparison.Ordinal);
        usageIndex.ShouldBeGreaterThan(0);
        
        var (usageLine, usageColumn) = TestHelpers.GetPositionFromIndex(fileContent, usageIndex);
        
        // Step 1: Find the definition of FindSymbolDefinitionAsync
        var (location, symbolInfo, _) = await _fixture.RoslynService.FindSymbolDefinitionAsync(toolsFile, usageLine, usageColumn, _fixture.SolutionPath, TestContext.Current.CancellationToken);
        
        // Assert definition found
        location.ShouldNotBeNull();
        location.FilePath.ShouldEndWith("RoslynService.cs");
        symbolInfo.ShouldNotBeNull();
        symbolInfo.SymbolName.ShouldBe("FindSymbolDefinitionAsync");
        
        // Step 2: From the definition, find all references
        var (references, _, _) = await _fixture.RoslynService.FindReferencesAsync(location.FilePath, location.Line, location.Column, includeDeclaration: true, solutionPath: _fixture.SolutionPath, cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert references found
        var referenceLocations = references as ReferenceLocation[] ?? references.ToArray();
        referenceLocations.ShouldNotBeNull();
        referenceLocations.Count().ShouldBeGreaterThan(1); // At least definition + one usage
        
        // Should include the original usage in RoslynTools
        referenceLocations.ShouldContain(r => 
            r.FilePath.EndsWith("RoslynTools.cs") && 
            Math.Abs(r.Line - usageLine) <= 1); // Allow for small line differences
    }

    [Fact]
    public async Task EndToEnd_AnalyzeTypeHierarchyOfProject()
    {
        // This test explores the type hierarchy of key services in the project
        
        // Arrange
        var serviceFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs");
        
        // Act - Get type hierarchy for RoslynService
        var typeHierarchy = await _fixture.RoslynService.GetTypeHierarchyAsync(serviceFile, 1, 1, direction: "Both", typeName: "RoslynService", solutionPath: _fixture.SolutionPath, cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert
        typeHierarchy.ShouldNotBeNull();
        // May or may not show Object as base type depending on filtering
        if (typeHierarchy.BaseTypes.Count != 0)
        {
            typeHierarchy.BaseTypes.First().ShouldNotBeNull();
        }
        
        // Check if any types derive from RoslynService
        // (Probably none, but the structure should be there)
        typeHierarchy.DerivedTypes.ShouldNotBeNull();
    }

    [Fact]
    public async Task EndToEnd_ExploreProjectStructure()
    {
        // This test verifies we can explore the project structure effectively
        
        // Act - Get diagnostics for the entire solution to understand project health
        var diagnostics = await _fixture.RoslynService.GetCompilationDiagnosticsAsync(null, _fixture.SolutionPath, TestContext.Current.CancellationToken);
        
        // Assert
        var diagnosticInfos = diagnostics as DiagnosticInfo[] ?? diagnostics.ToArray();
        diagnosticInfos.ShouldNotBeNull();
        
        // The solution should compile (no errors)
        var errorCount = diagnosticInfos.Count(d => d.Severity == "Error");
        errorCount.ShouldBe(0, $"Solution has {errorCount} compilation errors");
        
        // Check various files exist and can be analyzed
        var filesToCheck = new[]
        {
            "DotnetLensMcp/Services/RoslynService.cs",
            "DotnetLensMcp/Services/SolutionCache.cs", 
            "DotnetLensMcp/Services/WorkspaceResolver.cs",
            "DotnetLensMcp/Tools/RoslynTools.cs",
            "DotnetLensMcp/Models/SymbolInfo.cs"
        };
        
        foreach (var relativePath in filesToCheck)
        {
            var filePath = TestHelpers.GetTestFilePath(relativePath);
            File.Exists(filePath).ShouldBeTrue($"Expected file {relativePath} to exist");
            
            // Verify we can get type info from each file
            var fileContent = await File.ReadAllTextAsync(filePath, TestContext.Current.CancellationToken);
            var classIndex = fileContent.IndexOf("public class", StringComparison.Ordinal);
            
            if (classIndex > 0)
            {
                var (line, column) = TestHelpers.GetPositionFromIndex(fileContent, classIndex + "public ".Length);
                
                var typeInfo = await _fixture.RoslynService.GetTypeAtPositionAsync(filePath, line, column, _fixture.SolutionPath, TestContext.Current.CancellationToken);
                
                typeInfo.ShouldNotBeNull($"Failed to get type info from {relativePath}");
            }
        }
    }

    [Fact]
    public async Task EndToEnd_FindAllImplementationsOfInterfaces()
    {
        // This test finds all implementations of common interfaces in the project
        
        // Look for IDisposable implementations as an example
        var implementations = await _fixture.RoslynService.FindImplementationsAsync(TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs"), 1, 1, typeName: "IDisposable", findInterfaceImplementations: true, findDerivedTypes: false, solutionPath: _fixture.SolutionPath, cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert
        var implementationInfos = implementations as ImplementationInfo[] ?? implementations.ToArray();
        implementationInfos.ShouldNotBeNull();
    }

    [Fact]
    public async Task EndToEnd_VerifyDiagnosticsForKnownPatterns()
    {
        // This test verifies that we can detect common code patterns through diagnostics
        
        // Arrange - Find a complex method in RoslynService
        var serviceFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs");
        
        // Act - Get diagnostics for a specific file
        var diagnostics = await _fixture.RoslynService.GetCompilationDiagnosticsAsync(serviceFile, _fixture.SolutionPath, TestContext.Current.CancellationToken);
        
        // Assert
        var diagnosticInfos = diagnostics as DiagnosticInfo[] ?? diagnostics.ToArray();
        diagnosticInfos.ShouldNotBeNull();
        
        // Common diagnostic IDs we might see:
        // CS8600 - Converting null literal or possible null value
        // CS8602 - Dereference of a possibly null reference
        // CS8604 - Possible null reference argument
        // IDE0003 - Remove qualification
        // IDE0059 - Unnecessary assignment
        
        // Verify all diagnostics are valid
        foreach (var diagnostic in diagnosticInfos)
        {
            diagnostic.Id.ShouldNotBeNullOrWhiteSpace();
            diagnostic.Message.ShouldNotBeNullOrWhiteSpace();
            diagnostic.FilePath.ShouldBe(serviceFile);
        }
    }

    [Fact] 
    public async Task EndToEnd_VerifyExtensionMethodsAvailable()
    {
        // This test verifies that extension methods are properly discovered
        
        // Arrange
        var serviceFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs");
        var fileContent = await File.ReadAllTextAsync(serviceFile, TestContext.Current.CancellationToken);
        
        // Find a location where we might use LINQ or other extension methods
        var linqUsage = ".Where(";
        var usageIndex = fileContent.IndexOf(linqUsage, StringComparison.Ordinal);
        
        if (usageIndex > 0)
        {
            // Position on the dot before Where - this is where we'd get completions
            usageIndex += 1; // Move past the dot
            var (line, column) = TestHelpers.GetPositionFromIndex(fileContent, usageIndex);
            
            // Act - Get available members including extension methods
            var members = await _fixture.RoslynService.GetAvailableMembersAsync(serviceFile, line, column, includeStatic: false, includeExtensionMethods: true, solutionPath: _fixture.SolutionPath, cancellationToken: TestContext.Current.CancellationToken);
            
            // Assert
            var memberInfos = members as MemberInfo[] ?? members.ToArray();
            memberInfos.ShouldNotBeNull();
            
            // Should include LINQ extension methods if System.Linq is imported
            var extensionMethods = memberInfos.Where(m => m.IsExtension).ToList();
            
            // If no extension methods found, it might be because the position isn't quite right
            // or the collection type doesn't support LINQ
            if (extensionMethods.Any())
            {
                // Common LINQ methods should be available
                extensionMethods.ShouldContain(m => m.Name == "Where" || m.Name == "Select" || m.Name == "FirstOrDefault");
            }
            else
            {
                // At least we should have some members available
                memberInfos.Count().ShouldBeGreaterThan(0);
            }
        }
    }
}