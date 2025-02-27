using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Nino.Generator.Template;

namespace Nino.Generator.Collection;

public class CollectionSerializerGenerator(Compilation compilation, List<ITypeSymbol> potentialCollectionSymbols)
    : NinoCollectionGenerator(compilation, potentialCollectionSymbols)
{
    protected override void Generate(SourceProductionContext spc)
    {
        var compilation = Compilation;
        var typeSymbols = PotentialCollectionSymbols;
        var sb = new StringBuilder();

        HashSet<string> addedType = new HashSet<string>();
        foreach (var type in typeSymbols)
        {
            var typeFullName = type.ToDisplayString();
            if (!addedType.Add(typeFullName)) continue;
            sb.GenerateClassSerializeMethods(typeFullName);

            //if type is nullable
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                if (type is INamedTypeSymbol { TypeArguments.Length: 1 } namedTypeSymbol)
                {
                    var fullName = namedTypeSymbol.TypeArguments[0].ToDisplayString();
                    GenerateNullableStructMethods(sb,
                        namedTypeSymbol.TypeArguments[0].GetSerializePrefix(), fullName);
                    continue;
                }
            }

            //if type is KeyValuePair
            if (type.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.KeyValuePair<TKey, TValue>")
            {
                if (type is INamedTypeSymbol { TypeArguments.Length: 2 } namedTypeSymbol)
                {
                    var type1 = namedTypeSymbol.TypeArguments[0].ToDisplayString();
                    var type2 = namedTypeSymbol.TypeArguments[1].ToDisplayString();
                    GenerateKvpStructMethods(sb, namedTypeSymbol.TypeArguments[0].GetSerializePrefix(), type1,
                        namedTypeSymbol.TypeArguments[0].GetSerializePrefix(), type2);
                    continue;
                }
            }

            //if type is array
            if (type is IArrayTypeSymbol)
            {
                sb.AppendLine(GenerateCollectionSerialization(((IArrayTypeSymbol)type).ElementType.GetSerializePrefix(),
                    typeFullName, "Length", "        ", "", "", ((IArrayTypeSymbol)type).ElementType.ToDisplayString(),
                    true, true));
                continue;
            }

            //if type is ICollection
            var i = type.AllInterfaces.FirstOrDefault(namedTypeSymbol =>
                namedTypeSymbol.Name == "ICollection" && namedTypeSymbol.TypeArguments.Length == 1);
            if (i != null)
            {
                sb.AppendLine(GenerateCollectionSerialization(i.TypeArguments[0].GetSerializePrefix(), typeFullName,
                    "Count", "        "));
                continue;
            }

            //if type is Span
            if (type.OriginalDefinition.ToDisplayString() == "System.Span<T>")
            {
                if (type is INamedTypeSymbol { TypeArguments.Length: 1 } ns)
                {
                    sb.AppendLine(GenerateCollectionSerialization(ns.TypeArguments[0].GetSerializePrefix(),
                        typeFullName,
                        "Length", "        ", "", "", ns.TypeArguments[0].ToDisplayString(), false, true));
                    continue;
                }
            }

            //otherwise we add a comment of the error type
            sb.AppendLine($"// Type: {typeFullName} is not supported");
        }

        var curNamespace = compilation.AssemblyName!.GetNamespace();

        // generate code
        var code = $$"""
                     // <auto-generated/>

                     using System;
                     using global::Nino.Core;
                     using System.Buffers;
                     using System.Collections.Generic;
                     using System.Collections.Concurrent;
                     using System.Runtime.InteropServices;
                     using System.Runtime.CompilerServices;

                     namespace {{curNamespace}}
                     {
                         public static partial class Serializer
                         {
                     {{sb}}    }
                     }
                     """;

        spc.AddSource("NinoSerializer.Collection.g.cs", code);
    }

    private static string GenerateCollectionSerialization(string prefix, string collectionType, string lengthName,
        string indent,
        string typeParam = "", string genericConstraint = "", string elementType = "", bool isArray = false,
        bool canUseFor = false)
    {
        var span = canUseFor && isArray ? $"Span<{elementType}> span = value.AsSpan();" : "";
        var collection = isArray && canUseFor ? "span" : "value";
        var loop = canUseFor ? $"for (int i = 0; i < {collection}.Length; i++)" : $"foreach (var item in {collection})";
        var element = canUseFor ? $"{collection}[i]" : "item";
        var ret = $$"""
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static void Serialize{{typeParam}}(this {{collectionType}} value, ref Writer writer) {{genericConstraint}}
                    {
                        if (value == ({{collectionType}}) default)
                        {
                            writer.Write(TypeCollector.NullCollection);
                            return;
                        }
                        writer.Write(TypeCollector.GetCollectionHeader(value.{{lengthName}}));
                        {{span}}
                        {{loop}}
                        {
                            {{prefix}}({{element}}, ref writer);
                        }
                    }

                    """;
        // indent
        ret = ret.Replace("\n", $"\n{indent}");
        return $"{indent}{ret}";
    }

    private static void GenerateNullableStructMethods(StringBuilder sb, string prefix, string typeFullName)
    {
        sb.AppendLine($$"""
                                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                                public static void Serialize(this {{typeFullName}}? value, ref Writer writer)
                                {
                                    if (!value.HasValue)
                                    {
                                        writer.Write(false);
                                        return;
                                    }
                                    
                                    writer.Write(true);
                                    {{prefix}}(value.Value, ref writer);
                                }
                                
                        """);
    }

    private static void GenerateKvpStructMethods(StringBuilder sb, string prefix1, string type1, string prefix2,
        string type2)
    {
        sb.AppendLine($$"""
                                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                                public static void Serialize(this System.Collections.Generic.KeyValuePair<{{type1}}, {{type2}}> value, ref Writer writer)
                                {
                                    {{prefix1}}(value.Key, ref writer);
                                    {{prefix2}}(value.Value, ref writer);
                                }
                                
                        """);
    }
}