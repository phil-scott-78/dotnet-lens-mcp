// ReSharper disable all
#pragma warning disable



using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetLensMcp.Playground.Generics
{
    /// <summary>
    /// Test scenarios for GetTypeAtPosition with generic constraints
    /// </summary>
    
    // Test: Class constraint
    public class Repository<T> where T : class // T must be a reference type
    {
        public T FindById(int id) => default(T); // Position should resolve to T (constrained to class)
        public List<T> GetAll() => new List<T>(); // Position should resolve to List<T>
    }

    // Test: Struct constraint
    public class ValueContainer<T> where T : struct // T must be a value type
    {
        public T Value { get; set; } // Position should resolve to T (constrained to struct)
        public T? NullableValue { get; set; } // Position should resolve to Nullable<T>
    }

    // Test: New constraint
    public class Factory<T> where T : new() // T must have parameterless constructor
    {
        public T CreateNew() => new T(); // Position should resolve to T with new() constraint
        
        public List<T> CreateMany(int count) // Position should resolve to List<T>
        {
            var list = new List<T>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new T());
            }
            return list;
        }
    }

    // Test: Interface constraint
    public interface IEntity
    {
        int Id { get; set; }
        DateTime Created { get; set; }
    }

    public class EntityRepository<T> where T : IEntity // T must implement IEntity
    {
        public T GetById(int id) => default(T); // Position should resolve to T : IEntity
        public IEnumerable<T> GetRecent() => new List<T>(); // Position should resolve to IEnumerable<T>
    }

    // Test: Base class constraint
    public abstract class Animal
    {
        public abstract string Name { get; }
    }

    public class Zoo<T> where T : Animal // T must derive from Animal
    {
        private List<T> _animals = new List<T>(); // Position should resolve to List<T> where T : Animal
        
        public void Add(T animal) => _animals.Add(animal); // Position should resolve to T : Animal
        public T FindByName(string name) => _animals.Find(a => a.Name == name); // Position should resolve to T : Animal
    }

    // Test: Multiple constraints
    public class ComplexConstraint<T> 
        where T : class, IEntity, IComparable<T>, new() // Multiple constraints
    {
        public T Create() => new T(); // Position should resolve to T with all constraints
        public int Compare(T first, T second) => first.CompareTo(second); // Using IComparable<T> constraint
        public void Process(T entity) // Position should resolve to T with all constraints
        {
            var id = entity.Id; // Using IEntity constraint
            var newInstance = new T(); // Using new() constraint
        }
    }

    // Test: Type parameter constraints on methods
    public class ConstraintMethods
    {
        public T Max<T>(T first, T second) where T : IComparable<T> // T must implement IComparable<T>
        {
            return first.CompareTo(second) > 0 ? first : second; // Position should resolve to T : IComparable<T>
        }
        
        public void ProcessEntity<T>(T entity) where T : class, IEntity // Multiple constraints on method
        {
            Console.WriteLine($"Processing entity {entity.Id}"); // Using constraint members
        }
        
        public TResult Transform<TInput, TResult>(TInput input) 
            where TInput : class 
            where TResult : class, new() // Multiple type parameters with constraints
        {
            return new TResult(); // Positions should resolve with constraints
        }
    }

    // Test: Constraint inheritance
    public interface IAdvancedEntity : IEntity
    {
        string Name { get; set; }
    }

    public class AdvancedRepository<T> : EntityRepository<T> 
        where T : IAdvancedEntity // More specific constraint than base
    {
        public T FindByName(string name) => default(T); // Position should resolve to T : IAdvancedEntity
    }

    // Test: Enum constraint (C# 7.3+)
    public class EnumHelper<T> where T : Enum // T must be an enum
    {
        public string[] GetNames() => Enum.GetNames(typeof(T)); // Position should resolve to T : Enum
        public T Parse(string value) => (T)Enum.Parse(typeof(T), value); // Position should resolve to T : Enum
    }

    // Test: Unmanaged constraint (C# 7.3+)
    public class UnmanagedContainer<T> where T : unmanaged // T must be unmanaged type
    {
        public unsafe void ProcessPointer(T* ptr) // Position should resolve to T* (unmanaged)
        {
            T value = *ptr; // Position should resolve to T (unmanaged)
        }
        
        public Span<T> AsSpan(T[] array) => new Span<T>(array); // Position should resolve to Span<T>
    }

    // Test: Delegate constraint
    public class DelegateContainer<T> where T : Delegate // T must be a delegate
    {
        public T Handler { get; set; } // Position should resolve to T : Delegate
        public void Invoke(T del) => del.DynamicInvoke(); // Using Delegate members
    }

    // Test: Complex nested constraints
    public class NestedConstraints<TOuter, TInner> 
        where TOuter : IList<TInner>
        where TInner : IComparable<TInner>, new()
    {
        public void Sort(TOuter list) // Position should resolve to TOuter : IList<TInner>
        {
            // Bubble sort using IComparable
            for (int i = 0; i < list.Count - 1; i++)
            {
                for (int j = 0; j < list.Count - i - 1; j++)
                {
                    if (list[j].CompareTo(list[j + 1]) > 0)
                    {
                        TInner temp = list[j]; // Position should resolve to TInner
                        list[j] = list[j + 1];
                        list[j + 1] = temp;
                    }
                }
            }
        }
    }

    // Test: Usage of constrained generics
    public class ConstraintUsage
    {
        public void TestConstrainedTypes()
        {
            // Class constraint
            var repo = new Repository<string>(); // Position should resolve to Repository<System.String>
            // var invalidRepo = new Repository<int>(); // This would be invalid - int is not a class
            
            // Struct constraint
            var valueContainer = new ValueContainer<int>(); // Position should resolve to ValueContainer<System.Int32>
            // var invalidValue = new ValueContainer<string>(); // This would be invalid - string is not a struct
            
            // New constraint
            var factory = new Factory<List<int>>(); // Position should resolve to Factory<List<System.Int32>>
            var created = factory.CreateNew(); // Position should resolve to List<System.Int32>
            
            // Multiple constraints
            var complex = new ComplexConstraint<TestEntity>(); // Position should resolve to ComplexConstraint<TestEntity>
            var entity = complex.Create(); // Position should resolve to TestEntity
        }
        
        private class TestEntity : IEntity, IComparable<TestEntity>
        {
            public int Id { get; set; }
            public DateTime Created { get; set; }
            
            public int CompareTo(TestEntity other) => Id.CompareTo(other?.Id ?? 0);
        }
    }
}