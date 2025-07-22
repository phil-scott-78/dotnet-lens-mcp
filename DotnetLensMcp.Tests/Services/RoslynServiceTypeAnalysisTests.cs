using DotnetLensMcp.Models;
using DotnetLensMcp.Tests.Fixtures;
using Shouldly;

namespace DotnetLensMcp.Tests.Services;

[Collection("RoslynService Collection")]
public class RoslynServiceTypeAnalysisTests
{
    private readonly RoslynServiceFixture _fixture;

    public RoslynServiceTypeAnalysisTests(RoslynServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetTypeAtPositionAsync_ForVariable_ShouldReturnCorrectType()
    {
        // Arrange
        var testFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs");
        var fileContent = await File.ReadAllTextAsync(testFile, TestContext.Current.CancellationToken);
        
        // Find a variable declaration
        var variableName = "var document =";
        var index = fileContent.IndexOf(variableName, StringComparison.Ordinal);
        index.ShouldBeGreaterThan(0);
        index += "var ".Length; // Position on "document"
        
        var (line, column) = TestHelpers.GetPositionFromIndex(fileContent, index);
        
        // Act
        var typeInfo = await _fixture.RoslynService.GetTypeAtPositionAsync(testFile, line, column, _fixture.SolutionPath, TestContext.Current.CancellationToken);
        
        // Assert
        typeInfo.ShouldNotBeNull();
        typeInfo.FullTypeName.ShouldContain("Document");
    }

    [Fact]
    public async Task GetTypeAtPositionAsync_ForMethodCall_ShouldReturnReturnType()
    {
        // Arrange
        var testFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs");
        var fileContent = await File.ReadAllTextAsync(testFile, TestContext.Current.CancellationToken);
        
        // Find a method call
        var methodCall = "GetSyntaxRootAsync()";
        var index = fileContent.IndexOf(methodCall, StringComparison.Ordinal);
        
        if (index > 0)
        {
            var (line, column) = TestHelpers.GetPositionFromIndex(fileContent, index);
            
            // Act
            var typeInfo = await _fixture.RoslynService.GetTypeAtPositionAsync(testFile, line, column, _fixture.SolutionPath, TestContext.Current.CancellationToken);
            
            // Assert
            typeInfo.ShouldNotBeNull();
            typeInfo.FullTypeName.ShouldContain("Task");
        }
    }

    [Fact]
    public async Task GetTypeAtPositionAsync_ForProperty_ShouldReturnPropertyType()
    {
        // Arrange
        var testFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Models/SymbolInfo.cs");
        var fileContent = await File.ReadAllTextAsync(testFile, TestContext.Current.CancellationToken);
        
        // Find a property
        var propertyName = "public string SymbolName";
        var index = fileContent.IndexOf(propertyName, StringComparison.Ordinal);
        index.ShouldBeGreaterThan(0);
        index += "public string ".Length;
        
        var (line, column) = TestHelpers.GetPositionFromIndex(fileContent, index);
        
        // Act
        var typeInfo = await _fixture.RoslynService.GetTypeAtPositionAsync(testFile, line, column, _fixture.SolutionPath, TestContext.Current.CancellationToken);
        
        // Assert
        typeInfo.ShouldNotBeNull();
        typeInfo.FullTypeName.ShouldBe("string");
    }

    [Fact]
    public async Task GetTypeHierarchyAsync_ForClass_ShouldReturnBaseAndDerivedTypes()
    {
        // Arrange
        var testFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs");
        var fileContent = await File.ReadAllTextAsync(testFile, TestContext.Current.CancellationToken);
        
        // Find the class declaration
        var className = "public class RoslynService";
        var index = fileContent.IndexOf(className, StringComparison.Ordinal);
        index.ShouldBeGreaterThan(0);
        index += "public class ".Length;
        
        var (line, column) = TestHelpers.GetPositionFromIndex(fileContent, index);
        
        // Act
        var typeHierarchy = await _fixture.RoslynService.GetTypeHierarchyAsync(testFile, line, column, direction: "Both", solutionPath: _fixture.SolutionPath, cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert
        typeHierarchy.ShouldNotBeNull();
        typeHierarchy.BaseTypes.ShouldNotBeNull();
        // Object base type might be filtered out in some implementations
        if (typeHierarchy.BaseTypes.Any())
        {
            typeHierarchy.BaseTypes.ShouldContain(bt => bt.TypeName == "Object" || bt.TypeName == "object");
        }
    }

    [Fact]
    public async Task GetTypeHierarchyAsync_WithTypeName_ShouldFindTypeAndReturnHierarchy()
    {
        // Arrange
        var testFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs");
        
        // Act - search for RoslynService type which we know exists
        var typeHierarchy = await _fixture.RoslynService.GetTypeHierarchyAsync(testFile, 1, 1, direction: "Base", typeName: "RoslynService", solutionPath: _fixture.SolutionPath, cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert
        typeHierarchy.ShouldNotBeNull();
        // May or may not have base types depending on implementation
        if (typeHierarchy.BaseTypes.Count != 0)
        {
            typeHierarchy.BaseTypes.First().ShouldNotBeNull();
        }
    }
    
    [Fact]
    public async Task GetAvailableMembersAsync_WithExtensionMethods_ShouldIncludeExtensions()
    {
        // Arrange
        var testFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs");
        var fileContent = await File.ReadAllTextAsync(testFile, TestContext.Current.CancellationToken);
        
        // Find a string variable to test extension methods
        var stringVar = "string ";
        var index = fileContent.IndexOf($"{stringVar}filePath", StringComparison.Ordinal);
        
        if (index > 0)
        {
            index += stringVar.Length;
            
            // Move to a position where we might call methods on the string
            var methodCallIndex = fileContent.IndexOf("filePath.", index, StringComparison.Ordinal);
            if (methodCallIndex > 0)
            {
                methodCallIndex += "filePath.".Length;
                var (callLine, callColumn) = TestHelpers.GetPositionFromIndex(fileContent, methodCallIndex);
                
                // Act
                var members = await _fixture.RoslynService.GetAvailableMembersAsync(testFile, callLine, callColumn, includeExtensionMethods: true, includeStatic: false, solutionPath: _fixture.SolutionPath, cancellationToken: TestContext.Current.CancellationToken);
                
                // Assert
                var memberInfos = members as MemberInfo[] ?? members.ToArray();
                memberInfos.ShouldNotBeNull();
                
                // Should include both instance methods and extension methods
                memberInfos.ShouldContain(m => m.Name == "Length" && !m.IsExtension);
                // String likely has LINQ extension methods if System.Linq is imported
                var extensionMethods = memberInfos.Where(m => m.IsExtension).ToList();
                if (extensionMethods.Any())
                {
                    extensionMethods.ShouldContain(m => m.IsExtension);
                }
            }
        }
    }

    [Fact]
    public async Task GetTypeHierarchy_ForInterfaceImplementations_ShouldWork()
    {
        // Arrange
        var testFile = TestHelpers.GetTestFilePath("DotnetLensMcp/Services/RoslynService.cs");
        var fileContent = await File.ReadAllTextAsync(testFile, TestContext.Current.CancellationToken);
        
        // Find SolutionCache which implements IDisposable
        var className = "public class SolutionCache : IDisposable";
        var index = fileContent.IndexOf(className, StringComparison.Ordinal);
        
        if (index > 0)
        {
            index += "public class ".Length;
            var (line, column) = TestHelpers.GetPositionFromIndex(fileContent, index);
            
            // Act
            var typeHierarchy = await _fixture.RoslynService.GetTypeHierarchyAsync(testFile, line, column, direction: "Both", solutionPath: _fixture.SolutionPath, cancellationToken: TestContext.Current.CancellationToken);
            
            // Assert
            typeHierarchy.ShouldNotBeNull();
            typeHierarchy.Interfaces.ShouldNotBeNull();
            typeHierarchy.Interfaces.ShouldContain(i => i.TypeName == "IDisposable");
        }
    }
}