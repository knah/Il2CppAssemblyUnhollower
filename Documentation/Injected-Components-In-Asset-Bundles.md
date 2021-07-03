# Injected Components in Asset Bundles

Starting with version `0.4.15.0`, injected components can be used in asset bundles.

## How-to
 * Your class must meet the critereon mentioned in Class Injection.
 * Add a dummy script for your component into Unity. Remove any methods, constructors, and properties. Fields can optionally be left in for future deserialization support.
 * Apply the component to your intended objects in Unity and build the asset bundle.
 * At runtime, register your component with `RegisterTypeInIl2Cpp` before loading any objects from the asset bundle.

## Limitations
 * Currently, deserialization for component fields is not supported. Any fields on the component will initially have their default value as defined in the mono assembly.