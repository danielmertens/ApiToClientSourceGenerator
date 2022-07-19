using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Helpers
{
    public class ApiExtractor
    {
        private const string RouteAttributeName = "Route";
        private const string HttpGetAttributeName = "HttpGet";

        public IEnumerable<ApiDefinition> FindApiEndpoints(GeneratorExecutionContext context)
        {
            var classReceiver = (ClassSyntaxReceiver)context.SyntaxReceiver;
            var apiList = new List<ApiDefinition>();

            foreach (var node in classReceiver.ClassCandidates)
            {
                // The node is a controller class
                if (node.AttributeLists != null && HasAttribute(node.AttributeLists, RouteAttributeName))
                {
                    apiList.AddRange(CreateApiInfo(node, context));
                }
            }

            return apiList;
        }

        private bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
            => attributeLists.Any(a => a.Attributes.Any(att => att.Name.ToString() == attributeName));

        private List<ApiDefinition> CreateApiInfo(ClassDeclarationSyntax controller, GeneratorExecutionContext context)
        {
            var definitionList = new List<ApiDefinition>();

            // Get className without controller sufix
            var controllerName = controller.Identifier.Text.Replace("Controller", "");

            // Get controller route
            var controllerRoute = GetControllerRoute(controller, context.Compilation);

            // Get list of API methods in class.
            var methods = controller.Members
                .Where(member => member.IsKind(SyntaxKind.MethodDeclaration))
                .Cast<MethodDeclarationSyntax>()
                .Where(IsEndpointMethod);

            foreach (var method in methods)
            {
                var definition = ExtractApiDefinition(method);
                if (definition == null) continue;

                definition.ControllerName = controllerName;
                definition.ControllerRoute = controllerRoute;
                definitionList.Add(definition);
            }

            return definitionList;
        }

        private bool IsEndpointMethod(MethodDeclarationSyntax method)
        {
            var attributeLists = method.AttributeLists;
            return HasAttribute(attributeLists, HttpGetAttributeName);
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

        private ApiDefinition ExtractApiDefinition(MethodDeclarationSyntax method)
        {
            var attributes = method.AttributeLists
                .SelectMany(list => list.Attributes)
                .Select(attribute => attribute.Name)
                .Cast<IdentifierNameSyntax>()
                .Select(name => name.Identifier.Text)
                .ToList();

            // Only support get for now
            if (attributes.Contains(HttpGetAttributeName))
            {
                return ExtractGetApiDefinition(method);
            }

            return null;
        }

        private ApiDefinition ExtractGetApiDefinition(MethodDeclarationSyntax method)
        {
            var methodName = method.Identifier.Text;

            // Get path from attribute
            var getAttribute = method.AttributeLists
                .SelectMany(list => list.Attributes)
                .Where(attribute => ((IdentifierNameSyntax)attribute.Name).Identifier.Text == HttpGetAttributeName)
                .FirstOrDefault();

            var getPath = string.Empty;

            if (getAttribute.ArgumentList != null && getAttribute.ArgumentList.Arguments.Count > 0)
            {
                getPath = getAttribute.ArgumentList.Arguments[0].Expression.ToString();
                getPath = getPath.Replace("\"", "");
            }

            var def = new ApiDefinition
            {
                MethodName = methodName,
                HttpType = "GET",
                MethodRoute = getPath,
                ReturnType = method.ReturnType
            };

            return def;
        }
    }
}
