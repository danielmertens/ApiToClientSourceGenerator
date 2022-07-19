using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Models;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Helpers
{
    public class ClassExtractor
    {
        public IEnumerable<ClassDefinition> CreateClassDefinitions(GeneratorExecutionContext context, IEnumerable<ApiDefinition> apiList)
        {
            var classReceiver = (ClassSyntaxReceiver)context.SyntaxReceiver;
            var classList = new List<ClassDefinition>();

            foreach (var api in apiList)
            {
                var returnType = api.ReturnType;
                var stringClassName = "";

                if (returnType.IsKind(SyntaxKind.GenericName))
                {
                    stringClassName = ((IdentifierNameSyntax)((GenericNameSyntax)returnType).TypeArgumentList.Arguments[0]).Identifier.Text;
                }
                else if (returnType.IsKind(SyntaxKind.IdentifierName))
                {
                    stringClassName = ((IdentifierNameSyntax)returnType).Identifier.Text;
                }
                else if (returnType.IsKind(SyntaxKind.PredefinedType))
                {
                    // Return type is a primitive type. No need to load the class.
                    continue;
                }

                if (stringClassName == "") continue;

                // Skip if we already have this type mapped.
                if (classList.FirstOrDefault(c => c.ClassName == stringClassName) != null) continue;

                foreach (var node in classReceiver.ClassCandidates)
                {
                    if (node.Identifier.Text == stringClassName)
                    {
                        classList.Add(CreateClassInfo(node));
                    }
                }
            }

            return classList;
        }

        private ClassDefinition CreateClassInfo(ClassDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            var def = new ClassDefinition
            {
                ClassName = name,
                Properties = new List<PropertyDefinition>()
            };

            var properties = node.Members
                .Where(m => m.IsKind(SyntaxKind.PropertyDeclaration))
                .Cast<PropertyDeclarationSyntax>();

            // TODO: Clean this up. Specific classes that can output typescript types?
            foreach (var prop in properties)
            {
                var typeName = "";
                var nullable = false;
                if (prop.Type.IsKind(SyntaxKind.IdentifierName))
                {
                    typeName = ((IdentifierNameSyntax)prop.Type).Identifier.Text;
                }
                else if (prop.Type.IsKind(SyntaxKind.PredefinedType))
                {
                    typeName = ((PredefinedTypeSyntax)prop.Type).Keyword.Text;
                }
                else if (prop.Type.IsKind(SyntaxKind.NullableType))
                {
                    typeName = ((PredefinedTypeSyntax)((NullableTypeSyntax)prop.Type).ElementType).Keyword.Text;
                    nullable = true;
                }

                def.Properties.Add(new PropertyDefinition
                {
                    Name = prop.Identifier.Text,
                    Type = typeName,
                    Nullable = nullable
                });
            }

            return def;
        }
    }
}
