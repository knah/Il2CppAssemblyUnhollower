# Class injection
Starting with version `0.4.0.0`, managed classes can be injected into IL2CPP domain. Currently this is fairly limited, but functional enough for GC integration and implementing custom MonoBehaviors.

## How-to
 * Your class must inherit from a non-abstract IL2CPP class.
 * You must include a constructor that takes IntPtr and passes it to base class constructor. It will be called when objects of your class are created from IL2CPP side.
 * To create your object from managed side, call base class IntPtr constructor with result of `ClassInjector.DerivedConstructorPointer<T>()`, where T is your class type, and call `ClassInjector.DerivedConstructorBody(this)` in constructor body.
 * An example of injected class is `Il2CppToMonoDelegateReference` in [DelegateSupport.cs](UnhollowerRuntimeLib/DelegateSupport.cs)
 * Call `ClassInjector.RegisterTypeInIl2Cpp<T>()` before first use of class to be injected
 * The injected class can be used normally afterwards, for example a custom MonoBehavior implementation would work with `AddComponent<T>`
 
## Fine-tuning
  * `[HideFromIl2Cpp]` can be used to prevent a method from being exposed to il2cpp
 
## Caveats
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

## Limitations
 * Interfaces can't be implemented
 * Virtual methods can't be overridden
 * Only instance methods are exposed to IL2CPP side - no fields, properties, events or static methods will be visible to IL2CPP reflection
 * Only a limited set of types is supported for method signatures
 