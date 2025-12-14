using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ludamo.Cache.SourceGenerator;

[Generator]
public class CacheDecoratorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx
            .AddSource("CacheDecoratedAttribute.g.cs",
                SourceText.From(SourceGenerationHelper.CacheDecoratedAttribute, Encoding.UTF8)));

        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(x => x != null)
            .Collect();

        context.RegisterSourceOutput(provider, static (spc, methods) =>
        {
            // methods is an ImmutableArray<WrappedInfo?>; filter nulls and group by (Namespace, Name)
            var methodList = methods.Where(m => m != null).Select(m => m!.Value).ToArray();
            var groups = methodList.GroupBy(m => (m.Namespace, m.Name));

            foreach (var group in groups)
            {
                var representative = group.First();
                var classInfo = new WrappedInfo
                {
                    Namespace = representative.Namespace,
                    Name = representative.Name,
                    Accessibility = representative.Accessibility,
                    InterfaceName = representative.InterfaceName,
                    Methods = [.. group.SelectMany(g => g.Methods)]
                };

                var output = SourceGenerationHelper.GenerateDecoratorWrapper(classInfo);

                spc.AddSource($"{classInfo.Name}CacheDecorator.g.cs", SourceText.From(output, Encoding.UTF8));
                spc.AddSource($"{classInfo.Name}ServiceCollectionExtension.g.cs",
                    SourceText.From(SourceGenerationHelper.GenerateServiceCollectionExtension(classInfo), Encoding.UTF8));
            }
        });
    }

    private static bool Predicate(SyntaxNode node, CancellationToken token)
    {
        return node is MethodDeclarationSyntax;
    }

    private static WrappedInfo? Transform(
        GeneratorSyntaxContext syntaxContext, CancellationToken cancellationToken)
    {
        var methodDeclarationSyntax = (MethodDeclarationSyntax)syntaxContext.Node;

        foreach (var attributeList in methodDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeList.Attributes)
            {
                if (cancellationToken.IsCancellationRequested) return null;

                var attributeName = attributeSyntax.Name.ToString();
                if (attributeName != "CacheDecorated"
                    && attributeName != "CacheDecoratedAttribute")
                    continue;

                var attributeSymbolInfo =
                    syntaxContext.SemanticModel.GetSymbolInfo(attributeSyntax);

                if (attributeSymbolInfo.Symbol is not IMethodSymbol methodSymbol) continue;

                var attributeSymbol = methodSymbol.ContainingType;

                if (attributeSymbol.ToDisplayString() != SourceGenerationHelper.AttributeNamespace
                    && attributeSymbol.ToDisplayString() != SourceGenerationHelper.FullAttributeNamespace)
                    continue;

                return GetDecoratedToGenerate(syntaxContext, methodDeclarationSyntax);
            }
        }

        return null;
    }

    private static WrappedInfo? GetDecoratedToGenerate(GeneratorSyntaxContext syntaxContext
        , MethodDeclarationSyntax methodDeclarationSyntax)
    {
        var methodSemanticSymbol = syntaxContext.SemanticModel
                            .GetDeclaredSymbol(methodDeclarationSyntax) as IMethodSymbol;

        string? specificReturnType = null;
        if (methodSemanticSymbol!.IsAsync)
        {
            var returnType = (methodSemanticSymbol.ReturnType as INamedTypeSymbol)!;
            specificReturnType = returnType.IsGenericType ? returnType.TypeArguments[0].ToDisplayString() : "void";
        }
        else
        {
            specificReturnType = methodSemanticSymbol.ReturnType.ToDisplayString();
        }

        var firstMethodArgument = methodSemanticSymbol.Parameters.Select(p => new MethodParameterInfo
        {
            Name = p.Name,
            Type = p.Type.ToDisplayString()
        }).FirstOrDefault();

        var arguments = methodSemanticSymbol.GetAttributes()
            .FirstOrDefault(ad => ad.AttributeClass!.ToDisplayString() ==
               SourceGenerationHelper.AttributeNamespace
                || ad.AttributeClass!.ToDisplayString() ==
                SourceGenerationHelper.FullAttributeNamespace)
            ?.ConstructorArguments ?? [];
        int? expiresInSeconds = null;
        if (arguments != null && arguments.Length == 1)
        {
            var arg = arguments[0];
            if (arg.Value != null)
            {
                expiresInSeconds = (int?)arg.Value;
            }
        }
        
        var interfaces = methodSemanticSymbol.ContainingType.AllInterfaces;
        
        string? interfaceName = null;
        if (interfaces.Length > 0)
        {
            var methodInterface = interfaces.FirstOrDefault(i =>
                i.GetMembers().OfType<IMethodSymbol>().Any(m => m.Name == methodSemanticSymbol.Name));
            if (methodInterface is not null)
            {
                interfaceName = $"{methodInterface.ContainingNamespace.ToDisplayString()}.{methodInterface.Name}";
            }
        }

        var classInfo = new WrappedInfo
        {
            Namespace = methodSemanticSymbol!.ContainingNamespace.ToDisplayString(),
            Name = methodSemanticSymbol!.ContainingType.Name,
            Accessibility = methodSemanticSymbol!.ContainingType.DeclaredAccessibility,
            InterfaceName = interfaceName,
            Methods = [new MethodDetail
            {
                MethodName = methodSemanticSymbol!.Name,
                ReturnType = methodSemanticSymbol!.ReturnType.ToDisplayString(),
                SpecificReturnType = specificReturnType,
                IsAsync = methodSemanticSymbol.IsAsync,
                KeyArgument = firstMethodArgument,
                ExpiresInSeconds = expiresInSeconds,
            }]
        };

        return classInfo;
    }
}
