# Command-Line Usage

 ## Basic Usage
  0. Build or get a release
  1. Obtain dummy assemblies using [Il2CppDumper](https://github.com/Perfare/Il2CppDumper)
  2. Run `AssemblyUnhollower --input=<path to Il2CppDumper's dummy dll dir> --output=<output directory> --mscorlib=<path to target mscorlib>`    
       
 Resulting assemblies may be used with your favorite loader that offers a Mono domain in the IL2CPP game process, such as [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader).    
 This appears to be working reasonably well for Unity 2018.4.x games, but more extensive testing is required.  
 Generated assemblies appear to be invalid according to .NET Core/.NET Framework, but run fine on Mono.

## Command-line parameter reference


| Parameter | Explanation |
| --------- | ----------- |
| `--help`, -h, /? | Optional. Show this help |
| `--verbose` | Optional. Produce more console output |
| `--input=<directory path>` | Required. Directory with Il2CppDumper's dummy assemblies |
| `--output=<directory path>` | Required. Directory to put results into |
| `--mscorlib=<file path>` | Required. mscorlib.dll of target runtime system (typically loader's) |
| `--unity=<directory path>` | Optional. Directory with original Unity assemblies for unstripping |
| `--gameassembly=<file path>` | Optional. Path to GameAssembly.dll. Used for certain analyses |
| `--deobf-uniq-chars=<number>` | Optional. How many characters per unique token to use during deobfuscation |
| `--deobf-uniq-max=<number>` | Optional. How many maximum unique tokens per type are allowed during deobfuscation |
| `--deobf-analyze` | Optional. Analyze deobfuscation performance with different parameter values. Will not generate assemblies. |
| `--blacklist-assembly=<assembly name>` | Optional. Don't write specified assembly to output. Can be used multiple times |
| `--no-xref-cache` | Optional. Don't generate xref scanning cache. All scanning will be done at runtime. |
| `--no-copy-unhollower-libs` | Optional. Don't copy unhollower libraries to output directory |
| `--obf-regex=<regex>` | Optional. Specifies a regex for obfuscated names. All types and members matching will be renamed |
| `--rename-map=<file path>` | Optional. Specifies a file specifying rename map for obfuscated types and members |
| `--passthrough-names` | Optional. If specified, names will be copied from input assemblies as-is without renaming or deobfuscation |
| Deobfuscation map generation mode: | |
| `--deobf-generate` | Generate a deobfuscation map for input files. Will not generate assemblies. |
| `--deobf-generate-asm=<assembly name>` | Optional. Include this assembly for deobfuscation map generation. If none are specified, all assemblies will be included. |
| `--deobf-generate-new=<directory path>` | Required. Specifies the directory with new (obfuscated) assemblies. The `--input` parameter specifies old (unobfuscated) assemblies. |


## Required external setup
Before certain features can be used (namely class injection and delegate conversion), some external setup is required.
 * Set `ClassInjector.Detour` to an implementation of a managed detour with semantics as described in the interface 
 * Alternatively, set `ClassInjector.DoHook` to an Action with same semantics as `DetourAttach` (signature `void**, void*`, first is a pointer to a variable containing pointer to hooked code start, second is a pointer to patch code start, a pointer to call-original code start is written to the first parameter)
 * Call `UnityVersionHandler.Initialize` with appropriate Unity version (default is 2018.4.20)
