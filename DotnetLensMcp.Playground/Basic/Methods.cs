// ReSharper disable all
#pragma warning disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace DotnetLensMcp.Playground.Basic
{
    /// <summary>
    /// Test scenarios for GetTypeAtPosition with method return types and parameters
    /// </summary>
    public class Methods
    {
        // Test: Method return type - simple types
        public string GetString() // Position on 'string' should resolve to System.String
        {
            return "Hello";
        }

        public int GetNumber() // Position on 'int' should resolve to System.Int32
        {
            return 42;
        }

        public void VoidMethod() // Position on 'void' should resolve to System.Void
        {
            Console.WriteLine("Void method");
        }

        // Test: Method return type - generic types
        public List<string> GetStringList() // Position should resolve to System.Collections.Generic.List<System.String>
        {
            return new List<string> { "a", "b", "c" };
        }

        public Dictionary<int, User> GetUserDictionary() // Position should resolve to Dictionary<System.Int32, User>
        {
            return new Dictionary<int, User>();
        }

        // Test: Method return type - async methods
        public async Task<string> GetStringAsync() // Position should resolve to System.Threading.Tasks.Task<System.String>
        {
            await Task.Delay(100);
            return "Async result";
        }

        public async Task DoWorkAsync() // Position should resolve to System.Threading.Tasks.Task
        {
            await Task.Delay(100);
        }

        public async ValueTask<int> GetValueAsync() // Position should resolve to System.Threading.Tasks.ValueTask<System.Int32>
        {
            await Task.Delay(100);
            return 42;
        }

        // Test: Method parameters
        public void MethodWithParameters(
            string text, // Position should resolve to System.String
            int number, // Position should resolve to System.Int32
            DateTime? nullableDate, // Position should resolve to System.Nullable<System.DateTime>
            List<User> users) // Position should resolve to List<User>
        {
            // Method body
        }

        // Test: Generic method
        public T GenericMethod<T>(T input) where T : class // Position on T should resolve to type parameter
        {
            return input;
        }

        public TResult GenericWithConstraints<TInput, TResult>(TInput input) 
            where TInput : IComparable<TInput>
            where TResult : class, new() // Positions should resolve to respective type parameters
        {
            return new TResult();
        }

        // Test: Method with tuple return
        public (int Count, string Message) GetTuple() // Position should resolve to ValueTuple<System.Int32, System.String>
        {
            return (10, "Hello");
        }

        public (bool Success, T Result, string Error) TryGet<T>(string key) // Position should resolve to ValueTuple with generic
        {
            return (true, default(T), null);
        }

        // Test: Local functions
        public void MethodWithLocalFunction()
        {
            // Local function with return type
            int LocalAdd(int a, int b) // Position should resolve to System.Int32
            {
                return a + b;
            }

            // Local async function
            async Task<string> LocalAsync() // Position should resolve to Task<System.String>
            {
                await Task.Delay(10);
                return "Local";
            }

            var result = LocalAdd(1, 2); // Position should resolve to System.Int32
            var asyncResult = LocalAsync(); // Position should resolve to Task<System.String>
        }

        // Test: Iterator methods
        public IEnumerable<int> GetNumbers() // Position should resolve to IEnumerable<System.Int32>
        {
            yield return 1;
            yield return 2;
            yield return 3;
        }

        public async IAsyncEnumerable<string> GetStringsAsync() // Position should resolve to IAsyncEnumerable<System.String>
        {
            await Task.Delay(10);
            yield return "one";
            yield return "two";
        }

        // Test: Expression-bodied members
        public string Name => "Expression"; // Position should resolve to System.String
        
        public int Calculate(int x) => x * 2; // Position should resolve to System.Int32
        
        public Task<bool> IsValidAsync(string input) => Task.FromResult(!string.IsNullOrEmpty(input)); // Position should resolve to Task<System.Boolean>

        // Test: Method overloads
        public string Process(int value) => value.ToString(); // Position should resolve to System.String
        
        public string Process(string value) => value.ToUpper(); // Position should resolve to System.String
        
        public string Process(int x, int y) => $"{x},{y}"; // Position should resolve to System.String

        // Test: Delegates and events
        public delegate void MyDelegate(string message); // Position should resolve to delegate type
        
        public event EventHandler<EventArgs> MyEvent; // Position should resolve to EventHandler<EventArgs>
        
        public event Action<int> SimpleEvent; // Position should resolve to Action<System.Int32>

        // Test: Properties with different accessors
        public string ReadOnlyProperty { get; } = "ReadOnly"; // Position should resolve to System.String
        
        public int AutoProperty { get; set; } // Position should resolve to System.Int32
        
        public List<string> InitOnlyProperty { get; init; } // Position should resolve to List<System.String>

        private string _backingField;
        public string PropertyWithBacking // Position should resolve to System.String
        {
            get => _backingField;
            set => _backingField = value ?? string.Empty;
        }

        // Helper class for tests
        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime Created { get; set; }
        }
    }
}