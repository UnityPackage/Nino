using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Nino.Generator.Template;

namespace Nino.Generator.Common;

public class TypeConstGenerator(
    Compilation compilation,
    List<ITypeSymbol> ninoSymbols,
    Dictionary<string, List<string>> inheritanceMap,
    Dictionary<string, List<string>> subTypeMap,
    ImmutableArray<string> topNinoTypes)
    : NinoCommonGenerator(compilation, ninoSymbols, inheritanceMap, subTypeMap, topNinoTypes)
{
    protected override void Generate(SourceProductionContext spc)
    {
        var compilation = Compilation;
        var ninoSymbols = NinoSymbols;

        // get type full names from models (namespaces + type names)
        var serializableTypes = ninoSymbols
            .Where(symbol => symbol.IsPolymorphicType())
            .Where(symbol => symbol.IsInstanceType()).ToList();

        var types = new StringBuilder();
        foreach (var type in serializableTypes)
        {
            string variableName = type.GetTypeFullName().GetTypeConstName();
            types.AppendLine($"\t\t// {type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
            types.AppendLine($"\t\tpublic const int {variableName} = {type.GetId()};");
        }

        //remove last newline
        if (types.Length > 0)
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