using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Text;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static partial class ZigWin32
{
    // Implements the ISignatureTypeProvider interface used as a callback by MetadataReader to create objects that represent types.
    class TypeRefDecoder : ISignatureTypeProvider<TypeRef, INothing?>
    {
        readonly Dictionary<string, Api> api_namespace_map;
        readonly Dictionary<TypeDefinitionHandle, TypeGenInfo> type_map;

        public TypeRefDecoder(
            Dictionary<string, Api> api_namespace_map,
            Dictionary<TypeDefinitionHandle, TypeGenInfo> type_map)
        {
            this.api_namespace_map = api_namespace_map;
            this.type_map = type_map;
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

        public TypeRef GetPrimitiveType(PrimitiveTypeCode type_code)
        {
            // TODO: use lookup table?
            return new TypeRef.Primitive(type_code);
        }

        public TypeRef GetSZArrayType(TypeRef elementType)
        {
            throw new NotImplementedException();
        }

        public TypeRef GetTypeFromDefinition(MetadataReader mr, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            return new TypeRef.User(this.type_map[handle]);
        }

        public TypeRef GetTypeFromReference(MetadataReader mr, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var type_ref = mr.GetTypeReference(handle);
            var @namespace = mr.GetString(type_ref.Namespace);
            var name = mr.GetString(type_ref.Name);

            if (!type_ref.ResolutionScope.IsNil)
            {
                if (type_ref.ResolutionScope.Kind == HandleKind.ModuleDefinition)
                {
                    var api = this.api_namespace_map[@namespace];
                    return new TypeRef.User(api.types[api.type_name_fqn_map[name]]);
                }
                else if (type_ref.ResolutionScope.Kind == HandleKind.TypeReference)
                {
                    TypeGenInfo enclosing_type_ref = this.resolveEnclosingType(mr, (TypeReferenceHandle)type_ref.ResolutionScope);
                    Debug.Assert(@namespace.Length == 0, "I thought all nested types had empty namespaces");
                    return new TypeRef.User(enclosing_type_ref.getNestedTypeByName(name));
                }
                else if (type_ref.ResolutionScope.Kind == HandleKind.AssemblyReference)
                {
                    // This occurs for System.Guid, not sure if it is supposed to
                    if (@namespace == "System")
                    {
                        if (name == "Guid")
                        {
                            return TypeRef.Guid.Instance;
                        }
                    }
                    throw new InvalidOperationException();
                }
            }
            throw new InvalidDataException(string.Format(
                "unexpected type reference resolution scope IsNil {0} and/or Kind {1}",
                type_ref.ResolutionScope.IsNil,
                type_ref.ResolutionScope.Kind));
        }

        public TypeRef GetTypeFromSpecification(MetadataReader mr, INothing? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            throw new NotImplementedException();
        }

        private TypeGenInfo resolveEnclosingType(MetadataReader mr, TypeReferenceHandle type_ref_handle)
        {
            var type_ref = mr.GetTypeReference(type_ref_handle);
            var @namespace = mr.GetString(type_ref.Namespace);
            var name = mr.GetString(type_ref.Name);

            if (!type_ref.ResolutionScope.IsNil)
            {
                if (type_ref.ResolutionScope.Kind == HandleKind.ModuleDefinition)
                {
                    Api api = this.api_namespace_map[@namespace];
                    return api.types[api.type_name_fqn_map[name]];
                }

                if (type_ref.ResolutionScope.Kind == HandleKind.TypeReference)
                {
                    TypeGenInfo enclosing_type_ref = this.resolveEnclosingType(mr, (TypeReferenceHandle)type_ref.ResolutionScope);
                    Debug.Assert(@namespace.Length == 0, "I thought all nested types had empty namespaces");
                    return enclosing_type_ref.getNestedTypeByName(name);
                }
            }

            throw new NotImplementedException("unexpected ResolutionScope for enclosing type");
        }
    }

    // When formatting a type to Zig, we need to know if the type is the top-level type or
    // a child type of something like a pointer.  This is so we can generate the correct
    // `void` type.  Top level void types become void, but pointers to void types must
    // become pointers to the `opaque{}` type.
    enum DepthContext
    {
        top_level,
        child,
    }

    abstract class TypeRef
    {
        public abstract void addTypeRefs(TypeRefScope scope);

        public abstract void formatZigType(StringBuilder builder, DepthContext depth_context);

        public class ArrayOf : TypeRef
        {
            public readonly TypeRef element_type;
            public readonly ArrayShape shape;

            public ArrayOf(TypeRef element_type, ArrayShape shape)
            {
                this.element_type = element_type;
                this.shape = shape;
            }

            public override void addTypeRefs(TypeRefScope scope)
            {
                this.element_type.addTypeRefs(scope);
            }

            public override void formatZigType(StringBuilder builder, DepthContext depth_context)
            {
                // TODO: take ArrayShape into account
                builder.Append("[*]");
                this.element_type.formatZigType(builder, DepthContext.child);
            }
        }

        public class RefOf : TypeRef
        {
            public readonly TypeRef target_type;

            public RefOf(TypeRef target_type)
            {
                this.target_type = target_type;
            }

            public override void addTypeRefs(TypeRefScope scope)
            {
                this.target_type.addTypeRefs(scope);
            }

            public override void formatZigType(StringBuilder builder, DepthContext depth_context)
            {
                // TODO: do I need to surround it with parens?
                builder.Append("*(");
                this.target_type.formatZigType(builder, DepthContext.child);
                builder.Append(')');
            }
        }

        public class Ptr : TypeRef
        {
            public readonly TypeRef target_type;

            public Ptr(TypeRef target_type)
            {
                this.target_type = target_type;
            }

            public override void addTypeRefs(TypeRefScope scope)
            {
                this.target_type.addTypeRefs(scope);
            }

            public override void formatZigType(StringBuilder builder, DepthContext depth_context)
            {
                // TODO: do I need to surround it with parens?
                builder.Append("*(");
                this.target_type.formatZigType(builder, DepthContext.child);
                builder.Append(')');
            }
        }

        public class User : TypeRef
        {
            public readonly TypeGenInfo info;

            public User(TypeGenInfo info)
            {
                this.info = info;
            }

            public override void addTypeRefs(TypeRefScope scope)
            {
                scope.addTypeRef(this.info);
            }

            public override void formatZigType(StringBuilder builder, DepthContext depth_context)
            {
                builder.AppendFormat("{0}", this.info.name);
            }
        }

        public class Primitive : TypeRef
        {
            public readonly PrimitiveTypeCode code;

            // TODO: use lookup table instead?
            public Primitive(PrimitiveTypeCode code)
            {
                this.code = code;
            }

            public override void addTypeRefs(TypeRefScope scope)
            {
            }

            public override void formatZigType(StringBuilder builder, DepthContext depth_context)
            {
                builder.Append(this.code switch
                {
    #pragma warning disable SA1025 // Code should not contain multiple whitespace in a row
                    PrimitiveTypeCode.Void      => (depth_context == DepthContext.top_level) ? "void" : "opaque{}",
                    PrimitiveTypeCode.Boolean   => "bool",
                    PrimitiveTypeCode.Char      => "u8",
                    PrimitiveTypeCode.SByte     => "i8",
                    PrimitiveTypeCode.Byte      => "u8",
                    PrimitiveTypeCode.Int16     => "i16",
                    PrimitiveTypeCode.UInt16    => "u16",
                    PrimitiveTypeCode.Int32     => "i32",
                    PrimitiveTypeCode.UInt32    => "u32",
                    PrimitiveTypeCode.Int64     => "i64",
                    PrimitiveTypeCode.UInt64    => "u64",
                    PrimitiveTypeCode.Single    => "f32",
                    PrimitiveTypeCode.Double    => "f64",
                    PrimitiveTypeCode.String    => "[]const u8",
                    PrimitiveTypeCode.TypedReference => "??TypedReference???",
                    PrimitiveTypeCode.IntPtr    => "isize",
                    PrimitiveTypeCode.UIntPtr   => "usize",
                    PrimitiveTypeCode.Object    => "???Object???",
    #pragma warning restore SA1025 // Code should not contain multiple whitespace in a row
                    _ => throw new InvalidOperationException(),
                });
            }
        }

        public class Guid : TypeRef
        {
            public static Guid Instance = new Guid();

            private Guid()
            {
            }

            public override void addTypeRefs(TypeRefScope scope)
            {
                // TODO: add a type reference for guid!!
            }

            public override void formatZigType(StringBuilder builder, DepthContext depth_context)
            {
                // todo: use an actual type for a guid?
                builder.AppendFormat("[16]u8");
            }
        }
    }

    interface INothing
    {
    }
}
