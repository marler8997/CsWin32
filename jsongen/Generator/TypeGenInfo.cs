#pragma warning disable SA1402 // File may only contain a single type

namespace JsonWin32Generator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection.Metadata;

    internal class TypeGenInfo
    {
        private List<TypeGenInfo>? nestedTypes;

        private TypeGenInfo(TypeDefinition def, string apiName, string name, string apiNamespace, string fqn, TypeGenInfo? enclosingType)
        {
            this.Def = def;
            this.ApiName = apiName;
            this.Name = name;
            this.ApiNamespace = apiNamespace;
            this.Fqn = fqn;
            this.EnclosingType = enclosingType;
        }

        internal TypeDefinition Def { get; }

        internal string ApiName { get; }

        internal string Name { get; }

        internal string ApiNamespace { get; }

        internal string Fqn { get; } // note: all fqn (fully qualified name)'s are unique

        internal TypeGenInfo? EnclosingType { get; }

        internal bool IsNested
        {
            get
            {
                return this.EnclosingType != null;
            }
        }

        internal uint NestedTypeCount
        {
            get
            {
                return (this.nestedTypes == null) ? 0 : (uint)this.nestedTypes.Count;
            }
        }

        internal IEnumerable<TypeGenInfo> NestedTypesEnumerable
        {
            get
            {
                return (this.nestedTypes == null) ? Enumerable.Empty<TypeGenInfo>() : this.nestedTypes;
            }
        }

        internal static TypeGenInfo CreateNotNested(TypeDefinition def, string name, string @namespace, Dictionary<string, string> apiNamespaceToName)
        {
            Enforce.Invariant(!def.IsNested, "CreateNotNested called for TypeDefinition that is nested");
            string? apiName;
            if (!apiNamespaceToName.TryGetValue(@namespace, out apiName))
            {
                Enforce.Data(@namespace.StartsWith(Metadata.WindowsWin32NamespacePrefix, StringComparison.Ordinal));
                apiName = @namespace.Substring(Metadata.WindowsWin32NamespacePrefix.Length);
                apiNamespaceToName.Add(@namespace, apiName);
            }

            string fqn = Fmt.In($"{@namespace}.{name}");
            return new TypeGenInfo(
                def: def,
                apiName: apiName,
                name: name,
                apiNamespace: @namespace,
                fqn: fqn,
                enclosingType: null);
        }

        internal static TypeGenInfo CreateNested(MetadataReader mr, TypeDefinition def, TypeGenInfo enclosingType)
        {
            Enforce.Invariant(def.IsNested, "CreateNested called for TypeDefinition that is not nested");
            string name = mr.GetString(def.Name);
            string @namespace = mr.GetString(def.Namespace);
            Enforce.Data(@namespace.Length == 0, "I thought all nested types had an empty namespace");
            string fqn = Fmt.In($"{enclosingType.Fqn}+{name}");
            return new TypeGenInfo(
                def: def,
                apiName: enclosingType.ApiName,
                name: name,
                apiNamespace: enclosingType.ApiNamespace,
                fqn: fqn,
                enclosingType: enclosingType);
        }

        internal TypeGenInfo? TryGetNestedTypeByName(string name)
        {
            if (this.nestedTypes != null)
            {
                foreach (TypeGenInfo info in this.nestedTypes)
                {
                    if (info.Name == name)
                    {
                        return info;
                    }
                }
            }

            return null;
        }

        internal void AddNestedType(TypeGenInfo type_info)
        {
            if (this.nestedTypes == null)
            {
                this.nestedTypes = new List<TypeGenInfo>();
            }
            else if (this.TryGetNestedTypeByName(type_info.Name) != null)
            {
                throw new InvalidOperationException(Fmt.In($"nested type '{type_info.Name}' already exists in '{this.Fqn}'"));
            }

            this.nestedTypes.Add(type_info);
        }

        internal TypeGenInfo GetNestedTypeByName(string name) => this.TryGetNestedTypeByName(name) is TypeGenInfo info ? info :
                throw new ArgumentException(Fmt.In($"type '{this.Fqn}' does not have nested type '{name}'"));

        internal bool HasNestedTypeInScope(TypeGenInfo info)
        {
            Enforce.Invariant(info.IsNested);
            foreach (TypeGenInfo nested_info in this.NestedTypesEnumerable)
            {
                if (object.ReferenceEquals(nested_info, info))
                {
                    return true;
                }
            }

            return this.EnclosingType is TypeGenInfo e && e.HasNestedTypeInScope(info);
        }
    }

    // Note: keeps insertion order (the reason is for predictable code generation)
    internal class TypeGenInfoSet : IEnumerable<TypeGenInfo>
    {
        private readonly List<TypeGenInfo> orderedList;
        private readonly Dictionary<string, TypeGenInfo> fqnMap;

        internal TypeGenInfoSet()
        {
            this.orderedList = new List<TypeGenInfo>();
            this.fqnMap = new Dictionary<string, TypeGenInfo>();
        }

        internal TypeGenInfo this[string fqn]
        {
            get => this.fqnMap[fqn];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new InvalidOperationException();

        public IEnumerator<TypeGenInfo> GetEnumerator() => this.orderedList.GetEnumerator();

        internal void Add(TypeGenInfo info)
        {
            this.orderedList.Add(info);
            this.fqnMap.Add(info.Fqn, info);
        }

        internal bool AddOrVerifyEqual(TypeGenInfo info)
        {
            if (this.fqnMap.TryGetValue(info.Fqn, out TypeGenInfo? other))
            {
                Enforce.Data(object.ReferenceEquals(info, other), Fmt.In(
                    $"found 2 types with the same fully-qualified-name '{info.Fqn}' that are not equal"));
                return false; // already added
            }

            this.Add(info);
            return true; // newly added
        }

        internal bool Contains(TypeGenInfo info) => this.fqnMap.ContainsKey(info.Fqn);
    }
}
