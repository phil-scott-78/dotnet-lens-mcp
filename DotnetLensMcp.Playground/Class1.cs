namespace DotnetLensMcp.Playground;

/// <summary>
/// Entry point for DotnetLensMcp.Playground test scenarios.
/// This project contains static test code for validating RoslynService functionality.
/// </summary>
public class TestScenarioOverview
{
    /// <summary>
    /// GetTypeAtPosition Test Scenarios:
    /// 
    /// Basic/Variables.cs - Variable declarations, collections, arrays, tuples, anonymous types
    /// Basic/Methods.cs - Method return types, parameters, generics, async methods
    /// Basic/SimpleClass.cs - Classes, interfaces, properties, fields, nested types
    /// Generics/GenericClass.cs - Complex generic types, nested generics, covariance
    /// Generics/GenericConstraints.cs - Various generic constraints (class, struct, new(), etc.)
    /// Advanced/ModernFeatures.cs - Records, patterns, nullable references, modern C# features
    /// </summary>
    public static void Overview()
    {
        // This file serves as documentation for the test scenarios
        // Each file contains specific positions marked with comments indicating
        // what type should be resolved at that position
    }
    
    // Test data for integration tests
    public const string TestNamespace = "DotnetLensMcp.Playground";
    
    // Common test positions (line, column) for key scenarios
    public static class TestPositions
    {
        // Variables.cs positions
        public const int VarStringLine = 16;
        public const int VarStringColumn = 13; // 'var' keyword position
        
        // Methods.cs positions  
        public const int AsyncMethodLine = 35;
        public const int AsyncMethodColumn = 18; // Task<string> position
        
        // Add more positions as tests are implemented
    }
}
