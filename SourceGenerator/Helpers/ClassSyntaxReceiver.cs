using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace SourceGenerator.Helpers
{
    public class ClassSyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> ClassCandidates { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax cds)
                ClassCandidates.Add(cds);
        }
    }
}
