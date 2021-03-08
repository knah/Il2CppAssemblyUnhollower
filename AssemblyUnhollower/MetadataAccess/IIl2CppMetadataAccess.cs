using System.Collections.Generic;
using Mono.Cecil;

namespace AssemblyUnhollower.MetadataAccess
{
    public interface IIl2CppMetadataAccess : IMetadataAccess
    {
        IList<GenericInstanceType>? GetKnownInstantiationsFor(TypeDefinition genericDeclaration);
        string? GetStringStoredAtAddress(long offsetInMemory);
        MethodReference? GetMethodRefStoredAt(long offsetInMemory);
    }
}