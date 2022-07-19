using Microsoft.CodeAnalysis;
using SourceGenerator.Helpers;
using System.Diagnostics;
using System.IO;

namespace SourceGenerator
{
    [Generator]
    public class TypeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif 
            Debug.WriteLine("Initalize code generator");
            context.RegisterForSyntaxNotifications(() => new ClassSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var apiList = new ApiExtractor().FindApiEndpoints(context);

            var classList = new ClassExtractor().CreateClassDefinitions(context, apiList);

            var text = TypescriptWriter.Write(apiList, classList);

            // Doesn't work! Only outputs cs files. 
            // context.AddSource($"apiClient.tsx", builder.ToString());

            // TODO: look at location this is generated. Sometimes it's in the root
            // of the repo and sometimes in the WebApplication folder.
            File.WriteAllText("apiClient.tsx", text);
        }
    }
}
