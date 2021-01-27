#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace JsonWin32Generator
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    internal class JsonGenerator
    {
        private readonly MetadataReader mr;
        private readonly CancellationToken cancelToken;
        private readonly Dictionary<string, Api> apiNamespaceMap = new Dictionary<string, Api>();
        private readonly Dictionary<TypeDefinitionHandle, TypeGenInfo> typeMap = new Dictionary<TypeDefinitionHandle, TypeGenInfo>();
        private readonly TypeRefDecoder typeRefDecoder;

        private JsonGenerator(MetadataReader mr, CancellationToken cancelToken)
        {
            this.mr = mr;
            this.cancelToken = cancelToken;

            Dictionary<string, string> apiNamespaceToName = new Dictionary<string, string>();

            // ---------------------------------------------------------------
            // Scan all types and sort into the api they belong to
            // ---------------------------------------------------------------
            List<TypeDefinitionHandle> nestedTypes = new List<TypeDefinitionHandle>();
            foreach (TypeDefinitionHandle typeDefHandle in this.mr.TypeDefinitions)
            {
                TypeDefinition typeDef = mr.GetTypeDefinition(typeDefHandle);

                // skip nested types until we get all the non-nested types, this is because
                // we need to be able to look up the enclosing type to get all the info we need
                if (typeDef.IsNested)
                {
                    nestedTypes.Add(typeDefHandle);
                    continue;
                }

                string typeName = mr.GetString(typeDef.Name);
                string typeNamespace = mr.GetString(typeDef.Namespace);
                if (typeNamespace.Length == 0)
                {
                    Debug.Assert(typeName == "<Module>", "found a type without a namespace that is not nested and not '<Module>'");
                    continue;
                }

                TypeGenInfo typeInfo = TypeGenInfo.CreateNotNested(typeDef, typeName, typeNamespace, apiNamespaceToName);
                this.typeMap.Add(typeDefHandle, typeInfo);

                Api? api;
                if (!this.apiNamespaceMap.TryGetValue(typeInfo.ApiNamespace, out api))
                {
                    api = new Api(typeInfo.ApiNamespace);
                    this.apiNamespaceMap.Add(typeInfo.ApiNamespace, api);
                }

                if (typeInfo.Name == "Apis")
                {
                    // NOTE: The "Apis" type is a specially-named type reserved to contain all the constant
                    // and function declarations for an api.
                    Debug.Assert(api.Constants == null, "multiple Apis types in the same namespace");
                    api.Constants = typeInfo.Def.GetFields();
                    api.Funcs = typeInfo.Def.GetMethods();
                }
                else
                {
                    api.AddTopLevelType(typeInfo);
                }
            }

            // ---------------------------------------------------------------
            // Now go back through and create objects for the nested types
            // ---------------------------------------------------------------
            for (uint pass = 1; ; pass++)
            {
                int saveCount = nestedTypes.Count;
                Console.WriteLine("DEBUG: nested loop pass {0} (types left: {1})", pass, saveCount);

                for (int i = nestedTypes.Count - 1; i >= 0; i--)
                {
                    TypeDefinitionHandle typeDefHandle = nestedTypes[i];
                    TypeDefinition typeDef = mr.GetTypeDefinition(typeDefHandle);
                    Debug.Assert(typeDef.IsNested, "codebug");
                    if (this.typeMap.TryGetValue(typeDef.GetDeclaringType(), out TypeGenInfo? enclosingType))
                    {
                        TypeGenInfo typeInfo = TypeGenInfo.CreateNested(mr, typeDef, enclosingType);
                        this.typeMap.Add(typeDefHandle, typeInfo);
                        enclosingType.AddNestedType(typeInfo);
                        nestedTypes.RemoveAt(i);
                        i--;
                    }
                }

                if (nestedTypes.Count == 0)
                {
                    break;
                }

                if (saveCount == nestedTypes.Count)
                {
                    throw new InvalidDataException(Fmt.In(
                        $"found {nestedTypes.Count} nested types whose declaring type handle does not match any type definition handle"));
                }
            }

            this.typeRefDecoder = new TypeRefDecoder(this.apiNamespaceMap, this.typeMap);
        }

        internal static void Generate(MetadataReader mr, string outDir, CancellationToken cancelToken)
        {
            JsonGenerator generator = new JsonGenerator(mr, cancelToken);

            foreach (Api api in generator.apiNamespaceMap.Values)
            {
                string filepath = Path.Combine(outDir, api.BaseFileName);
                using var fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
                var writer = new TabWriter(streamWriter);
                Console.WriteLine("Api: {0}", api.Name);
                generator.GenerateApi(writer, api);
            }
        }

#pragma warning disable SA1513 // Closing brace should be followed by blank line
        private static void WriteJsonArray(TabWriter writer, string prefix, List<string> jsonElements)
        {
            if (jsonElements == null || jsonElements.Count == 0)
            {
                writer.WriteLine("{0}[]", prefix);
            }
            else
            {
                writer.WriteLine("{0}[", prefix);
                writer.Tab();
                string elementPrefix = string.Empty;
                foreach (string jsonElement in jsonElements)
                {
                    writer.WriteLine("{0}{1}", elementPrefix, jsonElement);
                    elementPrefix = ",";
                }
                writer.Untab();
                writer.WriteLine("]");
            }
        }

        private void GenerateApi(TabWriter writer, Api api)
        {
            writer.WriteLine("{");
            writer.WriteLine();
            writer.WriteLine("\"Constants\":[");
            if (api.Constants != null)
            {
                string fieldPrefix = string.Empty;
                foreach (FieldDefinitionHandle fieldDef in api.Constants)
                {
                    this.cancelToken.ThrowIfCancellationRequested();
                    writer.Tab();
                    this.GenerateConst(writer, fieldPrefix, fieldDef);
                    writer.Untab();
                    fieldPrefix = ",";
                }
            }
            writer.WriteLine("]");
            var unicodeSet = new UnicodeAliasSet();
            writer.WriteLine();
            writer.WriteLine(",\"Types\":[");
            {
                string fieldPrefix = string.Empty;
                foreach (TypeGenInfo typeInfo in api.TopLevelTypes)
                {
                    this.cancelToken.ThrowIfCancellationRequested();
                    writer.Tab();
                    this.GenerateType(writer, fieldPrefix, typeInfo);
                    writer.Untab();
                    fieldPrefix = ",";
                    unicodeSet.RegisterTopLevelSymbol(typeInfo.Name);
                }
            }
            writer.WriteLine("]");
            writer.WriteLine();
            writer.WriteLine(",\"Functions\":[");
            if (api.Funcs != null)
            {
                string fieldPrefix = string.Empty;
                foreach (MethodDefinitionHandle funcHandle in api.Funcs)
                {
                    this.cancelToken.ThrowIfCancellationRequested();
                    writer.Tab();
                    var funcName = this.GenerateFunc(writer, fieldPrefix, funcHandle);
                    writer.Untab();
                    fieldPrefix = ",";
                    unicodeSet.RegisterTopLevelSymbol(funcName);
                }
            }
            writer.WriteLine("]");

            // NOTE: the win32metadata project winmd file doesn't explicitly contain unicode aliases
            //       but it seems like a good thing to include.
            writer.WriteLine();
            writer.WriteLine(",\"UnicodeAliases\":[");
            writer.Tab();
            {
                string fieldPrefix = string.Empty;
                foreach (UnicodeAlias alias in unicodeSet.Candidates)
                {
                    if (alias.HaveAnsi && alias.HaveWide && !unicodeSet.NonCandidates.Contains(alias.Alias))
                    {
                        writer.WriteLine("{0}\"{1}\"", fieldPrefix, alias.Alias);
                        fieldPrefix = ",";
                    }
                }
            }
            writer.Untab();
            writer.WriteLine("]");
            writer.WriteLine();
            writer.WriteLine("}");
        }

        private void GenerateConst(TabWriter writer, string constFieldPrefix, FieldDefinitionHandle fieldDefHandle)
        {
            writer.WriteLine("{0}{{", constFieldPrefix);
            writer.Tab();
            using var defer = Defer.Do(() =>
            {
                writer.Untab();
                writer.WriteLine("}");
            });

            FieldDefinition fieldDef = this.mr.GetFieldDefinition(fieldDefHandle);

            FieldAttributes expected = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault;
            if (fieldDef.Attributes != expected)
            {
                throw new InvalidOperationException(Fmt.In(
                    $"Expected Constant FieldDefinition to have these attributes '{expected}' but got '{fieldDef.Attributes}'"));
            }
            Enforce.Data(fieldDef.GetOffset() == -1);
            Enforce.Data(fieldDef.GetRelativeVirtualAddress() == 0);

            List<string> jsonAttributes = new List<string>();

            // TODO: what is fieldDef.GetMarshallingDescriptor?
            foreach (CustomAttributeHandle attrHandle in fieldDef.GetCustomAttributes())
            {
                ConstantAttr attr = this.DecodeConstantAttr(this.mr.GetCustomAttribute(attrHandle));
                if (object.ReferenceEquals(attr, ConstantAttr.Instance))
                {
                    // we already assume "const" on all constant values where this matters (i.e. string literals)
                }
                else if (attr is ConstantAttr.NativeTypeInfo nativeTypeInfo)
                {
                    // we already assume null-termination on all constant string literals
                    Enforce.Data(nativeTypeInfo.UnmanagedType == UnmanagedType.LPWStr);
                    Enforce.Data(nativeTypeInfo.IsNullTerminated);
                }
                else if (attr is ConstantAttr.Obsolete obsolete)
                {
                    jsonAttributes.Add(Fmt.In($"{{\"Kind\":\"Obsolete\",\"Message\":\"{obsolete.Message}\"}}"));
                }
                else
                {
                    Enforce.Data(false);
                }
            }

            string name = this.mr.GetString(fieldDef.Name);
            Constant constant = this.mr.GetConstant(fieldDef.GetDefaultValue());
            string value = constant.ReadConstValue(this.mr);
            var typeRef = TypeRef.Primitive.Get(constant.TypeCode.ToPrimitiveTypeCode());
            writer.WriteLine("\"Name\":\"{0}\"", name);
            writer.WriteLine(",\"Type\":{0}", typeRef.ToJson());
            writer.WriteLine(",\"Value\":{0}", value);
            WriteJsonArray(writer, ",\"Attrs\":", jsonAttributes);
        }

        private void GenerateType(TabWriter writer, string typeFieldPrefix, TypeGenInfo typeInfo)
        {
            writer.WriteLine("{0}{{", typeFieldPrefix);
            writer.Tab();
            using var defer = Defer.Do(() =>
            {
                writer.Untab();
                writer.WriteLine("}");
            });

            var isContainerType = this.GenerateTypeInner(writer, typeInfo);
            if (!isContainerType)
            {
                Enforce.Data(typeInfo.Def.GetMethods().Count == 0);
                Enforce.Data(typeInfo.NestedTypeCount == 0);
            }
            else
            {
                writer.WriteLine(",\"Methods\":[");
                writer.Tab();
                string methodElementPrefix = string.Empty;
                foreach (MethodDefinitionHandle methodDefHandle in typeInfo.Def.GetMethods())
                {
                    MethodDefinition methodDef = this.mr.GetMethodDefinition(methodDefHandle);
                    writer.WriteLine("{0}\"TODO: Method '{1}'\"", methodElementPrefix, this.mr.GetString(methodDef.Name));
                    methodElementPrefix = ",";
                }
                writer.Untab();
                writer.WriteLine("]");

                string nestedFieldPrefix = string.Empty;
                writer.WriteLine(",\"NestedTypes\":[");
                foreach (TypeGenInfo nestedType in typeInfo.NestedTypesEnumerable)
                {
                    writer.Tab();
                    this.GenerateType(writer, nestedFieldPrefix, nestedType);
                    writer.Untab();
                    nestedFieldPrefix = ",";
                }
                writer.WriteLine("]");
            }
        }

        // returns true we generated a container type and it still needs to be ended
        private bool GenerateTypeInner(TabWriter writer, TypeGenInfo typeInfo)
        {
            writer.WriteLine("\"Name\":\"{0}\"", typeInfo.Name);

            DecodedTypeAttributes attrs = new DecodedTypeAttributes(typeInfo.Def.Attributes);
            writer.WriteLine(",\"Layout\":\"{0}\"", attrs.Layout);
            if (typeInfo.IsNested)
            {
                Enforce.Data(attrs.Visibility == TypeVisibility.NestedPublic);
            }
            else
            {
                Enforce.Data(attrs.Visibility == TypeVisibility.Public);
            }
            Enforce.Data(attrs.IsAbstract == !attrs.IsSealed);
            Enforce.Data(attrs.IsAbstract == attrs.IsInterface);
            Enforce.Data(typeInfo.Def.GetDeclarativeSecurityAttributes().Count == 0);
            Enforce.Data(typeInfo.Def.GetEvents().Count == 0);
            Enforce.Data(typeInfo.Def.GetGenericParameters().Count == 0);
            Enforce.Data(typeInfo.Def.GetMethodImplementations().Count == 0);
            Enforce.Data(typeInfo.Def.GetProperties().Count == 0);
            Enforce.Data(typeInfo.Def.GetNestedTypes().Length == typeInfo.NestedTypeCount);

            if (!attrs.IsAbstract)
            {
                // TODO: handle these InterfaceImplementations when I implement abstract types
                Enforce.Data(typeInfo.Def.GetInterfaceImplementations().Count == 0);
            }

            TypeLayout typeLayout = typeInfo.Def.GetLayout();
            string? skipBecause = null;
            if (attrs.IsAbstract)
            {
                skipBecause = "its an abstract type (probably a COM type?)";
            }
            else if (attrs.Layout == TypeLayout2.Explicit)
            {
                skipBecause = "it has an explicit layout";
            }
            else if (attrs.Layout == TypeLayout2.Auto)
            {
                skipBecause = "it has an 'auto' layout (follow up on https://github.com/microsoft/win32metadata/issues/188)";
            }
            else if (!typeLayout.IsDefault || typeLayout.PackingSize != 0 || typeLayout.Size != 0)
            {
                skipBecause = Fmt.In(
                    $"it has a non-default layout IsDefault={typeLayout.IsDefault} PackingSize={typeLayout.PackingSize} Size={typeLayout.Size}");
            }

            if (skipBecause != null)
            {
                writer.WriteLine(",\"Comment\":\"not generating the info type yet because {0}\"", skipBecause);
                return true; // we have started a container type just in case
            }

            List<string> jsonAttributes = new List<string>();

            // TODO: how many types have guids?  should it be a direct field or an attribute?
            string? optionalTypeGuid = null;
            bool isNativeTypedef = false;

            foreach (CustomAttributeHandle attrHandle in typeInfo.Def.GetCustomAttributes())
            {
                BasicTypeAttr attr = this.DecodeBasicTypeAttr(this.mr.GetCustomAttribute(attrHandle));
                if (attr is BasicTypeAttr.Guid guidAttr)
                {
                    optionalTypeGuid = guidAttr.Value;
                }
                else if (attr is BasicTypeAttr.RaiiFree raiiAttr)
                {
                    jsonAttributes.Add(Fmt.In($"{{\"Kind\":\"RAIIFree\",\"FreeFunc\":\"{raiiAttr.FreeFunc}\"}}"));
                }
                else if (attr is BasicTypeAttr.NativeTypedef)
                {
                    isNativeTypedef = true;
                }
                else
                {
                    Enforce.Data(false);
                }
            }

            WriteJsonArray(writer, ",\"Attrs\":", jsonAttributes);
            if (isNativeTypedef)
            {
                if (typeInfo.Def.GetFields().Count != 1)
                {
                    throw new InvalidDataException(Fmt.In($"native typedef '{typeInfo.Name}' has fields?"));
                }
                FieldDefinition targetDef = this.mr.GetFieldDefinition(typeInfo.Def.GetFields().First());
                string targetDefJson = targetDef.DecodeSignature(this.typeRefDecoder, null).ToJson();
                writer.WriteLine(",\"Kind\":\"Typedef\"");
                writer.WriteLine(",\"Def\":{0}", targetDefJson);
                Enforce.Data(typeInfo.NestedTypeCount == 0);
                return false; // not a container type
            }

            writer.WriteLine(",\"Kind\":\"struct (I think)\"");
            writer.WriteLine(",\"Fields\":[");
            writer.Tab();
            string fieldElemPrefix = string.Empty;
            foreach (FieldDefinitionHandle fieldDefHandle in typeInfo.Def.GetFields())
            {
                FieldDefinition fieldDef = this.mr.GetFieldDefinition(fieldDefHandle);
                string fieldTypeJson = fieldDef.DecodeSignature(this.typeRefDecoder, null).ToJson();
                writer.WriteLine("{0}{{\"Name\":\"{1}\",\"Type\":{2}}}", fieldElemPrefix, this.mr.GetString(fieldDef.Name), fieldTypeJson);
                fieldElemPrefix = ",";
            }
            writer.Untab();
            writer.WriteLine("]");
            return true; // we are inside the struct type
        }

        private string GenerateFunc(TabWriter writer, string funcFieldPrefix, MethodDefinitionHandle funcHandle)
        {
            writer.WriteLine("{0}{{", funcFieldPrefix);
            writer.Tab();
            using var defer = Defer.Do(() =>
            {
                writer.Untab();
                writer.WriteLine("}");
            });

            MethodDefinition funcDef = this.mr.GetMethodDefinition(funcHandle);
            string funcName = this.mr.GetString(funcDef.Name);
            writer.WriteLine("\"Name\":\"{0}\"", funcName);

            // Looks like right now all the functions have these same attributes
            var decodedAttrs = new DecodedMethodAttributes(funcDef.Attributes);
            Enforce.Data(decodedAttrs.MemberAccess == MemberAccess.Public);
            Enforce.Data(decodedAttrs.IsStatic);
            Enforce.Data(!decodedAttrs.IsFinal);
            Enforce.Data(!decodedAttrs.IsVirtual);
            Enforce.Data(!decodedAttrs.IsAbstract);
            Enforce.Data(decodedAttrs.PInvokeImpl);
            Enforce.Data(decodedAttrs.HideBySig);
            Enforce.Data(!decodedAttrs.NewSlot);
            Enforce.Data(!decodedAttrs.SpecialName);
            Enforce.Data(!decodedAttrs.CheckAccessOnOverride);
            Enforce.Data(funcDef.GetCustomAttributes().Count == 0);
            Enforce.Data(funcDef.GetDeclarativeSecurityAttributes().Count == 0);
            Enforce.Data(funcDef.ImplAttributes == MethodImplAttributes.PreserveSig);

            MethodImport methodImport = funcDef.GetImport();
            var methodImportAttrs = new DecodedMethodImportAttributes(methodImport.Attributes);
            Enforce.Data(methodImportAttrs.ExactSpelling);
            Enforce.Data(methodImportAttrs.CharSet == CharSet.None);
            Enforce.Data(methodImportAttrs.BestFit == null);
            Enforce.Data(methodImportAttrs.CallConv == CallConv.Winapi);
            Enforce.Data(methodImportAttrs.ThrowOnUnmapableChar == null);

            Enforce.Data(this.mr.GetString(methodImport.Name) == funcName);

            ModuleReference moduleRef = this.mr.GetModuleReference(methodImport.Module);
            Enforce.Data(moduleRef.GetCustomAttributes().Count == 0);
            string importName = this.mr.GetString(moduleRef.Name);

            MethodSignature<TypeRef> methodSig = funcDef.DecodeSignature(this.typeRefDecoder, null);

            Enforce.Data(methodSig.Header.Kind == SignatureKind.Method);
            Enforce.Data(methodSig.Header.CallingConvention == SignatureCallingConvention.Default);
            Enforce.Data(methodSig.Header.Attributes == SignatureAttributes.None);

            writer.WriteLine(",\"SetLastError\":{0}", methodImportAttrs.SetLastError ? "true" : "false");
            writer.WriteLine(",\"DllImport\":\"{0}\"", importName);
            writer.WriteLine(",\"ReturnType\":{0}", methodSig.ReturnType.ToJson());
            writer.WriteLine(",\"Params\":[");
            writer.Tab();
            string paramFieldPrefix = string.Empty;
            int nextExpectedSequenceNumber = 1;
            foreach (ParameterHandle paramHandle in funcDef.GetParameters())
            {
                Parameter param = this.mr.GetParameter(paramHandle);
                if (param.SequenceNumber == 0)
                {
                    // this is the return parameter
                    continue;
                }

                Enforce.Data(param.SequenceNumber == nextExpectedSequenceNumber, "parameters were not ordered");
                nextExpectedSequenceNumber++;

                // TODO: handle param.Attributes
                // TODO: handle param.GetCustomAttributes()
                // TODO: handle param.GetDefaultValue();
                // TODO: handle param.GetMarshallingDescriptor();
                string paramName = this.mr.GetString(param.Name);
                Enforce.Data(paramName.Length > 0);

                var paramType = methodSig.ParameterTypes[param.SequenceNumber - 1];
                writer.WriteLine("{0}{{\"name\":\"{1}\",\"type\":{2}}}", paramFieldPrefix, paramName, paramType.ToJson());
                paramFieldPrefix = ",";
            }
            writer.Untab();
            writer.WriteLine("]");
            return funcName;
        }

#pragma warning restore SA1513 // Closing brace should be followed by blank line

        private ConstantAttr DecodeConstantAttr(CustomAttribute attr)
        {
            NamespaceAndName attrName = attr.GetAttrTypeName(this.mr);
            CustomAttributeValue<CustomAttrType> attrArgs = attr.DecodeValue(CustomAttrDecoder.Instance);
            if (attrName == new NamespaceAndName("Windows.Win32.Interop", "ConstAttribute"))
            {
                Enforce.AttrFixedArgCount(attrName, attrArgs, 0);
                Enforce.AttrNamedArgCount(attrName, attrArgs, 0);
                return ConstantAttr.Instance;
            }

            if (attrName == new NamespaceAndName("Windows.Win32.Interop", "NativeTypeInfoAttribute"))
            {
                Enforce.AttrFixedArgCount(attrName, attrArgs, 1);
                Enforce.AttrNamedArgCount(attrName, attrArgs, 1);
                UnmanagedType unmanagedType = Enforce.AttrFixedArgAsUnmanagedType(attrArgs.FixedArguments[0]);
                Enforce.NamedArgName(attrName, attrArgs, "IsNullTerminated", 0);
                bool isNullTerminated = Enforce.AttrNamedAsBool(attrArgs.NamedArguments[0]);
                return new ConstantAttr.NativeTypeInfo(unmanagedType, isNullTerminated);
            }

            if (attrName == new NamespaceAndName("System", "ObsoleteAttribute"))
            {
                Enforce.AttrFixedArgCount(attrName, attrArgs, 1);
                Enforce.AttrNamedArgCount(attrName, attrArgs, 0);
                return new ConstantAttr.Obsolete(Enforce.AttrFixedArgAsString(attrArgs.FixedArguments[0]));
            }

            throw new NotImplementedException(Fmt.In($"uhandled constant custom attribute \"{attrName.Namespace}\", \"{attrName.Name}\""));
        }

        private BasicTypeAttr DecodeBasicTypeAttr(CustomAttribute attr)
        {
            NamespaceAndName attrName = attr.GetAttrTypeName(this.mr);
            CustomAttributeValue<CustomAttrType> attrArgs = attr.DecodeValue(CustomAttrDecoder.Instance);
            if (attrName == new NamespaceAndName("System.Runtime.InteropServices", "GuidAttribute"))
            {
                Enforce.AttrFixedArgCount(attrName, attrArgs, 1);
                Enforce.AttrNamedArgCount(attrName, attrArgs, 0);
                return new BasicTypeAttr.Guid(Enforce.AttrFixedArgAsString(attrArgs.FixedArguments[0]));
            }

            if (attrName == new NamespaceAndName("Windows.Win32.Interop", "RAIIFreeAttribute"))
            {
                Enforce.AttrFixedArgCount(attrName, attrArgs, 1);
                Enforce.AttrNamedArgCount(attrName, attrArgs, 0);
                return new BasicTypeAttr.RaiiFree(Enforce.AttrFixedArgAsString(attrArgs.FixedArguments[0]));
            }

            if (attrName == new NamespaceAndName("Windows.Win32.Interop", "NativeTypedefAttribute"))
            {
                Enforce.AttrFixedArgCount(attrName, attrArgs, 0);
                Enforce.AttrNamedArgCount(attrName, attrArgs, 0);
                return new BasicTypeAttr.NativeTypedef();
            }

            throw new NotImplementedException(Fmt.In($"uhandled type custom attribute \"{attrName.Namespace}\", \"{attrName.Name}\""));
        }

        private class UnicodeAlias
        {
            internal UnicodeAlias(string alias, string? ansi = null, string? wide = null, bool haveAnsi = false, bool haveWide = false)
            {
                this.Alias = alias;
                this.Ansi = (ansi != null) ? ansi : alias + "A";
                this.Wide = (wide != null) ? wide : alias + "W";
                this.HaveAnsi = haveAnsi;
                this.HaveWide = haveWide;
            }

            internal string Alias { get; }

            internal string Ansi { get; }

            internal string Wide { get; }

            internal bool HaveAnsi { get; set; }

            internal bool HaveWide { get; set; }
        }

        private class UnicodeAliasSet
        {
            private readonly Dictionary<string, UnicodeAlias> ansiMap = new Dictionary<string, UnicodeAlias>();
            private readonly Dictionary<string, UnicodeAlias> wideMap = new Dictionary<string, UnicodeAlias>();

            internal UnicodeAliasSet()
            {
            }

            internal HashSet<string> NonCandidates { get; } = new HashSet<string>();

            internal List<UnicodeAlias> Candidates { get; } = new List<UnicodeAlias>();

            internal void RegisterTopLevelSymbol(string symbol)
            {
                // TODO: For now this is the only way I know of to tell if a symbol is a unicode A/W variant
                //       I check that there are the A/W variants, and that the base symbol is not already defined.
                if (symbol.EndsWith("A", StringComparison.Ordinal))
                {
                    UnicodeAlias? alias;
                    if (this.ansiMap.TryGetValue(symbol, out alias))
                    {
                        Debug.Assert(alias.HaveAnsi == false, "codebug");
                        alias.HaveAnsi = true;
                    }
                    else
                    {
                        string common = symbol.Remove(symbol.Length - 1);
                        alias = new UnicodeAlias(alias: common, ansi: symbol, wide: null, haveAnsi: true, haveWide: false);
                        Debug.Assert(!this.wideMap.ContainsKey(alias.Wide), "codebug");
                        this.wideMap.Add(alias.Wide, alias);
                        this.Candidates.Add(alias);
                    }
                }
                else if (symbol.EndsWith("W", StringComparison.Ordinal))
                {
                    UnicodeAlias? alias;
                    if (this.wideMap.TryGetValue(symbol, out alias))
                    {
                        Debug.Assert(alias.HaveWide == false, "codebug");
                        alias.HaveWide = true;
                    }
                    else
                    {
                        string common = symbol.Remove(symbol.Length - 1);
                        alias = new UnicodeAlias(alias: common, ansi: null, wide: symbol, haveAnsi: false, haveWide: true);
                        Debug.Assert(!this.ansiMap.ContainsKey(alias.Ansi), "codebug");
                        this.ansiMap.Add(alias.Ansi, alias);
                        this.Candidates.Add(alias);
                    }
                }
                else
                {
                    this.NonCandidates.Add(symbol);
                }
            }
        }
    }
}
