using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace SourceGenerator.Models
{
    public class ApiDefinition
    {
        public string ControllerRoute { get; set; }
        public string MethodRoute { get; set; }
        public string Url => String.IsNullOrEmpty(MethodRoute)
            ? ControllerRoute
            : $"{ControllerRoute}/{MethodRoute}";

        public string ControllerName { get; set; }
        public string MethodName { get; set; }

        public string HttpType { get; set; }
        public TypeSyntax ReturnType { get; set; }

        // TODO: Do something with url arguments.
    }
}
