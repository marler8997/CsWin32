#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace ZigWin32
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public class ZigGenerator
    {
        private readonly StreamWriter out_file;
        private readonly MetadataReader mr;
        private readonly CancellationToken cancel_token;
        private readonly List<TypeDefinition> Apis;

        private readonly ZigSignatureTypeProvider signatureTypeProviderNoSafeHandles;
        private readonly Dictionary<FieldDefinitionHandle, bool> fieldsToSyntax = new Dictionary<FieldDefinitionHandle, bool>();

        private ZigGenerator(StreamWriter out_file, MetadataReader mr, CancellationToken cancel_token)
        {
            this.out_file = out_file;
            this.mr = mr;
            this.cancel_token = cancel_token;
            this.Apis = mr.TypeDefinitions.Select(mr.GetTypeDefinition).Where(td => mr.StringComparer.Equals(td.Name, "Apis")).ToList();
            this.signatureTypeProviderNoSafeHandles = new ZigSignatureTypeProvider(this, preferNativeInt: true, preferSafeHandles: false);
        }

        public static void Generate(StreamWriter out_file, Stream metadata_stream, CancellationToken cancel_token)
        {
            using var pe_reader = new PEReader(metadata_stream);
            var mr = pe_reader.GetMetadataReader();
            var generator = new ZigGenerator(out_file, mr, cancel_token);
            generator.GenerateAllConstants();
        }

        internal void GenerateAllConstants()
        {
            foreach (FieldDefinitionHandle field_def in this.Apis.SelectMany(api => api.GetFields()))
            {
                this.cancel_token.ThrowIfCancellationRequested();
                this.GenerateConstantIfNotDoneAlready(field_def);
            }
        }

        private void GenerateConstantIfNotDoneAlready(FieldDefinitionHandle field_def)
        {
            if (!this.fieldsToSyntax.ContainsKey(field_def))
            {
                this.GenerateConstant(field_def);
                this.fieldsToSyntax.Add(field_def, true);
            }
        }

        private void GenerateConstant(FieldDefinitionHandle field_def)
        {
            FieldDefinition fieldDef = this.mr.GetFieldDefinition(field_def);
            string name = this.mr.GetString(fieldDef.Name);

            try
            {
                this.out_file.WriteLine("// {0}", name);
                /*
                TypeSyntax fieldType = field_def.DecodeSignature(this.signatureTypeProviderNoSafeHandles, null);
                Constant constant = this.mr.GetConstant(field_def.GetDefaultValue());

                ExpressionSyntax value = this.ToExpressionSyntax(constant);
                if (fieldType is not PredefinedTypeSyntax)
                {
                    if (fieldType is IdentifierNameSyntax { Identifier: { ValueText: string typeName } } && this.TryGetHandleReleaseMethod(typeName, out _))
                    {
                        // Cast to IntPtr first, then the actual handle struct.
                        value = CastExpression(fieldType, CastExpression(IntPtrTypeSyntax, ParenthesizedExpression(value)));
                    }
                    else
                    {
                        value = CastExpression(fieldType, ParenthesizedExpression(value));
                    }
                }

                var modifiers = TokenList(Token(this.Visibility));
                if (this.IsTypeDefStruct(fieldType as IdentifierNameSyntax))
                {
                    modifiers = modifiers.Add(Token(SyntaxKind.StaticKeyword)).Add(Token(SyntaxKind.ReadOnlyKeyword));
                }
                else
                {
                    modifiers = modifiers.Add(Token(SyntaxKind.ConstKeyword));
                }

                return FieldDeclaration(VariableDeclaration(fieldType).AddVariables(
                    VariableDeclarator(name).WithInitializer(EqualsValueClause(value))))
                    .WithModifiers(modifiers);
                */
            }
            catch (Exception ex)
            {
                TypeDefinition typeDef = this.mr.GetTypeDefinition(fieldDef.GetDeclaringType());
                string typeName = this.mr.GetString(typeDef.Name);
                string? ns = this.mr.GetString(typeDef.Namespace);
                throw new GenerationFailedException($"Failed creating field: {ns}.{typeName}.{name}", ex);
            }
        }

        internal TypeSyntax? GenerateSafeHandle(string releaseMethod)
        {
            throw new Exception("Not Implemented");
        }

        internal bool TryGetHandleReleaseMethod(string handleStructName, [NotNullWhen(true)] out string? releaseMethod)
        {
            throw new Exception("Not Implemented");
            /*
            return this.handleTypeReleaseMethod.TryGetValue(handleStructName, out releaseMethod);
            */
        }

        internal TypeDefinitionHandle? GenerateInteropType(TypeReferenceHandle typeRefHandle)
        {
            throw new Exception("Not Implemented");
            /*
            TypeReference typeRef = this.mr.GetTypeReference(typeRefHandle);
            string name = this.mr.GetString(typeRef.Name);
            if (this.typesByName.TryGetValue(name, out TypeDefinitionHandle typeDefHandle))
            {
                this.GenerateInteropType(typeDefHandle);
                return typeDefHandle;
            }
            else
            {
                // System.Guid reaches here, but doesn't need to be generated.
                ////throw new NotSupportedException($"Could not find a type def for: {this.mr.GetString(typeRef.Namespace)}.{name}");
                return null;
            }
            */
        }

        internal void GenerateInteropType(TypeDefinitionHandle typeDefHandle)
        {
            throw new Exception("Not Implemented");
            /*
            if (this.nestedToDeclaringLookup.TryGetValue(typeDefHandle, out TypeDefinitionHandle nestingParentHandle))
            {
                // We should only generate this type into its parent type.
                this.GenerateInteropType(nestingParentHandle);
                return;
            }

            if (!this.typesGenerating.Add(typeDefHandle))
            {
                return;
            }

            // https://github.com/microsoft/CsWin32/issues/31
            TypeDefinition typeDef = this.mr.GetTypeDefinition(typeDefHandle);
            if (this.typesByName.TryGetValue(this.mr.GetString(typeDef.Name), out TypeDefinitionHandle expectedHandle) && !expectedHandle.Equals(typeDefHandle))
            {
                // Skip generating types with conflicting names till we fix that issue.
                return;
            }

            MemberDeclarationSyntax? typeDeclaration = this.CreateInteropType(typeDefHandle);

            if (typeDeclaration is object)
            {
                this.types.Add(typeDefHandle, typeDeclaration);
            }
            */
        }
    }
}
