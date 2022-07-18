using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SourceGenerator
{
    [Generator]
    public class TypeGenerator : ISourceGenerator
    {
        private const string RouteAttributeName = "Route";
        private const string HttpGetAttributeName = "HttpGet";

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                //Debugger.Launch();
            }
#endif 
            Debug.WriteLine("Initalize code generator");
            context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var recv = (MySyntaxReceiver)context.SyntaxReceiver;

            var builder = new StringBuilder($"URL list{Environment.NewLine}");

            var apiList = new List<ApiDefinition>();
            var classList = new List<ClassDefinition>();

            foreach (var node in recv.ClassCandidates)
            {
                // The node is a controller class
                if (node.AttributeLists != null
                    && node.AttributeLists.Any(a => a.Attributes.Any(att => att.Name.ToString() == "Route")))
                {
                    apiList.AddRange(CreateApiInfo(node, context));
                }
            }

            foreach (var api in apiList)
            {
                var returnType = api.ReturnType;
                var stringClassName = "";

                // TODO: Now we don't know that it's a list. fix this.
                if (returnType.IsKind(SyntaxKind.GenericName))
                {
                    stringClassName = ((IdentifierNameSyntax)((GenericNameSyntax)returnType).TypeArgumentList.Arguments[0]).Identifier.Text;
                }
                else if (returnType.IsKind(SyntaxKind.IdentifierName))
                {
                    stringClassName = ((IdentifierNameSyntax)returnType).Identifier.Text;
                }

                if (stringClassName == "") continue;
                api.ReturnTypeString = stringClassName;

                // Skip if we already have this type mapped.
                if (classList.FirstOrDefault(c => c.ClassName == api.ReturnTypeString) != null) continue;

                foreach (var node in recv.ClassCandidates)
                {
                    if (node.Identifier.Text == stringClassName)
                    {
                        classList.Add(CreateClassInfo(node));
                    }
                }
            }

            var text = TypescriptWriter.Write(apiList, classList);

            // Doesn't work! Only outputs cs files. 
            // context.AddSource($"apiClient.tsx", builder.ToString());

            File.Delete("apiClient.tsx");
            File.WriteAllText("apiClient.tsx", text);
        }

        private List<ApiDefinition> CreateApiInfo(ClassDeclarationSyntax controller, GeneratorExecutionContext context)
        {
            var definitionList = new List<ApiDefinition>();

            var controllerName = controller.Identifier.Text.Replace("Controller", "");

            // Get controller route
            var controllerRoute = GetControllerRoute(controller, context.Compilation);

            // Get list of API methods in class.
            var methods = controller.Members
                .Where(member => member.IsKind(SyntaxKind.MethodDeclaration))
                .Where(method => method.AttributeLists.Any(attList => attList.Attributes.Any(att => att.Name.ToString() == "HttpGet")))
                .Cast<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var methodName = method.Identifier.Text;

                // Get path above methods
                var getAttribute = method.AttributeLists
                    .SelectMany(list => list.Attributes)
                    .FirstOrDefault(att => att.Name.ToString() == HttpGetAttributeName);

                var getPath = getAttribute.ArgumentList.Arguments[0].Expression.ToString();
                getPath = getPath.Replace("\"", "");
                //builder.AppendLine($"GET: {controllerRoute}/{getPath}");

                var def = new ApiDefinition
                {
                    ControllerName = controllerName,
                    MethodName = methodName,
                    HttpType = "GET",
                    Url = $"{controllerRoute}/{getPath}",
                    ReturnType = method.ReturnType
                };

                definitionList.Add(def);
            }

            return definitionList;
        }

        private string GetControllerRoute(ClassDeclarationSyntax node, Compilation compilation)
        {
            var routeAttribute = node.AttributeLists
                .SelectMany(attList => attList.Attributes)
                .Where(att => att.Name.ToString() == RouteAttributeName)
                .FirstOrDefault();

            var names = node.AttributeLists
                .SelectMany(attList => attList.Attributes)
                .Select(att => att.Name.ToString());

            if (routeAttribute is null) throw new NotSupportedException();
            if (routeAttribute.ArgumentList.Arguments.Count != 1) throw new NotSupportedException();

            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            var routeArg = routeAttribute.ArgumentList.Arguments[0];
            var routeExpr = routeArg.Expression;
            var routeTemplate = semanticModel.GetConstantValue(routeExpr).ToString();

            if (routeTemplate.Contains("[controller]"))
            {
                routeTemplate = routeTemplate.Replace("[controller]", node.Identifier.Text.Replace("Controller", ""));
            }

            return routeTemplate;
        }

        private ClassDefinition CreateClassInfo(ClassDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            var def = new ClassDefinition
            {
                ClassName = name,
                Properties = new List<(string name, string type)>()
            };

            var properties = node.Members
                .Where(m => m.IsKind(SyntaxKind.PropertyDeclaration))
                .Cast<PropertyDeclarationSyntax>();

            // TODO: Clean this up. Specific classes that can output typescript types.
            foreach (var prop in properties)
            {
                var typeName = "";
                if (prop.Type is IdentifierNameSyntax)
                {
                    typeName = ((IdentifierNameSyntax)prop.Type).Identifier.Text;
                }
                else if (prop.Type is PredefinedTypeSyntax)
                {
                    typeName = ((PredefinedTypeSyntax)prop.Type).Keyword.Text;
                }

                def.Properties.Add((prop.Identifier.Text, typeName));
            }

            return def;
        }
    }

    public class MySyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> ClassCandidates { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax cds)
                ClassCandidates.Add(cds);
        }
    }

    public class ApiDefinition
    {
        public string Url { get; set; }
        public string ControllerName { get; set; }
        public string MethodName { get; set; }
        public string HttpType { get; set; }
        public TypeSyntax ReturnType { get; set; }

        //TODO: This is BAD. Do this properly
        public string ReturnTypeString { get; set; }
    }

    public class ClassDefinition
    {
        public string ClassName { get; set; }
        public List<(string name, string type)> Properties { get; set; }
    }
}
