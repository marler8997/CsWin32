namespace ZigWin32
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public class ZigGenerator
    {
        public static void Generate(CancellationToken cancel_token, StreamWriter out_file, Stream metadata_stream)
        {
            var pe_reader = new PEReader(metadata_stream);
            var mr = pe_reader.GetMetadataReader();
            var generator = new ZigGenerator(cancel_token, out_file, mr);
            generator.GenerateAllConstants();
        }

        private readonly CancellationToken cancel_token;
        private readonly StreamWriter out_file;
        private readonly MetadataReader mr;
        private readonly List<TypeDefinition> Apis;
        // NOTE: rename to fields_generated
        private readonly Dictionary<FieldDefinitionHandle, Boolean> fieldsToSyntax = new Dictionary<FieldDefinitionHandle, Boolean>();
        public ZigGenerator(CancellationToken cancel_token, StreamWriter out_file, MetadataReader mr)
        {
            this.cancel_token = cancel_token;
            this.out_file = out_file;
            this.mr = mr;
            this.Apis = mr.TypeDefinitions.Select(mr.GetTypeDefinition).Where(td => mr.StringComparer.Equals(td.Name, "Apis")).ToList();
        }

        public void GenerateAllConstants()
        {
            foreach (FieldDefinitionHandle field_def in Apis.SelectMany(api => api.GetFields()))
            {
                cancel_token.ThrowIfCancellationRequested();
                GenerateConstantIfNotDoneAlready(field_def);
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
            this.out_file.WriteLine("// {0}", name);
            /*
            try
            {
                TypeSyntax fieldType = fieldDef.DecodeSignature(this.signatureTypeProviderNoSafeHandles, null);
                Constant constant = this.mr.GetConstant(fieldDef.GetDefaultValue());
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
            }
            catch (Exception ex)
            {
                TypeDefinition typeDef = this.mr.GetTypeDefinition(fieldDef.GetDeclaringType());
                string typeName = this.mr.GetString(typeDef.Name);
                string? ns = this.mr.GetString(typeDef.Namespace);
                throw new GenerationFailedException($"Failed creating field: {ns}.{typeName}.{name}", ex);
            }
            */
        }
    }
}
