# Il2CppAssemblyUnhollower
A tool to generate Managed->IL2CPP proxy assemblies from
 [Il2CppDumper](https://github.com/Perfare/Il2CppDumper )'s output.

This allows the use of IL2CPP domain and objects in it from a managed domain. 
This includes generic types and methods, arrays, and new object creation. Some things may be horribly broken. 
 
 ## Usage
  0. Build or get a release
  1. Obtain dummy assemblies using [Il2CppDumper](https://github.com/Perfare/Il2CppDumper)
  2. Run `AssemblyUnhollower --input=<path to Il2CppDumper's dummy dll dir> --output=<output directory> --mscorlib=<path to target mscorlib>`    
       
 Resulting assemblies may be used with your favorite loader that offers a Mono domain in the IL2CPP game process, such as [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader).    
 This appears to be working reasonably well for Unity 2018.4.x games, but more extensive testing is required.  
 Generated assemblies appear to be invalid according to .NET Core/.NET Framework, but run fine on Mono.

## Known Issues
 * Non-blittable structs can't be used in delegates
 * Types implementing interfaces, particularly IEnumerable, may be arbitrarily janky with interface methods. Additionally, using them in foreach may result in implicit casts on managed side (instead of `Cast<T>`, see below), leading to exceptions. Use `for` instead of `foreach` when possible as a workaround, or cast them to the specific interface you want to use.
 * in/out/ref parameters on generic parameter types (like `out T` in `Dictionary.TryGetValue`) are currently broken
 * Unity unstripping is currently limited to enums and InternalCall methods

## Generated assemblies caveats
 * IL2CPP types must be cast using `.Cast<T>` or `.TryCast<T>` methods instead of C-style casts or `as`.
 * When IL2CPP code requires a `System.Type`, use `Il2CppType.Of<T>()` instead of `typeof(T)`
 * For IL2CPP delegate types, use the implicit conversion from `System.Action` or `System.Func`, like this: `UnityAction a = new Action(() => {})` or `var x = (UnityAction) new Action(() => {})`
 * IL2CPP assemblies are stripped, so some methods or even classes could be missing compared to pre-IL2CPP assemblies. This is mostly applicable to Unity assemblies.
 * Using generics with value types may lead to exceptions or crashes because of missing method bodies. If a specific value-typed generic signature was not used in original game code, it can't be used externally either.

## Class injection
Starting with version 0.4.0.0, managed classes can be injected into IL2CPP domain. Currently this is fairly limited, but functional enough for GC integration and implementing custom MonoBehaviors.

How-to:
 * Your class must inherit from a non-abstract IL2CPP class.
 * You must include a constructor that takes IntPtr and passes it to base class constructor. It will be called when objects of your class are created from IL2CPP side.
 * To create your object from managed side, call base class IntPtr constructor with result of `ClassInjector.DerivedConstructorPointer<T>()`, where T is your class type, and call `ClassInjector.DerivedConstructorBody(this)` in constructor body.
 * An example of injected class is `Il2CppToMonoDelegateReference` in [DelegateSupport.cs](UnhollowerRuntimeLib/DelegateSupport.cs)
 * Call `ClassInjector.RegisterTypeInIl2Cpp<T>()` before first use of class to be injected
 * The injected class can be used normally afterwards, for example a custom MonoBehavior implementation would work with `AddComponent<T>`
 
Caveats:
 * Injected class instances are handled by IL2CPP garbage collection. This means that an object may be collected even if it's referenced from managed domain. Attempting to use that object afterwards will result in `ObjectCollectedException`. Conversely, managed representation of injected object will not be garbage collected as long as it's referenced from IL2CPP domain.
 * It might be possible to create a cross-domain reference loop that will prevent objects from being garbage collected. Avoid doing anything that will result in injected class instances (indirectly) storing references to itself. The simplest example of how to leak memory is this:
```c#
class Injected: Il2CppSystem.Object {
    Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object> list = new ...;
    public Injected() {
        list.Add(this); // reference to itself through an IL2CPP list. This will prevent both this and list from being garbage collected, ever.
    }
}
```

Limitations:
 * Interfaces can't be implemented
 * Virtual methods can't be overridden
 * Only instance methods are exposed to IL2CPP side - no fields, properties, events or static methods will be visible to IL2CPP reflection
 * Only a limited set of types is supported for method signatures
 
## Upcoming features (aka TODO list)
 * Unstripping engine code - restore more Unity methods using managed assemblies as reference
 * Proper interface support - IL2CPP interfaces will be generated as interfaces and properly implemented by IL2CPP types
 * Improve class injection to support virtual methods and interfaces