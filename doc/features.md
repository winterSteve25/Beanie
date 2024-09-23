# Constructs

## Classes

Classes work just like classes in C# or Java

```csharp
public class Class : IFormatted { // implements IFormatted interface
    public i32 value;
    private Object _value2;
    
    // Constructor
    //           | will compile to `this.value = value`;
    public Class(this value, Object value2) {
        this._value2 = value2;
    }
    
    // automatically overrides IFormatted
    public void ToString() {
        Console.WriteLine(this.value); // `this` refers to the current instance
        Console.WriteLine(value2);
    }
    
    // static method just like C# and Java
    public static void Method() {
        Console.WriteLine(value2); // <- error >> static methods have no
                                   //           access to instance members
    }
}
```

Use the `sealed` keyword to prevent inheritance on classes

```csharp
[Formatted] // use attribute to automatically generate an implemention of IFormatted
public sealed class BasicallyAStruct {
    public f32 value { get, private set };
    // properties like C# but its just syntactic sugar and compiler constraints
    // compiles down to normal field with no extra getter and setter
    
    // unless custom implementation is provided for getter and/or setter
    public f32 health { 
        get, 
        set = x => {
            this = x; // `this` here refers to the underlying field
            // maybe you want to call a function here to notify the change or something
        }
    };
}
```

Use the `abstract` keyword to allow functions with no implementations that
inheritors have to implement. `abstract` and `sealed` are mutually exclusive.

```csharp
public abstract class Parent {
    public abstract void DoSomething();
}
```

## Discriminated Unions

Discriminated unions are the same as Rust enums.
The size of the type is the size of the largest variant.

```csharp
public union DiscriminatedUnion<T, E> {
    Simple(T, E, i32);
    SimpleNamed(T t, E e, i32 num);
    
    public i32 Method() {
        switch this { // pattern matching
            case Simple(t, e, 25) => {
                Console.WriteLine(t);
                Console.WriteLine(e);
            },
            _ => {},
        };
            
        return switch this {
            case Simple(_, _, i) => i,
            case SimpleNamed(_, _, num) => num,
        };
    }
}
```

## Enums

Enums are similar to Java enums.

```csharp
public enum Enumeration() {
    TYPE1;
    TYPE2;
    TYPE3;
}

public enum EnumerationWithFields(string name, f32 value) {
    TYPE1("HALLO", 1.0f);
    TYPE2("Josh", 2.0f);
    
    public void Method() {
        // string interpolation
        Console.WriteLine(f"${name}: ${value}");
    }
}
```

## Interfaces

Interfaces are basically the same as C# and Java

```csharp
public interface IInterface { 
    i32 Number { get, protected set }; 
    void Function();
    i32 Other();
}
```

## Type Aliasing

Create aliases for types using `type` keyword

```csharp
type StrPtr = Ptr<string>;
type SharedPtr<T> = ShrdPtr<T>;
```

## Namespaces

Use namespaces to separate code

```csharp
namespace Foo; // declares this file to be in the namespace Foo

namespace Bar { // so this would be Foo.Bar
    public static void FunctionInFooBar() {} // Foo.Bar.FunctionInFooBar
}
```

# Memory Management

Memory is manually managed in Beanie with optional ways to use reference counting.

## Types of Pointers

You can't have a low level language without pointers.

- `Ptr<T>` equivalent to `T*` in C/C++
    - A form of raw pointer with no management
    - Does not imply ownership
- `Tracked<T>` similar to a raw pointer
    - You can check if the location has been freed
    - Unless you unsafely manually free the underlying pointer
- `Rc<T>` equivalent to `std::shared_ptr<T>` in C++
    - **R**eference **c**ounted
    - Automatically deallocated when there is no more values of the pointer
- `WeakRc<T>` equivalent to `std::weak_ptr<T>` in C++
    - Points to the same location as a given `Rc<T>`
    - However, having this pointer will not keep the pointer alive
    - If the `Rc<T>` this `WeakRc<T>` came from died, this will no longer be valid
- `Owned<T>` alias of `Ptr<T>`
    - Exactly the same as `Ptr<T>`, a raw pointer
    - But it implies that you own the pointer
    - Errors will be reported if you try to access an owned pointer after passing it to something else
    - Warning will be reported when converting `Owned<T>` to `Ptr<T>`
- `MemPtr` equivalent to `void*` in C/C++
    - Zero type information
    - Just a pointer to a specific memory address

## Ownership

If you "own" an object/pointer, it means it is up to you to manage its memory.

If a function returns an `Owned<T>` it means it is now up to you to manage the
memory of that pointer, free it, not free it, up to the caller of the function.

If a function expects an `Owned<T>` it means it will now manage the memory of
the pointer, and you should no longer expect the pointer to remain valid.

## Stack and Heap

