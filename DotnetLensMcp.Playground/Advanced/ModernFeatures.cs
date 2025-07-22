// ReSharper disable all
#pragma warning disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace DotnetLensMcp.Playground.Advanced
{
    /// <summary>
    /// Test scenarios for GetTypeAtPosition with modern C# features
    /// </summary>
    
    // Test: Records (C# 9.0+)
    public record Person(string FirstName, string LastName); // Positional record
    
    public record Employee(string FirstName, string LastName, int Id) : Person(FirstName, LastName); // Record inheritance
    
    public record struct Point(double X, double Y); // Record struct
    
    public record class Product // Record with properties
    {
        public int Id { get; init; } // Position should resolve to System.Int32
        public string Name { get; init; } // Position should resolve to System.String
        public decimal Price { get; init; } // Position should resolve to System.Decimal
    }

    // Test: Init-only properties (C# 9.0+)
    public class InitOnlyExample
    {
        public string Name { get; init; } // Position should resolve to System.String
        public DateTime Created { get; init; } = DateTime.Now; // Position should resolve to System.DateTime
        public List<string> Tags { get; init; } = new(); // Position should resolve to List<System.String>
    }

    // Test: Pattern matching scenarios
    public class PatternMatching
    {
        public void TestPatterns(object obj)
        {
            // Type patterns
            if (obj is string s) // Position on 's' should resolve to System.String
            {
                Console.WriteLine(s.Length);
            }
            
            // Declaration patterns
            if (obj is int { } number) // Position on 'number' should resolve to System.Int32
            {
                Console.WriteLine(number * 2);
            }
            
            // Property patterns
            if (obj is Person { FirstName: "John" } person) // Position on 'person' should resolve to Person
            {
                Console.WriteLine(person.LastName);
            }
            
            // Tuple patterns
            var point = (1, 2);
            var (x, y) = point; // Positions should resolve to System.Int32
            
            // Switch expressions
            string result = obj switch // Position should resolve to System.String
            {
                string str => str.ToUpper(),
                int i => i.ToString(),
                Person p => p.FirstName,
                _ => "Unknown"
            };
        }
        
        // Recursive patterns
        public decimal CalculatePrice(Product product) => product switch
        {
            { Price: > 100 } => product.Price * 0.9m, // Discount for expensive items
            { Price: > 50 } => product.Price * 0.95m,
            _ => product.Price
        }; // Return type should resolve to System.Decimal
    }

    // Test: Nullable reference types (C# 8.0+)
    #nullable enable
    public class NullableReferences
    {
        public string NonNullableString { get; set; } = ""; // Position should resolve to non-nullable System.String
        public string? NullableString { get; set; } // Position should resolve to nullable System.String
        
        public void ProcessStrings(string nonNull, string? nullable)
        {
            var length1 = nonNull.Length; // Safe - Position should resolve to System.Int32
            var length2 = nullable?.Length ?? 0; // Null-safe - Position should resolve to System.Int32
            
            List<string?> nullableList = new(); // Position should resolve to List<nullable System.String>
            Dictionary<string, string?> mixedDict = new(); // Complex nullable scenario
        }
        
        public T? GenericNullable<T>(T? input) where T : class // Position should resolve to nullable T
        {
            return input;
        }
    }
    #nullable restore

    // Test: Indices and ranges (C# 8.0+)
    public class IndicesAndRanges
    {
        public void TestRanges()
        {
            var array = new[] { 1, 2, 3, 4, 5 };
            
            Index lastIndex = ^1; // Position should resolve to System.Index
            var lastItem = array[lastIndex]; // Position should resolve to System.Int32
            
            Range range = 1..^1; // Position should resolve to System.Range
            var slice = array[range]; // Position should resolve to System.Int32[]
            
            ReadOnlySpan<int> span = array.AsSpan()[1..3]; // Position should resolve to ReadOnlySpan<System.Int32>
        }
    }

    // Test: Default interface implementations (C# 8.0+)
    public interface ILogger
    {
        void Log(string message);
        
        void LogError(string message) => Log($"ERROR: {message}"); // Default implementation
        void LogWarning(string message) => Log($"WARNING: {message}"); // Default implementation
    }

    // Test: Top-level statements simulation (would be in Program.cs normally)
    public static class TopLevelSimulation
    {
        public static void SimulateTopLevel()
        {
            var message = "Hello, World!"; // Position should resolve to System.String
            Console.WriteLine(message);
            
            var numbers = Enumerable.Range(1, 10); // Position should resolve to IEnumerable<System.Int32>
            var sum = numbers.Sum(); // Position should resolve to System.Int32
        }
    }

    // Test: File-scoped namespace (C# 10.0+) - Note: This is simulated since we're using traditional namespace
    // namespace DotnetLensMcp.Playground.Advanced; // Would be at file level

    // Test: Global using simulation (C# 10.0+) - Would be in GlobalUsings.cs
    // global using System.Text.Json; // Makes available everywhere

    // Test: Required members (C# 11.0+)
    public class RequiredExample
    {
        public required string Name { get; init; } // Position should resolve to System.String
        public required int Id { get; init; } // Position should resolve to System.Int32
        public DateTime? OptionalDate { get; init; } // Position should resolve to Nullable<System.DateTime>
    }

    // Test: Primary constructors (C# 12.0+)
    public class PrimaryConstructorClass(string name, int value) // Parameters should resolve to their types
    {
        public string Name { get; } = name; // Position should resolve to System.String
        public int Value { get; } = value; // Position should resolve to System.Int32
        
        public void PrintInfo() => Console.WriteLine($"{name}: {value}"); // Parameters in scope
    }

    // Test: Collection expressions (C# 12.0+)
    public class CollectionExpressions
    {
        public void TestCollections()
        {
            int[] array = [1, 2, 3, 4, 5]; // Position should resolve to System.Int32[]
            List<string> list = ["a", "b", "c"]; // Position should resolve to List<System.String>
            HashSet<int> set = [1, 2, 3, 3, 2, 1]; // Position should resolve to HashSet<System.Int32>
            
            // Spread operator
            int[] more = [0, ..array, 6]; // Position should resolve to System.Int32[]
        }
    }

    // Test: Raw string literals (C# 11.0+)
    public class RawStringLiterals
    {
        public string Json = """
            {
                "name": "Test",
                "value": 123
            }
            """; // Position should resolve to System.String
            
        public string Interpolated = $"""
            The value is {42}
            """; // Position should resolve to System.String
    }

    // Test: Generic math (C# 11.0+)
    public interface IAddable<T> where T : IAddable<T>
    {
        static abstract T operator +(T left, T right);
    }
    
    public class GenericMath
    {
        public T Add<T>(T x, T y) where T : IAddable<T> // Position should resolve to T with constraint
        {
            return x + y; // Using generic math
        }
    }
}