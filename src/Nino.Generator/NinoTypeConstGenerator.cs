using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Nino.Generator;

[Generator]
public class NinoTypeConstGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var ninoTypeModels = context.GetTypeSyntaxes();
        var compilationAndClasses = context.CompilationProvider.Combine(ninoTypeModels.Collect());
        context.RegisterSourceOutput(compilationAndClasses, (spc, source) => Execute(source.Left, source.Right, spc));
    }

    private static void Execute(Compilation compilation, ImmutableArray<CSharpSyntaxNode> syntaxes,
        SourceProductionContext spc)
    {
        if (!compilation.IsValidCompilation()) return;

        var ninoSymbols = syntaxes.GetNinoTypeSymbols(compilation);

        // get type full names from models (namespaces + type names)
        var serializableTypes = ninoSymbols
            .Where(symbol => symbol.IsReferenceType())
            .Where(symbol => symbol.IsInstanceType()).ToList();

        var types = new StringBuilder();
        foreach (var type in serializableTypes)
        {
            string variableName = type.GetTypeFullName().GetTypeConstName();
            types.AppendLine($"\t\t// {type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
            types.AppendLine($"\t\tpublic const int {variableName} = {type.GetId()};");
        }

        //remove last newline
        types.Remove(types.Length - 1, 1);

        var curNamespace = compilation.AssemblyName!.GetNamespace();

        // generate code
        var code = $$"""
                     // <auto-generated/>

                     using System;

                     namespace {{curNamespace}}
                     {
                         public static class NinoTypeConst
                         {
                     {{types}}
                         }
                     }
                     """;

        spc.AddSource("NinoTypeConst.g.cs", code);
    }
}