All objects allocated on the heap uses the `new` keyword similar to C++.
Without specification, it will be allocated on the stack.

### Returning from Functions

Often you want to return something from a function but the only way to do
that in C++ is to either copy the result from the function body,
or making it allocate on the heap, and returning the pointer,
or passing in a pointer to a stack allocated object at the caller.

None of the solutions feel particularly elegant, or intuitive.

So in Beanie you can do the following:

```csharp
public class Object {
    // ...
}

public stackalloc Object Function() {
}

public static int Main() {
    Object obj
    Object obj = Function();
}
```

This will essentially do the third solution discussed in the ways to do this in C++
and do the following:

1. The function defined will have a parameter added to the start
2. An allocation of the size of the return object will happen on the caller's stack
3. The pointer to the new stack allocation is passed to the function as the first parameter with the rest of the
   parameters
4. The returned object is instead stored onto the stack allocation passed in

## Example

```csharp
public class Object : ICopy {
    public MemPtr voidPtr;
    public Owned<string> ptrToString;
    public string str;
    
    public Object(this str) {
        voidPtr = &str; // this copies the address
        ptrToString = &str;
    }
    
    // `This` type refers to the current type
    public static void Copy(Ptr<This> other, Ptr<This> copied) {
        Object(copied, other->str.Copy())); // In-Place Construction
                                            // calls the constructor at the 
                                            // already existing allocation
                                            // assumes the allocation is
                                            // uninitialized so does NOT
                                            // do uninitialization
    }
}

public static int Main(string[] args) {
    
    Ptr<Object> objPtr = Ptr.Null;
    defer objPtr.Free(); // defer will defer the statement to the end of the scope
    
    {
        Object obj = Object("a string");
        objPtr = &obj;
        // obj is allocated on the stack so will be deallocated at the end of scope
        // objPtr will become invalid
        // a warning will be emitted but no error
    }
    
    Rc<Object> rcObj = Rc.Create(Object("Reference counted")); // reference count = 1
    
    {
        Rc<Object> copy = rcObj; // reference count = 2
    } // reference count = 1
    
    WeakRc<Object> weakRef = rcObj; // reference count still 1
    
    return 0;
}

```

# Compile Time Code

## Generics

There are 2 types of generics in Beanie.

1. Compile time generic
2. Runtime generic

### Compile Time

Compile time generics are expanded into separate types with the different types of generic parameters.

For example:

```csharp
public class CompileTimeGeneric<comp T> {
    // ...
}

public static int Main(string[] args) {
    CompileTimeGeneric<string> a;
    CompileTimeGeneric<i32> b;
    CompileTimeGeneric<i64> c;
}
```

It will compile into 3 separate classes with `T` replaced by `string`, `i32`, and `i64` each.

### Runtime

Runtime generics are similar to Java generics, where type erasure is used.

For example:

```csharp
public class CompileTimeGeneric<T> {
    // ...
}

public static int Main(string[] args) {
    CompileTimeGeneric<string> a;
    CompileTimeGeneric<i32> b;
    CompileTimeGeneric<i64> c;
}
```

When compiled, `T` will be replaced by `object` making the 3 different types the same.
However, type checking occur during compile time to make sure everything is correct.

With Runtime Generics you can use types like `List<? extends SomeInterface>` but this
will not be possible with compile time generics.

## Code Generation

Metaprogramming can make a lot of fancy magic happen.
There are a few different ways of generating code at compile time in Beanie.

### Attributes

Attributes are denoted as `[Attribute]` in Beanie similar to C#
To create one, create a sealed class that extends either `ConstructAttr` or `FunctionAttr`

```csharp
import Beanie.Compiler;

public sealed class ExampleConstructAttribute : IConstructAttr {
    public void Transform(Construct construct) {
        switch construct {
            case Class(/*...*/) => {
                // ... transform class object
                // add fields
                // add functions
                // whatever
            },
            case Union(/*...*/) => {},
            // ...
        }
    } 
}

public sealed class ExampleFunctionAttribute : FunctionAttr {
    
    private readonly i32 num;
    
    // constructor arguments are passed like so:
    // [ExampleFunctionAttribute(num)]
    public ExampleFunctionAttribute(this num) {
    }
    
    public void Transform(Function func) {
        // ... transform function
        // inject code into body at start, end, etc
        // add parameters
    } 
}
```

### Macros

Similar to rust macros, they are functions that are run during compile time
that expands into more code.

```csharp
import Beanie.Compiler;

public macro Macro(Type t, i32 num) {
    i32 num = num * num;
    
    return {
        i32 hallo = @{num}; // @{} will run code from the macro
        Console.WriteLine(f"${@{t.Name}}: ${hallo}")
    }; // this block will be inserted into where the macro is called
}
```

# Error Handling