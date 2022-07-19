using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SourceGenerator
{
    public static class TypescriptWriter
    {
        public static string Write(IEnumerable<ApiDefinition> apiList, IEnumerable<ClassDefinition> classList)
        {
            var text = "";
            text += CreateApiCalls(apiList, classList);
            text += CreateClassTypes(classList);

            return text;
        }

        private static string CreateApiCalls(IEnumerable<ApiDefinition> apiList, IEnumerable<ClassDefinition> classList)
        {
            var builder = new StringBuilder();

            foreach (var item in apiList)
            {
                builder.AppendLine($"export const fetch{item.ControllerName}{item.MethodName}: () => Promise<{ReturnTypeToTypescriptType(item.ReturnType, classList)}> = async () => {{");
                builder.AppendLine($"  const response = await fetch(\"{item.Url}\");");
                builder.AppendLine($"  const body = await response.json();");
                builder.AppendLine($"  return body;");
                builder.AppendLine("}");
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string CreateClassTypes(IEnumerable<ClassDefinition> classList)
        {
            StringBuilder builder = new StringBuilder();

            foreach (var item in classList)
            {
                builder.AppendLine($"export type {item.ClassName} = {{");

                foreach (var propDef in item.Properties)
                {
                    builder.AppendLine($"  {LowerFirstLetter(propDef.Name)}: {TypeToTypescriptType(propDef.Type, propDef.Nullable)}");
                }

                builder.AppendLine("}");
                builder.AppendLine();
            }

            return builder.ToString();
        }


        private static string LowerFirstLetter(string name)
        {
            return name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
        }

        private static string TypeToTypescriptType(string type, bool nullable = false)
        {
            string tsType = type switch
            {
                "int" => "number",
                "string" => "string",
                "DateTime" => "Date",
                _ => "any",
            };

            return nullable ? $"{tsType} | null | undefined" : tsType;
        }

        private static string ReturnTypeToTypescriptType(TypeSyntax syntax, IEnumerable<ClassDefinition> classList)
        {
            if (syntax.IsKind(SyntaxKind.GenericName))
            {
                var genericSyntax = (GenericNameSyntax)syntax;

                if (genericSyntax.TypeArgumentList.Arguments[0].IsKind(SyntaxKind.IdentifierName))
                {
                    string typeName = ((IdentifierNameSyntax)genericSyntax.TypeArgumentList.Arguments[0]).Identifier.Text;

                    if (classList.Any(c => c.ClassName == typeName))
                    {
                        return $"{typeName}[]";
                    }
                    else
                    {
                        return $"{TypeToTypescriptType(typeName)}[]";
                    }
                }
                else
                {
                    string typeName = ((PredefinedTypeSyntax)genericSyntax.TypeArgumentList.Arguments[0]).Keyword.Text;
                    return $"{TypeToTypescriptType(typeName)}[]";
                }


            }
            else if (syntax.IsKind(SyntaxKind.IdentifierName))
            {
                string typeName = ((IdentifierNameSyntax)syntax).Identifier.Text;

                if (classList.Any(c => c.ClassName == typeName))
                {
                    return typeName;
                }
                else
                {
                    return TypeToTypescriptType(typeName);
                }
            }
            else if (syntax.IsKind(SyntaxKind.PredefinedType))
            {
                string typeName = ((PredefinedTypeSyntax)syntax).Keyword.Text;
                return TypeToTypescriptType(typeName);
            }

            return "any";
        }
    }
}
