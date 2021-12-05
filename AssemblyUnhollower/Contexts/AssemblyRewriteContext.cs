using System.Collections.Generic;
using Mono.Cecil;

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

        public TypeRewriteContext GetContextForOriginalType(TypeDefinition type) => myNameTypeMap[type.FullName];
        public TypeRewriteContext? TryGetContextForOriginalType(TypeDefinition type) => myOldTypeMap.TryGetValue(type, out var result) ? result : null;
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
            if (methodRef is MethodDefinition methodDef)
                return GetContextForOriginalType(methodDef.DeclaringType).GetMethodByOldMethod(methodDef).NewMethod;
            
            var newType = GlobalContext.GetNewTypeForOriginal(methodRef.DeclaringType.Resolve());
            var baseMethod = newType.GetMethodByOldMethod(methodRef.Resolve());
            var newTypeRewritten = RewriteTypeRef(methodRef.DeclaringType);
            
            var newMethodRef = new MethodReference(baseMethod.NewMethod.Name, Imports.Void, newTypeRewritten);
            newMethodRef.HasThis = methodRef.HasThis;

            foreach (var parameter in methodRef.GenericParameters)
            {
                var newOwner = parameter.DeclaringMethod == methodRef ? (IGenericParameterProvider) newMethodRef : RewriteTypeRef(parameter);
                newMethodRef.GenericParameters.Add(new GenericParameter(parameter.Name, newOwner));
            }

            newMethodRef.ReturnType = RewriteTypeRef(methodRef.ReturnType, newMethodRef);

            foreach (var parameter in methodRef.Parameters)
            {
                newMethodRef.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes,
                    RewriteTypeRef(parameter.ParameterType, newMethodRef)));
            }

            return newMethodRef;
        }
        
        public TypeReference RewriteTypeRef(TypeReference? typeRef, MethodReference? currentlyRewrittenParameterOwner = null)
        {
            if (typeRef == null) return Imports.Object;
            
            var sourceModule = NewAssembly.MainModule;

            if (typeRef is ArrayType arrayType)
            {
                if (arrayType.Rank != 1)
                    return Imports.Il2CppObjectBase;
                
                var elementType = arrayType.ElementType;

                var convertedElementType = RewriteTypeRef(elementType);
                if (elementType.IsGenericParameter)
                    return new GenericInstanceType(Imports.Il2CppArrayBase) {GenericArguments = {convertedElementType}};
                
                return new GenericInstanceType(convertedElementType.IsValueType
                    ? Imports.Il2CppStructArray
                    : Imports.Il2CppReferenceArray) {GenericArguments = {convertedElementType}};
            }

            if (typeRef is GenericParameter genericParameter)
            {
                var genericParameterDeclaringType = genericParameter.DeclaringType;
                if(genericParameterDeclaringType != null)
                    return RewriteTypeRef(genericParameterDeclaringType).GenericParameters[genericParameter.Position];

                if (currentlyRewrittenParameterOwner != null)
                    return currentlyRewrittenParameterOwner.GenericParameters[genericParameter.Position];
                
                return RewriteMethodRef(genericParameter.DeclaringMethod).GenericParameters[genericParameter.Position];
            }

            if (typeRef is ByReferenceType byRef)
                return new ByReferenceType(RewriteTypeRef(byRef.ElementType));

            if(typeRef is PointerType pointerType)
                return new PointerType(RewriteTypeRef(pointerType.ElementType));

            if (typeRef is GenericInstanceType genericInstance)
            {
                var newRef = new GenericInstanceType(RewriteTypeRef(genericInstance.ElementType, currentlyRewrittenParameterOwner));
                foreach (var originalParameter in genericInstance.GenericArguments)
                    newRef.GenericArguments.Add(RewriteTypeRef(originalParameter, currentlyRewrittenParameterOwner));

                return newRef;
            }

            if (typeRef.IsPrimitive || typeRef.FullName == "System.TypedReference")
                return sourceModule.ImportReference(TargetTypeSystemHandler.Object.Module.GetType(typeRef.Namespace, typeRef.Name));
            
            if (typeRef.FullName == "System.Void")
                return Imports.Void;

            if (typeRef.FullName == "System.Object")
                return Imports.Object;

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