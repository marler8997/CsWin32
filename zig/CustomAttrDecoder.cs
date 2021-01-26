using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static partial class ZigWin32
{
    class CustomAttrDecoder : ICustomAttributeTypeProvider<CustomAttrType>
    {
        public static readonly CustomAttrDecoder Instance = new CustomAttrDecoder();

        private CustomAttrDecoder()
        {
        }

        public CustomAttrType GetPrimitiveType(PrimitiveTypeCode code)
        {
            if (code == PrimitiveTypeCode.Boolean)
            {
                return CustomAttrType.Bool.Instance;
            }
            if (code == PrimitiveTypeCode.String)
            {
                return CustomAttrType.Str.Instance;
            }
            throw new NotImplementedException("Only string and bool primitive types have been implemented for custom attributes");
        }

        public CustomAttrType GetSystemType() => CustomAttrType.SystemType.Instance;

        public CustomAttrType GetSZArrayType(CustomAttrType elementType) => throw new NotImplementedException();

        public CustomAttrType GetTypeFromDefinition(MetadataReader mr, TypeDefinitionHandle handle, byte rawTypeKind) => throw new NotImplementedException();

        public CustomAttrType GetTypeFromReference(MetadataReader mr, TypeReferenceHandle handle, byte rawTypeKind)
        {
            TypeReference type_ref = mr.GetTypeReference(handle);
            string @namespace = mr.GetString(type_ref.Namespace);
            string name = mr.GetString(type_ref.Name);
            if (@namespace == "System.Runtime.InteropServices")
            {
                if (name == "CallingConvention")
                {
                    return CustomAttrType.CallConv.Instance;
                }
                if (name == "UnmanagedType")
                {
                    return CustomAttrType.UnmanagedType.Instance;
                }
            }
            throw new NotImplementedException();
        }

        public CustomAttrType GetTypeFromSerializedName(string name) => throw new NotImplementedException();

        public PrimitiveTypeCode GetUnderlyingEnumType(CustomAttrType type)
        {
            if (object.ReferenceEquals(type, CustomAttrType.CallConv.Instance))
            {
                return PrimitiveTypeCode.Int32; // !!!!!!!! TODO: is this right???? What is this doing???
            }
            if (object.ReferenceEquals(type, CustomAttrType.UnmanagedType.Instance))
            {
                return PrimitiveTypeCode.Int32; // !!!!!!!! TODO: is this right???? What is this doing???
            }
            throw new NotImplementedException();
        }

        public bool IsSystemType(CustomAttrType type) => object.ReferenceEquals(type, CustomAttrType.SystemType.Instance);
    }

    abstract class CustomAttrType
    {
        public abstract string formatValue(object? value);

        public class Bool : CustomAttrType
        {
            public static readonly Bool Instance = new Bool();

            public override string formatValue(object? value) => string.Format("Bool({0})", value);
        }

        public class CallConv : CustomAttrType
        {
            public static readonly CallConv Instance = new CallConv();

            public override string formatValue(object? value) => string.Format("CallConv({0})", (CallingConvention)value!);
        }

        public class SystemType : CustomAttrType
        {
            public static readonly SystemType Instance = new SystemType();

            public override string formatValue(object? value) => string.Format("Type({0})", value);
        }

        public class Str : CustomAttrType
        {
            public static readonly Str Instance = new Str();

            public override string formatValue(object? value) => string.Format("String({0})", value);
        }

        public class UnmanagedType : CustomAttrType
        {
            public static readonly UnmanagedType Instance = new UnmanagedType();

            public override string formatValue(object? value) => string.Format("UnmanagedType({0})", value);
        }
    }

    class ConstantAttr
    {
        public static ConstantAttr Instance = new ConstantAttr();

        public class NativeTypeInfo : ConstantAttr
        {
            public readonly UnmanagedType unmanaged_type;
            public readonly bool is_null_terminated;

            public NativeTypeInfo(UnmanagedType unmanaged_type, bool is_null_terminated)
            {
                this.unmanaged_type = unmanaged_type;
                this.is_null_terminated = is_null_terminated;
            }
        }

        public class Obsolete : ConstantAttr
        {
            public readonly string value;

            public Obsolete(string value)
            {
                this.value = value;
            }
        }
    }

    class BasicTypeAttr
    {
        public class Guid : BasicTypeAttr
        {
            public readonly string value;

            public Guid(string value)
            {
                this.value = value;
            }
        }

        public class RaiiFree : BasicTypeAttr
        {
            public readonly string free_func;

            public RaiiFree(string free_func)
            {
                this.free_func = free_func;
            }
        }

        public class NativeTypedef : BasicTypeAttr
        {
        }
    }

    static void enforceAttrFixedArgCount(NamespaceAndName name, CustomAttributeValue<CustomAttrType> args, uint expected)
    {
        if (args.FixedArguments.Length != expected)
        {
            throw new InvalidDataException(string.Format(
                "expected attribute '{0}' to have {1} fixed arguments but got {2}",
                name.name,
                expected,
                args.FixedArguments.Length));
        }
    }

    static void enforceAttrNamedArgCount(NamespaceAndName name, CustomAttributeValue<CustomAttrType> args, uint expected)
    {
        if (args.NamedArguments.Length != expected)
        {
            throw new InvalidDataException(string.Format(
                "expected attribute '{0}' to have {1} named arguments but got {2}",
                name.name,
                expected,
                args.NamedArguments.Length));
        }
    }

    static void enforceNamedArgName(NamespaceAndName name, CustomAttributeValue<CustomAttrType> args, string expected, int index)
    {
        string? actual = args.NamedArguments[index].Name;
        assertData(actual == expected, string.Format(
            "expected attribute '{0}' to have named argument at index {1} to be named '{2}' but got '{3}'",
            name,
            index,
            expected,
            actual));
    }

    static string attrFixedArgAsString(CustomAttributeTypedArgument<CustomAttrType> attr_value)
    {
        if (object.ReferenceEquals(attr_value.Type, CustomAttrType.Str.Instance))
        {
            return (string)attr_value.Value!;
        }
        throw new InvalidDataException(string.Format("expected attribute value to be a string but got '{0}'", attr_value));
    }

    static UnmanagedType attrFixedArgAsUnmanagedType(CustomAttributeTypedArgument<CustomAttrType> attr_value)
    {
        if (object.ReferenceEquals(attr_value.Type, CustomAttrType.UnmanagedType.Instance))
        {
            return (UnmanagedType)attr_value.Value!;
        }
        throw new InvalidDataException(string.Format("expected attribute value to be an UnmanagedType enum value, but got '{0}'", attr_value));
    }

    static bool attrNamedAsBool(CustomAttributeNamedArgument<CustomAttrType> attr_value)
    {
        if (object.ReferenceEquals(attr_value.Type, CustomAttrType.Bool.Instance))
        {
            return (bool)attr_value.Value!;
        }
        throw new InvalidDataException(string.Format("expected attribute value to be an bool, but got '{0}'", attr_value));
    }
}
