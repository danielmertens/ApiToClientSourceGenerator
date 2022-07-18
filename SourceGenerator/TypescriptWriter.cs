using System.Collections.Generic;
using System.Text;

namespace SourceGenerator
{
    public static class TypescriptWriter
    {
        public static string Write(List<ApiDefinition> apiList, List<ClassDefinition> classList)
        {
            var text = "";
            text += CreateApiCalls(apiList);
            text += CreateClassTypes(classList);

            return text;
        }

        private static string CreateApiCalls(List<ApiDefinition> apiList)
        {
            var builder = new StringBuilder();

            foreach (var item in apiList)
            {
                builder.AppendLine($"export const fetch{item.ControllerName}{item.MethodName}: () => Promise<{item.ReturnTypeString}> = async () => {{");
                builder.AppendLine($"  const response = await fetch(\"{item.Url}\");");
                builder.AppendLine($"  const body = await response.json();");
                builder.AppendLine($"  return body;");
                builder.AppendLine("}");
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string CreateClassTypes(List<ClassDefinition> classList)
        {
            StringBuilder builder = new StringBuilder();

            foreach (var item in classList)
            {
                builder.AppendLine($"export type {item.ClassName} = {{");

                foreach (var propDef in item.Properties)
                {
                    builder.AppendLine($"  {LowerFirstLetter(propDef.name)}: {TypeToTypescriptType(propDef.type)}");
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

        private static string TypeToTypescriptType(string type)
        {
            switch (type)
            {
                case "int":
                    return "number";
                case "string":
                    return "string";
                case "DateTime":
                    return "Date";

                default: return "any";
            }
        }
    }
}
