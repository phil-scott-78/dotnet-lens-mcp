// ReSharper disable all
#pragma warning disable

using System;
using System.Collections.Generic;

namespace DotnetLensMcp.Playground.Basic
{
    /// <summary>
    /// Test scenarios for GetTypeAtPosition with classes, interfaces, and basic type resolution
    /// </summary>
    public class SimpleClass // Position on 'SimpleClass' should resolve to this type
    {
        // Test: Field types
        private string _privateField; // Position should resolve to System.String
        protected int _protectedField; // Position should resolve to System.Int32
        public DateTime PublicField; // Position should resolve to System.DateTime
        internal decimal InternalField; // Position should resolve to System.Decimal
        
        // Test: Static fields
        public static string StaticField = "Static"; // Position should resolve to System.String
        public const int ConstantField = 42; // Position should resolve to System.Int32
        public readonly Guid ReadOnlyField = Guid.NewGuid(); // Position should resolve to System.Guid

        // Test: Property types
        public string Name { get; set; } // Position should resolve to System.String
        public int Age { get; private set; } // Position should resolve to System.Int32
        public List<string> Tags { get; } = new List<string>(); // Position should resolve to List<System.String>

        // Test: Constructor parameters
        public SimpleClass(
            string name, // Position should resolve to System.String
            int age) // Position should resolve to System.Int32
        {
            Name = name;
            Age = age;
        }

        // Test: Interface implementation
        public interface ISimpleInterface // Position should resolve to interface type
        {
            string GetDescription(); // Position should resolve to System.String
            int Calculate(int x, int y); // Positions should resolve to System.Int32
        }

        // Test: Nested types
        public class NestedClass // Position should resolve to nested class type
        {
            public NestedEnum EnumProperty { get; set; } // Position should resolve to NestedEnum
        }

        public enum NestedEnum // Position should resolve to enum type
        {
            First,
            Second,
            Third
        }

        public struct NestedStruct // Position should resolve to struct type
        {
            public double X { get; set; } // Position should resolve to System.Double
            public double Y { get; set; } // Position should resolve to System.Double
        }

        // Test: Type usage in method bodies
        public void TypeUsageInMethodBody()
        {
            SimpleClass instance = new SimpleClass("Test", 25); // Position should resolve to SimpleClass
            ISimpleInterface interfaceVar = null; // Position should resolve to ISimpleInterface
            NestedClass nested = new NestedClass(); // Position should resolve to NestedClass
            NestedEnum enumValue = NestedEnum.First; // Position should resolve to NestedEnum
            NestedStruct structValue = new NestedStruct { X = 1.0, Y = 2.0 }; // Position should resolve to NestedStruct
        }

        // Test: Generic class usage
        public void GenericClassUsage()
        {
            GenericContainer<string> stringContainer = new GenericContainer<string>(); // Position should resolve to GenericContainer<System.String>
            GenericContainer<int> intContainer = new GenericContainer<int>(); // Position should resolve to GenericContainer<System.Int32>
            GenericContainer<SimpleClass> classContainer = new GenericContainer<SimpleClass>(); // Position should resolve to GenericContainer<SimpleClass>
        }
    }

    // Test: Generic class definition
    public class GenericContainer<T> // Position on T should resolve to type parameter
    {
        private T _value; // Position should resolve to T

        public T Value // Position should resolve to T
        {
            get => _value;
            set => _value = value;
        }

        public void SetValue(T newValue) // Position should resolve to T
        {
            _value = newValue;
        }

        public TResult Transform<TResult>(Func<T, TResult> transformer) // Positions should resolve to type parameters
        {
            return transformer(_value);
        }
    }

    // Test: Interface with generics
    public interface IRepository<TEntity, TKey> // Positions should resolve to type parameters
        where TEntity : class
        where TKey : IComparable<TKey>
    {
        TEntity GetById(TKey id); // Positions should resolve to type parameters
        IEnumerable<TEntity> GetAll(); // Position should resolve to IEnumerable<TEntity>
        Task<TEntity> GetByIdAsync(TKey id); // Position should resolve to Task<TEntity>
    }

    // Test: Abstract class
    public abstract class BaseEntity // Position should resolve to abstract class type
    {
        public abstract int Id { get; set; } // Position should resolve to System.Int32
        public virtual DateTime Created { get; set; } // Position should resolve to System.DateTime
        
        protected abstract void ValidateInternal(); // Position should resolve to void
    }

    // Test: Partial class
    public partial class PartialClass // Position should resolve to partial class type
    {
        public string FirstPart { get; set; } // Position should resolve to System.String
    }

    public partial class PartialClass
    {
        public string SecondPart { get; set; } // Position should resolve to System.String
    }

    // Test: Static class
    public static class HelperClass // Position should resolve to static class type
    {
        public static string FormatName(string first, string last) // Positions should resolve appropriately
        {
            return $"{first} {last}";
        }

        public static readonly Dictionary<int, string> Cache = new Dictionary<int, string>(); // Position should resolve to Dictionary type
    }

    // Test: Record types (C# 9.0+)
    public record PersonRecord(string Name, int Age); // Positions should resolve to parameter types

    public record struct PointRecord(double X, double Y); // Positions should resolve to parameter types

    // Test: Delegates
    public delegate void SimpleDelegate(string message); // Position should resolve to delegate type
    public delegate TResult GenericDelegate<in TInput, out TResult>(TInput input); // Positions should resolve to type parameters
}