# Il2CppAssemblyUnhollower
A tool to generate Managed->IL2CPP proxy assemblies from
 [Il2CppDumper](https://github.com/Perfare/Il2CppDumper )'s output.

This allows the use of IL2CPP domain and objects in it from a managed domain. 
This includes generic types and methods, arrays, and new object creation. Some things, such as ref parameters, may be horribly broken. 
 
 ## Usage
  0. Build or get a release
  1. Obtain dummy assemblies using [Il2CppDumper](https://github.com/Perfare/Il2CppDumper)
  2. Run `AssemblyUnhollower --input=<path to Il2CppDumper's dummy dll dir> --output=<output directory> --mscorlib=<path to target mscorlib>`    
       
 Resulting assemblies may be used with your favorite loader that offers a Mono domain in the IL2CPP game process.    
 This was not extensively tested on any commercial games yet.  
 Generated assemblies appear to be invalid according to .NET Core/.NET Framework, but run fine on Mono.

## Known Issues
 * Delegate support leaks some memory on each conversion, and it keeps managed delegates from being garbage collected. It's best to convert a delegate once, and then use it multiple times, than to convert the same delegate over and over.
 * Non-blittable structs can't be used in delegates
 * Types implementing interfaces, particularly IEnumerable, may be arbitrarily janky with interface methods. Additionally, using them in foreach may result in implicit casts on managed side (instead of `Cast<T>`, see below), leading to exceptions. Use `for` instead of `foreach` when possible as a workaround, or cast them to the specific interface you want to use.

## Generated assemblies caveats
 * IL2CPP types must be cast using `.Cast<T>` or `.TryCast<T>` methods instead of C-style casts or `as`.
 * When IL2CPP code requires a `System.Type`, use `T.Il2CppType` instead of `typeof(T)` (doesn't work for generic parameters, obviously)
 * Delegates need to be converted using DelegateSupport. There is also implicit conversion from appropriate Action or Func
 * IL2CPP assemblies are stripped, so some methods or even classes could be missing compared to pre-IL2CPP assemblies. This is mostly applicable to Unity assemblies.
 * Using generics with value types may lead to exceptions or crashes because of missing method bodies. If a specific value-typed generic signature was not used in original game code, it can't be used externally either.
 * No new types may be introduced into IL2CPP domain.
 
## Upcoming features (aka TODO list)
 * Unstripping engine code - restore Unity methods using managed assemblies as reference
 * Proper interface support - IL2CPP interfaces will be generated as interfaces and properly implemented by IL2CPP types
 * Research into creating IL2CPP-side classes and methods at runtime to allow for managed types in IL2CPP domain