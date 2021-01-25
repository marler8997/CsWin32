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
        public CustomAttrType GetPrimitiveType(PrimitiveTypeCode code)
        {
            if (code != PrimitiveTypeCode.String)
            {
                throw new NotImplementedException("Only String primitive types have been implemented for custom attributes");
            }
            return CustomAttrType.Str.Instance;
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
            }
            throw new NotImplementedException();
        }

        public CustomAttrType GetTypeFromSerializedName(string name) => throw new NotImplementedException();

        public PrimitiveTypeCode GetUnderlyingEnumType(CustomAttrType type)
        {
            if (object.ReferenceEquals(type, CustomAttrType.CallConv.Instance))
            {
                // !!!!!!!! TODO: is this right???? What is this doing???
                return PrimitiveTypeCode.Int32;
            }
            throw new NotImplementedException();
        }

        public bool IsSystemType(CustomAttrType type) => object.ReferenceEquals(type, CustomAttrType.SystemType.Instance);
    }

    abstract class CustomAttrType
    {
        public abstract string formatValue(object? value);

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

    static string attrFixedArgAsString(CustomAttributeTypedArgument<CustomAttrType> attr_value)
    {
        if (object.ReferenceEquals(attr_value.Type, CustomAttrType.Str.Instance))
        {
            return (string)attr_value.Value!;
        }
        throw new InvalidDataException(string.Format("expected attribute value to be a string but got '{0}'", attr_value));
    }
}
