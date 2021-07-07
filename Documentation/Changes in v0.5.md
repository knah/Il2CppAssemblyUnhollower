#Changes in v0.5
In version 0.5, a major redesign of type system was made, with the goal of making generated types align better with the actual C# type system. The initial implementation was a kind of MVP, and, as such, had a bunch of unpleasant limitations.  
The changes include the following:
 * `Il2CppSystem.String` is used in generated methods instead of `string`
   * There are implicit conversion operators defined, so this shouldn't create extra friction in code
   * String marshalling is expensive, and in some (many?) cases eliminating it might lead to performance improvements
   * Quite often the string is only read/tested, which is the primary scenario where performance would be gained
 * `object` is used in generated methods instead of `Il2CppSystem.Object`
   * This is intended to make it play better with interfaces and possible automatic injection/wrapping of managed types
   * This should also make working with boxed value types easier (at the cost of more complicated marshalling)
   * As an extension of this, this should allow removing generated types for primitives?
 * Interface types are generated as interfaces (previously they were generated as classes)
   * This would allow implementing them on injected types
   * For certain system interfaces with compatible method signatures, system types are used and no type is generated
 * For IL2CPP objects marshalled to managed, the appropriately-typed wrapper is created based on the actual object type (as opposed to declared type in method/property/field in 0.4.x)
   * This allows generating interfaces  and abstract classes as abstract (as no instances of them will be created)
   * This should allow moving virtual method dispatch to the managed side, simplifying generated code and allowing overriding/implementing methods in injected classes
 
Furthermore, there are a few ideas that are being considered now, but aren't implemented:
 * Generate lightweight ref-value-typed wrappers for IL2CPP object pointers
   * Holding references to IL2CPP objects requires using gchandles, so value types can't be used everywhere
   * However, all stack space is considered a GC root, so a pointer on stack should be considered a strong references
   * As such, generating wrappers using `ref struct` language feature (like ``Span`1``) can ensure that they don't leave the stack
   * Although value types are limited in other ways (i.e. no inheritance), this can provide a major performance boost for many native-to-managed transitions (i.e. delegates, injected methods, patches) that only require the declared type of their parameter (or can handle a few manual 0.4-style casts)
   * This would require all injection/delegate support code to be able to support both full wrapper type and the ref-struct wrappers
   * Open questions include the exact form of these generated wrappers - does CLR allow value type inheritance (if no fields are added), if not - whether base type methods should be included, handling generic types, and so on
 * Generate correctly-sized ref-struct marshalling stubs for non-blittable value types
   * This should allow generating native-to-managed wrapper (injected methods/delegates) that accept or return non-blittable value types (which was not possible in 0.4.x)
   * This might be done at runtime instead, as there's more information available at runtime
   * Open question include handling generics
   * Alternatively, this can be partially merged with the above, and ref-struct can be made a canonical representation of a non-blittable value type (generics would still be an issue though), with an option to box it
 * Given that the set of IL2CPP generic instantiations is predefined at compile time, de-generifying some types for both of the above is also an option