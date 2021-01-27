﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace JsonWin32Generator
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection.Metadata;
    using System.Text;

    // Implements the ISignatureTypeProvider interface used as a callback by MetadataReader to create objects that represent types.
    internal class TypeRefDecoder : ISignatureTypeProvider<TypeRef, INothing?>
    {
        private readonly Dictionary<string, Api> apiNamespaceMap;
        private readonly Dictionary<TypeDefinitionHandle, TypeGenInfo> typeMap;

        internal TypeRefDecoder(Dictionary<string, Api> apiNamespaceMap, Dictionary<TypeDefinitionHandle, TypeGenInfo> typeMap)
        {
            this.apiNamespaceMap = apiNamespaceMap;
            this.typeMap = typeMap;
        }

        public TypeRef GetArrayType(TypeRef from, ArrayShape shape)
        {
            return new TypeRef.ArrayOf(from, shape);
        }

        public TypeRef GetByReferenceType(TypeRef from)
        {
            return new TypeRef.RefOf(from);
        }

        public TypeRef GetFunctionPointerType(MethodSignature<TypeRef> signature)
        {
            throw new NotImplementedException();
        }

        public TypeRef GetGenericInstantiation(TypeRef genericType, ImmutableArray<TypeRef> typeArguments)
        {
            throw new NotImplementedException();
        }

        public TypeRef GetGenericMethodParameter(INothing? genericContext, int index)
        {
            throw new NotImplementedException();
        }

        public TypeRef GetGenericTypeParameter(INothing? genericContext, int index)
        {
            throw new NotImplementedException();
        }

        public TypeRef GetModifiedType(TypeRef modifier, TypeRef unmodifiedType, bool isRequired)
        {
            throw new NotImplementedException();
        }

        public TypeRef GetPinnedType(TypeRef elementType)
        {
            throw new NotImplementedException();
        }

        public TypeRef GetPointerType(TypeRef from)
        {
            return new TypeRef.Ptr(from);
        }

        public TypeRef GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            return TypeRef.Primitive.Get(typeCode);
        }

        public TypeRef GetSZArrayType(TypeRef elementType)
        {
            throw new NotImplementedException();
        }

        public TypeRef GetTypeFromDefinition(MetadataReader mr, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            return new TypeRef.User(this.typeMap[handle]);
        }

        public TypeRef GetTypeFromReference(MetadataReader mr, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var typeRef = mr.GetTypeReference(handle);
            var @namespace = mr.GetString(typeRef.Namespace);
            var name = mr.GetString(typeRef.Name);

            if (!typeRef.ResolutionScope.IsNil)
            {
                if (typeRef.ResolutionScope.Kind == HandleKind.ModuleDefinition)
                {
                    var api = this.apiNamespaceMap[@namespace];
                    return new TypeRef.User(api.TopLevelTypes[api.TypeNameFqnMap[name]]);
                }
                else if (typeRef.ResolutionScope.Kind == HandleKind.TypeReference)
                {
                    TypeGenInfo enclosingTypeRef = this.ResolveEnclosingType(mr, (TypeReferenceHandle)typeRef.ResolutionScope);
                    Debug.Assert(@namespace.Length == 0, "I thought all nested types had empty namespaces");
                    return new TypeRef.User(enclosingTypeRef.GetNestedTypeByName(name));
                }
                else if (typeRef.ResolutionScope.Kind == HandleKind.AssemblyReference)
                {
                    // This occurs for System.Guid, not sure if it is supposed to
                    if (@namespace == "System")
                    {
                        if (name == "Guid")
                        {
                            return TypeRef.Guid.Instance;
                        }
                    }
                    else if (@namespace == "Windows.System")
                    {
                        if (name == "DispatcherQueueController")
                        {
                            return new TypeRef.UnhandledClrType(@namespace, name);
                        }
                    }

                    throw new InvalidOperationException();
                }
            }

            throw new InvalidDataException(string.Format(
                CultureInfo.InvariantCulture,
                "unexpected type reference resolution scope IsNil {0} and/or Kind {1}",
                typeRef.ResolutionScope.IsNil,
                typeRef.ResolutionScope.Kind));
        }

        public TypeRef GetTypeFromSpecification(MetadataReader mr, INothing? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            throw new NotImplementedException();
        }

        private TypeGenInfo ResolveEnclosingType(MetadataReader mr, TypeReferenceHandle typeRefHandle)
        {
            var typeRef = mr.GetTypeReference(typeRefHandle);
            var @namespace = mr.GetString(typeRef.Namespace);
            var name = mr.GetString(typeRef.Name);

            if (!typeRef.ResolutionScope.IsNil)
            {
                if (typeRef.ResolutionScope.Kind == HandleKind.ModuleDefinition)
                {
                    Api api = this.apiNamespaceMap[@namespace];
                    return api.TopLevelTypes[api.TypeNameFqnMap[name]];
                }

                if (typeRef.ResolutionScope.Kind == HandleKind.TypeReference)
                {
                    TypeGenInfo enclosingTypeRef = this.ResolveEnclosingType(mr, (TypeReferenceHandle)typeRef.ResolutionScope);
                    Debug.Assert(@namespace.Length == 0, "I thought all nested types had empty namespaces");
                    return enclosingTypeRef.GetNestedTypeByName(name);
                }
            }

            throw new NotImplementedException("unexpected ResolutionScope for enclosing type");
        }
    }
}
