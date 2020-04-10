# Il2CppAssemblyUnhollower
A tool to generate Managed->IL2CPP proxy assemblies from
 [Il2CppDumper](https://github.com/Perfare/Il2CppDumper )'s output.

This allows the use of IL2CPP domain and objects in it from a managed domain. 
This includes generic types and methods, arrays, and new object creation. Some things, such as ref parameters, may be horribly broken. 
 
 ## Usage
  0. Build or get a release
  1. Obtain dummy assemblies using [Il2CppDumper](https://github.com/Perfare/Il2CppDumper)
  2. Run `AssemblyUnhollower <path to Il2CppDumper's dummy dll dir> <output directory> <path to target mscorlib>`    
  3. Copy UnhollowerBaseLib from Unhollower's dir to output directory
  4. Copy DelegateSupport library (found in release files) to output dir (optional, if you want to pass managed delegates to IL2CPP code)
       
 Resulting assemblies may be used with your favorite loader that offers a managed domain in the IL2CPP game process.    
 This was not extensively tested on any commercial games yet. Generated assemblies were tested with Mono and may break horribly on .NET Core or .NET Framework.

## Known Issues
 * Generic types with system types in generic arguments (int/string/etc) don't work
 * Arrays of systems types (int/string/etc) don't work
 * Delegates require external support for now

## Generated assemblies caveats
 * IL2CPP types must be cast using `.Cast<T>` or `.TryCast<T>` methods instead of C-style casts or `as`.
 * When IL2CPP code requires a `System.Type`, use `T.Il2CppType` instead of `typeof(T)` (doesn't work for generic parameters, obviously)
 * IL2CPP assemblies are stripped, so some methods or even classes could be missing compared to pre-IL2CPP assemblies. This is mostly applicable to Unity assemblies.
 * Using generics with value types may lead to exceptions or crashes because of missing method bodies. If a specific value-typed generic signature was not used in original game code, it can't be used externally either.
 * No new types may be introduced into IL2CPP domain.