// ReSharper disable all
#pragma warning disable


using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetLensMcp.Playground.Generics
{
    /// <summary>
    /// Test scenarios for GetTypeAtPosition with complex generic types
    /// </summary>
    
    // Test: Generic class with single type parameter
    public class Box<T> // Position on T should resolve to type parameter
    {
        public T Content { get; set; } // Position should resolve to T
        
        public Box(T content) // Position should resolve to T
        {
            Content = content;
        }
        
        public TNew Transform<TNew>(Func<T, TNew> transformer) // Positions should resolve to type parameters
        {
            return transformer(Content);
        }
    }

    // Test: Generic class with multiple type parameters
    public class Pair<TFirst, TSecond> // Positions should resolve to type parameters
    {
        public TFirst First { get; set; } // Position should resolve to TFirst
        public TSecond Second { get; set; } // Position should resolve to TSecond
        
        public Pair(TFirst first, TSecond second)
        {
            First = first;
            Second = second;
        }
        
        public Pair<TSecond, TFirst> Swap() // Position should resolve to Pair<TSecond, TFirst>
        {
            return new Pair<TSecond, TFirst>(Second, First);
        }
    }

    // Test: Nested generic types
    public class ComplexGenerics
    {
        // Nested generics in fields
        public List<Dictionary<string, int>> NestedField; // Position should resolve to List<Dictionary<System.String, System.Int32>>
        
        // Deeply nested generics
        public Dictionary<string, List<Tuple<int, string, DateTime>>> DeeplyNested; // Complex nested type
        
        // Generic with generic constraints
        public IEnumerable<KeyValuePair<int, List<string>>> ComplexEnumerable; // Position should resolve to complex type

        public void TestNestedGenerics()
        {
            // Test: Type resolution in nested generic instantiation
            var dict = new Dictionary<string, List<int>>(); // Position should resolve to Dictionary<System.String, List<System.Int32>>
            
            var listOfDicts = new List<Dictionary<int, string>>(); // Position should resolve to List<Dictionary<System.Int32, System.String>>
            
            var tupleList = new List<(int Id, string Name, DateTime Created)>(); // Position should resolve to List of named tuple
            
            // Test: Complex LINQ result types
            var grouped = dict.GroupBy(kvp => kvp.Value.Count)
                             .Select(g => new { Count = g.Key, Items = g.ToList() }); // Position should resolve to IEnumerable of anonymous type
        }
    }

    // Test: Generic interfaces
    public interface IContainer<T> // Position on T should resolve to type parameter
    {
        T Get(); // Position should resolve to T
        void Set(T value); // Position should resolve to T
    }

    public interface ITransformer<TInput, TOutput> // Positions should resolve to type parameters
    {
        TOutput Transform(TInput input); // Positions should resolve to type parameters
        Task<TOutput> TransformAsync(TInput input); // Position should resolve to Task<TOutput>
    }

    // Test: Generic class implementing generic interface
    public class StringContainer : IContainer<string> // Position should resolve to IContainer<System.String>
    {
        private string _value;
        
        public string Get() => _value; // Position should resolve to System.String
        public void Set(string value) => _value = value; // Position should resolve to System.String
    }

    // Test: Generic class with self-referential constraint
    public class Node<T> where T : Node<T> // Self-referential generic constraint
    {
        public T Parent { get; set; } // Position should resolve to T
        public List<T> Children { get; set; } = new List<T>(); // Position should resolve to List<T>
    }

    // Test: Covariant and contravariant generics
    public interface ICovariant<out T> // Covariant type parameter
    {
        T Get(); // Position should resolve to T
    }

    public interface IContravariant<in T> // Contravariant type parameter
    {
        void Process(T item); // Position should resolve to T
    }

    // Test: Generic method in non-generic class
    public class GenericMethods
    {
        public T Identity<T>(T value) => value; // Positions should resolve to T
        
        public TOutput Convert<TInput, TOutput>(TInput input, Func<TInput, TOutput> converter)
        {
            return converter(input); // Types should resolve to type parameters
        }
        
        public List<T> CreateList<T>(params T[] items) // Positions should resolve to T and T[]
        {
            return new List<T>(items);
        }
        
        // Test: Generic method with complex return type
        public Dictionary<TKey, List<TValue>> GroupBy<TSource, TKey, TValue>(
            IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector) // All positions should resolve to appropriate types
        {
            return source.GroupBy(keySelector)
                        .ToDictionary(g => g.Key, g => g.Select(valueSelector).ToList());
        }
    }

    // Test: Generic type with default values
    public class DefaultValueContainer<T>
    {
        public T Value { get; set; } = default(T); // Position should resolve to T, default expression should work
        
        public bool HasValue => !EqualityComparer<T>.Default.Equals(Value, default(T)); // Complex generic usage
    }

    // Test: Usage of generic types
    public class GenericUsage
    {
        public void TestGenericInstantiations()
        {
            // Simple generic instantiation
            var stringBox = new Box<string>("Hello"); // Position should resolve to Box<System.String>
            var intBox = new Box<int>(42); // Position should resolve to Box<System.Int32>
            
            // Nested generic instantiation
            var boxOfList = new Box<List<string>>(new List<string>()); // Position should resolve to Box<List<System.String>>
            var pairOfBoxes = new Pair<Box<int>, Box<string>>(intBox, stringBox); // Complex nested type
            
            // Generic method calls
            var methods = new GenericMethods();
            var result = methods.Identity<DateTime>(DateTime.Now); // Position should resolve to System.DateTime
            var converted = methods.Convert<string, int>("123", int.Parse); // Positions should resolve to System.String and System.Int32
        }
    }
}