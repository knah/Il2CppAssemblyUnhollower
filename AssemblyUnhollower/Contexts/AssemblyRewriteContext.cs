using System.Collections.Generic;
using Mono.Cecil;
using UnhollowerBaseLib;

namespace AssemblyUnhollower.Contexts
{
    public class AssemblyRewriteContext
    {
        public readonly RewriteGlobalContext GlobalContext;
        
        public readonly AssemblyDefinition OriginalAssembly;
        public readonly AssemblyDefinition NewAssembly;

        private readonly Dictionary<TypeDefinition, TypeRewriteContext> myOldTypeMap = new Dictionary<TypeDefinition, TypeRewriteContext>();
        private readonly Dictionary<TypeDefinition, TypeRewriteContext> myNewTypeMap = new Dictionary<TypeDefinition, TypeRewriteContext>();
        private readonly Dictionary<string, TypeRewriteContext> myNameTypeMap = new Dictionary<string, TypeRewriteContext>();

        public readonly AssemblyKnownImports Imports;

        public IEnumerable<TypeRewriteContext> Types => myOldTypeMap.Values;

        public AssemblyRewriteContext(RewriteGlobalContext globalContext, AssemblyDefinition originalAssembly, AssemblyDefinition newAssembly)
        {
            OriginalAssembly = originalAssembly;
            NewAssembly = newAssembly;
            GlobalContext = globalContext;

            Imports = AssemblyKnownImports.For(newAssembly.MainModule, globalContext);
        }

        public TypeRewriteContext GetContextForOriginalType(TypeDefinition type) => myOldTypeMap[type];
        public TypeRewriteContext GetContextForNewType(TypeDefinition type) => myNewTypeMap[type];

        public void RegisterTypeRewrite(TypeRewriteContext context)
        {
            if (context.OriginalType != null)
                myOldTypeMap[context.OriginalType] = context;
            myNewTypeMap[context.NewType] = context;
            myNameTypeMap[(context.OriginalType ?? context.NewType).FullName] = context;
        }

        public MethodReference RewriteMethodRef(MethodReference methodRef)
        {
            var newType = GlobalContext.GetNewTypeForOriginal(methodRef.DeclaringType.Resolve());
            return newType.GetMethodByOldMethod(methodRef.Resolve()).NewMethod; 
        }
        
        public TypeReference RewriteTypeRef(TypeReference? typeRef)
        {
            if (typeRef == null) return Imports.Il2CppObjectBase;
            
            var sourceModule = NewAssembly.MainModule;

            if (typeRef is ArrayType arrayType)
            {
                if (arrayType.Rank != 1)
                    return Imports.Il2CppObjectBase;
                
                var elementType = typeRef.GetElementType();
                if (elementType.FullName == "System.String")
                    return sourceModule.ImportReference(typeof(Il2CppStringArray));

                var convertedElementType = RewriteTypeRef(elementType);
                if (elementType.IsGenericParameter)
                    return new GenericInstanceType(sourceModule.ImportReference(typeof(Il2CppArrayBase<>))) {GenericArguments = {convertedElementType}};
                
                return new GenericInstanceType(sourceModule.ImportReference(convertedElementType.IsValueType
                    ? typeof(Il2CppStructArray<>)
                    : typeof(Il2CppReferenceArray<>))) {GenericArguments = {convertedElementType}};
            }

            if (typeRef is GenericParameter genericParameter)
            {
                var genericParameterDeclaringType = genericParameter.DeclaringType;
                if(genericParameterDeclaringType != null)
                    return RewriteTypeRef(genericParameterDeclaringType).GenericParameters[genericParameter.Position];

                return RewriteMethodRef(genericParameter.DeclaringMethod).GenericParameters[genericParameter.Position];
            }

            if (typeRef is ByReferenceType byRef)
                return new ByReferenceType(RewriteTypeRef(byRef.ElementType));

            if(typeRef is PointerType pointerType)
                return new PointerType(RewriteTypeRef(pointerType.ElementType));

            if (typeRef is GenericInstanceType genericInstance)
            {
                var newRef = new GenericInstanceType(RewriteTypeRef(genericInstance.ElementType));
                foreach (var originalParameter in genericInstance.GenericArguments)
                    newRef.GenericArguments.Add(RewriteTypeRef(originalParameter));

                return newRef;
            }

            if (typeRef.IsPrimitive || typeRef.FullName == "System.TypedReference")
                return sourceModule.ImportReference(TargetTypeSystemHandler.Object.Module.GetType(typeRef.Namespace, typeRef.Name));
            
            if (typeRef.FullName == "System.Void")
                return Imports.Void;

            if (typeRef.FullName == "System.String")
                return Imports.String;

            if(typeRef.FullName == "System.Object")
                return sourceModule.ImportReference(GlobalContext.GetAssemblyByName("mscorlib").GetTypeByName("System.Object").NewType);
            
            var originalTypeDef = typeRef.Resolve();
            var targetAssembly = GlobalContext.GetNewAssemblyForOriginal(originalTypeDef.Module.Assembly);
            var target = targetAssembly.GetContextForOriginalType(originalTypeDef).NewType;

            return sourceModule.ImportReference(target);
        }

        public TypeRewriteContext GetTypeByName(string name)
        {
            return myNameTypeMap[name];
        }
        
        public TypeRewriteContext? TryGetTypeByName(string name)
        {
            return myNameTypeMap.TryGetValue(name, out var result) ? result : null;
        }
    }
}