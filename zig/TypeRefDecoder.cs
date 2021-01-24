using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static partial class ZigWin32
{
    // Implements the ISignatureTypeProvider interface used as a callback by MetadataReader to create objects that represent types.
    internal class TypeRefDecoder : ISignatureTypeProvider<TypeRef, INothing?>
    {
        private readonly Dictionary<string, TypeGenInfo> no_namespace_type_map;
        private readonly Dictionary<string, Api> api_namespace_map;
        private readonly Dictionary<TypeDefinitionHandle, TypeGenInfo> type_map;

        public TypeRefDecoder(
            Dictionary<string, TypeGenInfo> no_namespace_type_map,
            Dictionary<string, Api> api_namespace_map,
            Dictionary<TypeDefinitionHandle, TypeGenInfo> type_map)
        {
            this.no_namespace_type_map = no_namespace_type_map;
            this.api_namespace_map = api_namespace_map;
            this.type_map = type_map;
        }

        public TypeRef GetArrayType(TypeRef from, ArrayShape shape)
        {
            return new ArrayTypeRef(from, shape);
        }

        public TypeRef GetByReferenceType(TypeRef from)
        {
            return new ReferenceTypeRef(from);
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
            return new PointerTypeRef(from);
        }

        public TypeRef GetPrimitiveType(PrimitiveTypeCode type_code)
        {
            // TODO: use lookup table?
            return new PrimitiveTypeRef(type_code);
        }

        public TypeRef GetSZArrayType(TypeRef elementType)
        {
            throw new NotImplementedException();
        }

        public TypeRef GetTypeFromDefinition(MetadataReader mr, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            return new CustomTypeRef(this.type_map[handle]);
        }

        public TypeRef GetTypeFromReference(MetadataReader mr, TypeReferenceHandle handle, byte rawTypeKind)
        {
            TypeReference type_ref = mr.GetTypeReference(handle);
            string @namespace = mr.GetString(type_ref.Namespace);
            string name = mr.GetString(type_ref.Name);
            if (@namespace.Length == 0)
            {
                return new CustomTypeRef(this.no_namespace_type_map[name]);
            }

            // This occurs for System.Guid, not sure if it is supposed to
            if (@namespace == "System")
            {
                if (name != "Guid")
                {
                    throw new InvalidOperationException(); // if this happens, new System types have unexpectedly been added
                }
                return new UnhandledTypeRef(@namespace, name);
            }

            Api api = this.api_namespace_map[@namespace];
            return new CustomTypeRef(api.type_name_map[name]);
        }

        public TypeRef GetTypeFromSpecification(MetadataReader mr, INothing? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            throw new NotImplementedException();
        }
    }

    internal abstract class TypeRef
    {
        public abstract void addImports(HashSet<TypeGenInfo> imports);

        public abstract void formatZigType(StringBuilder builder);
    }

    internal class ArrayTypeRef : TypeRef
    {
        public readonly TypeRef element_type;
        public readonly ArrayShape shape;

        public ArrayTypeRef(TypeRef element_type, ArrayShape shape)
        {
            this.element_type = element_type;
            this.shape = shape;
        }

        public override void addImports(HashSet<TypeGenInfo> imports)
        {
            this.element_type.addImports(imports);
        }

        public override void formatZigType(StringBuilder builder)
        {
            builder.Append("[]");
            this.element_type.formatZigType(builder);
        }
    }

    internal class ReferenceTypeRef : TypeRef
    {
        public readonly TypeRef target_type;

        public ReferenceTypeRef(TypeRef target_type)
        {
            this.target_type = target_type;
        }

        public override void addImports(HashSet<TypeGenInfo> imports)
        {
            this.target_type.addImports(imports);
        }

        public override void formatZigType(StringBuilder builder)
        {
            // TODO: do I need to surround it with parens?
            builder.Append("*(");
            this.target_type.formatZigType(builder);
            builder.Append(')');
        }
    }

    internal class PointerTypeRef : TypeRef
    {
        public readonly TypeRef target_type;

        public PointerTypeRef(TypeRef target_type)
        {
            this.target_type = target_type;
        }

        public override void addImports(HashSet<TypeGenInfo> imports)
        {
            this.target_type.addImports(imports);
        }

        public override void formatZigType(StringBuilder builder)
        {
            // TODO: do I need to surround it with parens?
            builder.Append("*(");
            this.target_type.formatZigType(builder);
            builder.Append(')');
        }
    }

    internal class CustomTypeRef : TypeRef
    {
        public readonly TypeGenInfo info;

        public CustomTypeRef(TypeGenInfo info)
        {
            this.info = info;
        }

        public override void addImports(HashSet<TypeGenInfo> imports)
        {
            imports.Add(this.info);
        }

        public override void formatZigType(StringBuilder builder)
        {
            builder.AppendFormat("{0}", this.info.name);
        }
    }

    internal class PrimitiveTypeRef : TypeRef
    {
        public readonly PrimitiveTypeCode code;

        // TODO: use lookup table instead?
        public PrimitiveTypeRef(PrimitiveTypeCode code)
        {
            this.code = code;
        }

        public override void addImports(HashSet<TypeGenInfo> imports)
        {
        }

        public override void formatZigType(StringBuilder builder)
        {
            builder.Append(this.code switch
            {
#pragma warning disable SA1025 // Code should not contain multiple whitespace in a row
                PrimitiveTypeCode.Void      => "void",
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

    internal class UnhandledTypeRef : TypeRef
    {
        public readonly string @namespace;
        public readonly string name;

        // TODO: use lookup table instead?
        public UnhandledTypeRef(string @namespace, string name)
        {
            this.@namespace = @namespace;
            this.name = name;
        }

        public override void addImports(HashSet<TypeGenInfo> imports)
        {
        }

        public override void formatZigType(StringBuilder builder)
        {
            builder.AppendFormat("struct {{ unhandled_type: []const u8 = \"{0}.{1}\" }}", this.@namespace, this.name);
        }
    }

    internal interface INothing
    {
    }
}
