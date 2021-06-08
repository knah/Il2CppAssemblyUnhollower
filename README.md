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

### Command-line parameter reference
```
Usage: AssemblyUnhollower [parameters]
Possible parameters:
        --help, -h, /? - Optional. Show this help
        --verbose - Optional. Produce more console output
        --input=<directory path> - Required. Directory with Il2CppDumper's dummy assemblies
        --output=<directory path> - Required. Directory to put results into
        --mscorlib=<file path> - Required. mscorlib.dll of target runtime system (typically loader's)
        --unity=<directory path> - Optional. Directory with original Unity assemblies for unstripping
        --gameassembly=<file path> - Optional. Path to GameAssembly.dll. Used for certain analyses
        --deobf-uniq-chars=<number> - Optional. How many characters per unique token to use during deobfuscation
        --deobf-uniq-max=<number> - Optional. How many maximum unique tokens per type are allowed during deobfuscation
        --deobf-analyze - Optional. Analyze deobfuscation performance with different parameter values. Will not generate assemblies.
        --blacklist-assembly=<assembly name> - Optional. Don't write specified assembly to output. Can be used multiple times
        --no-xref-cache - Optional. Don't generate xref scanning cache. All scanning will be done at runtime.
        --no-copy-unhollower-libs - Optional. Don't copy unhollower libraries to output directory
        --obf-regex=<regex> - Optional. Specifies a regex for obfuscated names. All types and members matching will be renamed
        --rename-map=<file path> - Optional. Specifies a file specifying rename map for obfuscated types and members
        --passthrough-names - Optional. If specified, names will be copied from input assemblies as-is without renaming or deobfuscation
Deobfuscation map generation mode:
        --deobf-generate - Generate a deobfuscation map for input files. Will not generate assemblies.
        --deobf-generate-asm=<assembly name> - Optional. Include this assembly for deobfuscation map generation. If none are specified, all assemblies will be included.
        --deobf-generate-new=<directory path> - Required. Specifies the directory with new (obfuscated) assemblies. The --input parameter specifies old (unobfuscated) assemblies. 
```

## Required external setup
Before certain features can be used (namely class injection and delegate conversion), some external setup is required.
 * Set `ClassInjector.Detour` to an implementation of a managed detour with semantics as described in the interface 
 * Alternatively, set `ClassInjector.DoHook` to an Action with same semantics as `DetourAttach` (signature `void**, void*`, first is a pointer to a variable containing pointer to hooked code start, second is a pointer to patch code start, a pointer to call-original code start is written to the first parameter)
 * Call `UnityVersionHandler.Initialize` with appropriate Unity version (default is 2018.4.20)

## Known Issues
 * Non-blittable structs can't be used in delegates
 * Types implementing interfaces, particularly IEnumerable, may be arbitrarily janky with interface methods. Additionally, using them in foreach may result in implicit casts on managed side (instead of `Cast<T>`, see below), leading to exceptions. Use `var` in `foreach` or use `for` instead of `foreach` when possible as a workaround, or cast them to the specific interface you want to use.
 * in/out/ref parameters on generic parameter types (like `out T` in `Dictionary.TryGetValue`) are currently broken
 * Unity unstripping only partially restores types, and certain methods can't be unstripped still; some calls to unstripped methods might result in crashes
 * Unstripped methods with array operations inside contain invalid bytecode
 * Unstripped methods with casts inside will likely throw invalid cast exceptions or produce nulls
 * Some unstripped methods are stubbed with `NotSupportedException` in cases where rewrite failed
 * Nullables have issues when returned from field/property getters and methods

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
 
 Fine-tuning:
  * `[HideFromIl2Cpp]` can be used to prevent a method from being exposed to il2cpp
 
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
 
 ## Injected components in asset bundles
 Starting with version 0.4.15.0, injected components can be used in asset bundles. Currently, deserialization for component fields is not supported. Any fields on the component will initially have their default value as defined in the mono assembly.

 How-to:
 * Your class must meet the above critereon mentioned in Class Injection.
 * Add a dummy script for your component into Unity. Remove any methods, constructors, and properties. Fields can optionally be left in for future deserialization support.
 * Apply the component to your intended objects in Unity and build the asset bundle.
 * At runtime, register your component with `RegisterTypeInIl2Cpp` before loading any objects from the asset bundle.

## Upcoming features (aka TODO list)
 * Unstripping engine code - fix current issues with unstripping failing or generating invalid bytecode
 * Proper interface support - IL2CPP interfaces will be generated as interfaces and properly implemented by IL2CPP types
 * Improve class injection to support virtual methods and interfaces
 * Improve class injection to support deserializing fields

## Used libraries
Bundled into output files:
 * [iced](https://github.com/0xd4d/iced) by 0xd4d, an x86 disassembler used for xref scanning and possibly more in the future

Used by generator itself:
 * [Mono.Cecil](https://github.com/jbevain/cecil) by jbevain, the main tool to produce assemblies