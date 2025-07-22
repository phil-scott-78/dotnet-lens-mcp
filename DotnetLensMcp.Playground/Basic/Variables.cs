// ReSharper disable all
#pragma warning disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetLensMcp.Playground.Basic
{
    /// <summary>
    /// Test scenarios for GetTypeAtPosition with various variable declarations
    /// </summary>
    public class Variables
    {
        // Test: Simple variable declaration (var keyword)
        public void VarDeclarations()
        {
            var simpleString = "Hello, World!"; // @test:type target="simpleString" expect="string?" kind="Local"
            var simpleInt = 42; // @test:type target="simpleInt" expect="int" kind="Local"
            var simpleDouble = 3.14; // @test:type target="simpleDouble" expect="double" kind="Local"
            var simpleBool = true; // @test:type target="simpleBool" expect="bool" kind="Local"
        }

        // Test: Explicit type declaration
        public void ExplicitTypeDeclarations()
        {
            string explicitString = "Explicit"; // @test:type target="explicitString" expect="string" kind="Local"
            int explicitInt = 100; // @test:type target="explicitInt" expect="int" kind="Local"
            DateTime explicitDate = DateTime.Now; // @test:type target="explicitDate" expect="System.DateTime"
            object explicitObject = new object(); // @test:type target="explicitObject" expect="object" kind="Local"
        }

        // Test: Nullable types
        public void NullableTypes()
        {
            int? nullableInt = 42; // @test:type target="nullableInt" expect="int?" kind="Local"
            bool? nullableBool = null; // @test:type target="nullableBool" expect="bool?" kind="Local"
            DateTime? nullableDate = DateTime.Now; // @test:type target="nullableDate" expect="System.DateTime?" kind="Local"
            
            // Nullable reference types (C# 8.0+)
            string? nullableString = null; // @test:type target="nullableString" expect="string?" kind="Local"
        }

        // Test: Array types
        public void ArrayTypes()
        {
            int[] intArray = new int[5]; // @test:type target="intArray" expect="int[]" kind="Local"
            string[] stringArray = { "a", "b", "c" }; // @test:type target="stringArray" expect="string[]" kind="Local"
            object[] objectArray = new object[10]; // @test:type target="objectArray" expect="object[]" kind="Local"
            
            // Multi-dimensional arrays
            int[,] twoDimensional = new int[3, 4]; // @test:type target="twoDimensional" expect="int[,]" kind="Local"
            int[][] jagged = new int[3][]; // @test:type target="jagged" expect="int[][]"
        }

        // Test: Collection types with generics
        public void CollectionTypes()
        {
            List<string> stringList = new List<string>(); // @test:type target="stringList" expect="System.Collections.Generic.List<string>" kind="Local" generic="true" args="string"
            Dictionary<int, string> dictionary = new Dictionary<int, string>(); // @test:type target="dictionary" expect="System.Collections.Generic.Dictionary<int, string>" kind="Local" generic="true" args="int,string"
            HashSet<double> hashSet = new HashSet<double>(); // @test:type target="hashSet" expect="System.Collections.Generic.HashSet<double>" kind="Local" generic="true" args="double"
            
            // Nested generics
            List<Dictionary<string, int>> nestedGeneric = new List<Dictionary<string, int>>(); // @test:type target="nestedGeneric" expect="System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, int>>" kind="Local" generic="true"
        }

        // Test: Task and async return types
        public async Task AsyncTypes()
        {
            Task simpleTask = Task.Delay(100); // @test:type target="simpleTask" expect="System.Threading.Tasks.Task" kind="Local"
            Task<int> taskWithResult = Task.FromResult(42); // @test:type target="taskWithResult" expect="System.Threading.Tasks.Task<int>" kind="Local" generic="true" args="int"
            
            var awaitedResult = await taskWithResult; // @test:type target="awaitedResult" expect="int" kind="Local"
            
            ValueTask<string> valueTask = new ValueTask<string>("test"); // @test:type target="valueTask" expect="System.Threading.Tasks.ValueTask<string>" kind="Local" generic="true" args="string"
        }

        // Test: Anonymous types
        public void AnonymousTypes()
        {
            var anonymous = new { Name = "John", Age = 30 }; // Position here should resolve to anonymous type
            var anonymousArray = new[] { 
                new { Id = 1, Value = "A" }, 
                new { Id = 2, Value = "B" } 
            }; // Position here should resolve to array of anonymous type
        }

        // Test: Tuple types
        public void TupleTypes()
        {
            (int, string) simpleTuple = (1, "one"); // @test:type target="simpleTuple" expect="(int, string)" kind="Local"
            (int Id, string Name) namedTuple = (1, "John"); // @test:type target="namedTuple" expect="(int Id, string Name)" kind="Local"
            
            var tupleVar = (Count: 10, Message: "Hello"); // @test:type target="tupleVar" expect="(int Count, string Message)" kind="Local"
            
            // Nested tuples
            ((int, int), string) nestedTuple = ((1, 2), "pair"); // @test:type target="nestedTuple" expect="((int, int), string)" kind="Local"
        }

        // Test: Lambda expression types
        public void LambdaTypes()
        {
            Func<int, int> square = x => x * x; // @test:type target="square" expect="System.Func<int, int>" kind="Local" generic="true" args="int,int"
            Action<string> print = s => Console.WriteLine(s); // @test:type target="print" expect="System.Action<string>" kind="Local" generic="true" args="string"
            Predicate<int> isEven = n => n % 2 == 0; // @test:type target="isEven" expect="System.Predicate<int>" kind="Local" generic="true" args="int"
            
            // Complex lambda
            Func<string, int, Task<bool>> complexLambda = async (s, i) =>  // @test:type target="complexLambda" expect="System.Func<string, int, System.Threading.Tasks.Task<bool>>" kind="Local" generic="true" args="string,int,System.Threading.Tasks.Task<bool>"
            {
                await Task.Delay(i);
                return s.Length > i;
            };
        }

        // Test: LINQ query expressions
        public void LinqTypes()
        {
            var numbers = new[] { 1, 2, 3, 4, 5 };
            
            var query = from n in numbers
                       where n > 2
                       select n * 2; // Position here should resolve to System.Collections.Generic.IEnumerable<System.Int32>
            
            var groupQuery = from n in numbers
                            group n by n % 2 into g
                            select new { Key = g.Key, Items = g.ToList() }; // Position here should resolve to IEnumerable of anonymous type
        }

        // Test: Dynamic type
        public void DynamicType()
        {
            dynamic dynamicVar = "string"; // Position here should resolve to dynamic
            dynamicVar = 42; // Still dynamic
            dynamicVar = new { Dynamic = true }; // Still dynamic
        }

        // Test: Type inference in complex scenarios
        public void ComplexTypeInference()
        {
            var result = GetComplexData(); // Position here should resolve to Dictionary<string, List<User>>
            var firstUser = result["key"].FirstOrDefault(); // Position here should resolve to User or null
            
            var projection = result.Select(kvp => new { 
                Key = kvp.Key, 
                UserCount = kvp.Value.Count 
            }); // Position here should resolve to IEnumerable of anonymous type
        }

        private Dictionary<string, List<User>> GetComplexData()
        {
            return new Dictionary<string, List<User>>();
        }

        private class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}