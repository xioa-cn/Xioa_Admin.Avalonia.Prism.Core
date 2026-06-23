using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Constructor.Generator;

[Generator]
public sealed class ConstructorGenerator : IIncrementalGenerator
{
    private const string ConstructorAttributeName = "Ava.Xioa.Common.Attributes.ConstructorAttribute";
    private const string ServiceAttributeMetadataName = "Ava.Xioa.Common.Attributes.ServiceAttribute`1";
    private const string GeneratedNamespace = "Ava.Xioa.Common.Generated";

    private static readonly DiagnosticDescriptor MultipleConstructorsDescriptor = new(
        "CG0001",
        "Multiple constructors selected",
        "Type '{0}' has multiple constructors marked with ConstructorAttribute",
        "Constructor.Generator",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor UnsupportedTypeDescriptor = new(
        "CG0002",
        "Unsupported constructor factory type",
        "Type '{0}' must be a non-generic class with an accessible constructor to generate a constructor factory",
        "Constructor.Generator",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor OpenGenericServiceDescriptor = new(
        "CG0003",
        "Unsupported generic service type",
        "ServiceAttribute on type '{0}' uses an open generic service type '{1}', which cannot be registered by this factory generator",
        "Constructor.Generator",
        DiagnosticSeverity.Error,
        true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var constructors = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ConstructorDeclarationSyntax constructor && constructor.AttributeLists.Count > 0,
                static (ctx, _) => GetConstructor(ctx))
            .Where(static constructor => constructor is not null);

        var compilationAndConstructors = context.CompilationProvider.Combine(constructors.Collect());

        context.RegisterSourceOutput(compilationAndConstructors, static (ctx, source) =>
            Execute(ctx, source.Left, source.Right));
    }

    private static IMethodSymbol? GetConstructor(GeneratorSyntaxContext context)
    {
        var constructor = (ConstructorDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(constructor);
        if (symbol is null)
        {
            return null;
        }

        foreach (var attribute in symbol.GetAttributes())
        {
            if (IsAttribute(attribute, ConstructorAttributeName))
            {
                return symbol;
            }
        }

        return null;
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<IMethodSymbol?> constructors)
    {
        var registrations = new List<RegistrationModel>();

        if (!constructors.IsDefaultOrEmpty)
        {
            foreach (var group in constructors
                         .Where(static constructor => constructor is not null)
                         .Cast<IMethodSymbol>()
                         .GroupBy(static constructor => constructor.ContainingType, SymbolEqualityComparer.Default))
            {
                var constructorList = group.ToArray();
                var type = constructorList[0].ContainingType;

                if (constructorList.Length > 1)
                {
                    foreach (var constructor in constructorList)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            MultipleConstructorsDescriptor,
                            constructor.Locations.FirstOrDefault(),
                            type.ToDisplayString()));
                    }

                    continue;
                }

                var selectedConstructor = constructorList[0];
                if (!CanGenerateFactory(type, selectedConstructor))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        UnsupportedTypeDescriptor,
                        selectedConstructor.Locations.FirstOrDefault(),
                        type.ToDisplayString()));
                    continue;
                }

                var registration = CreateRegistrationModel(type, selectedConstructor);
                if (registration.ServiceType is INamedTypeSymbol serviceType &&
                    serviceType.IsGenericType &&
                    serviceType.TypeArguments.Any(static argument => argument.TypeKind == TypeKind.TypeParameter))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        OpenGenericServiceDescriptor,
                        selectedConstructor.Locations.FirstOrDefault(),
                        type.ToDisplayString(),
                        serviceType.ToDisplayString()));
                    continue;
                }

                registrations.Add(registration);
            }
        }

        var assemblyName = SanitizeIdentifier(compilation.AssemblyName ?? "Application");
        var factoryMethods = GetReferencedFactoryMethods(compilation);
        if (registrations.Count > 0)
        {
            factoryMethods.Add(new FactoryMethodModel(
                $"global::{GeneratedNamespace}.{assemblyName}ConstructorFactoryExtensions",
                $"Add{assemblyName}ConstructorFactories"));
        }

        var source = GenerateSource(assemblyName, registrations, factoryMethods);
        context.AddSource(
            $"{assemblyName}.ConstructorFactories.g.cs",
            SourceText.From(source, Encoding.UTF8));
    }

    private static bool CanGenerateFactory(INamedTypeSymbol type, IMethodSymbol constructor)
    {
        return type.TypeKind == TypeKind.Class &&
               !type.IsStatic &&
               !type.IsAbstract &&
               !type.IsGenericType &&
               constructor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;
    }

    private static RegistrationModel CreateRegistrationModel(INamedTypeSymbol implementationType, IMethodSymbol constructor)
    {
        var serviceType = (ITypeSymbol)implementationType;
        var lifetime = "Singleton";
        string? serviceName = null;

        foreach (var attribute in implementationType.GetAttributes())
        {
            if (!IsServiceAttribute(attribute))
            {
                continue;
            }

            if (attribute.AttributeClass is INamedTypeSymbol { TypeArguments.Length: 1 } attributeType)
            {
                serviceType = attributeType.TypeArguments[0];
            }

            foreach (var namedArgument in attribute.NamedArguments)
            {
                if (namedArgument.Key == "ServiceLifetime" &&
                    namedArgument.Value.Value is int namedLifetime)
                {
                    lifetime = GetServiceLifetimeName(namedLifetime);
                }
                else if (namedArgument.Key == "ServiceName" &&
                         namedArgument.Value.Value is string namedServiceName &&
                         namedServiceName.Length > 0)
                {
                    serviceName = namedServiceName;
                }
            }

            if (attribute.ConstructorArguments.Length == 1)
            {
                var argument = attribute.ConstructorArguments[0];
                if (argument.Value is int constructorLifetime)
                {
                    lifetime = GetServiceLifetimeName(constructorLifetime);
                }
                else if (argument.Value is string constructorServiceName && constructorServiceName.Length > 0)
                {
                    serviceName = constructorServiceName;
                }
            }
            else if (attribute.ConstructorArguments.Length == 2)
            {
                if (attribute.ConstructorArguments[0].Value is string constructorServiceName && constructorServiceName.Length > 0)
                {
                    serviceName = constructorServiceName;
                }

                if (attribute.ConstructorArguments[1].Value is int constructorLifetime)
                {
                    lifetime = GetServiceLifetimeName(constructorLifetime);
                }
            }

            break;
        }

        return new RegistrationModel(
            serviceType,
            implementationType,
            constructor.Parameters.ToImmutableArray(),
            lifetime,
            serviceName);
    }

    private static string GenerateSource(
        string assemblyName,
        List<RegistrationModel> registrations,
        List<FactoryMethodModel> factoryMethods)
    {
        var source = new StringBuilder();
        source.AppendLine("// <auto-generated/>");
        source.AppendLine("#nullable enable");
        source.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        source.AppendLine();
        source.Append("namespace ");
        source.Append(GeneratedNamespace);
        source.AppendLine(";");
        source.AppendLine();
        source.Append("public static class ");
        source.Append(assemblyName);
        source.AppendLine("ConstructorFactoryExtensions");
        source.AppendLine("{");

        if (registrations.Count > 0)
        {
            source.Append("    public static IServiceCollection Add");
            source.Append(assemblyName);
            source.AppendLine("ConstructorFactories(this IServiceCollection services)");
            source.AppendLine("    {");
            source.AppendLine("        System.ArgumentNullException.ThrowIfNull(services);");
            source.AppendLine();

            foreach (var registration in registrations.OrderBy(static registration => registration.ImplementationType.ToDisplayString()))
            {
                AppendRegistration(source, registration);
            }

            source.AppendLine("        return services;");
            source.AppendLine("    }");
            source.AppendLine();
        }

        source.AppendLine("    public static IServiceCollection AddConstructorFactories(this IServiceCollection services)");
        source.AppendLine("    {");
        source.AppendLine("        System.ArgumentNullException.ThrowIfNull(services);");
        source.AppendLine();

        foreach (var factoryMethod in factoryMethods
                     .Distinct(FactoryMethodModelComparer.Instance)
                     .OrderBy(static method => method.TypeName)
                     .ThenBy(static method => method.MethodName))
        {
            source.Append("        ");
            source.Append(factoryMethod.TypeName);
            source.Append('.');
            source.Append(factoryMethod.MethodName);
            source.AppendLine("(services);");
        }

        source.AppendLine("        return services;");
        source.AppendLine("    }");
        source.AppendLine("}");

        return source.ToString();
    }

    private static List<FactoryMethodModel> GetReferencedFactoryMethods(Compilation compilation)
    {
        var factoryMethods = new List<FactoryMethodModel>();

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
            {
                continue;
            }

            var generatedNamespace = GetNamespace(assembly.GlobalNamespace, GeneratedNamespace);
            if (generatedNamespace is null)
            {
                continue;
            }

            foreach (var type in generatedNamespace.GetTypeMembers())
            {
                foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
                {
                    if (!IsFactoryMethod(method))
                    {
                        continue;
                    }

                    factoryMethods.Add(new FactoryMethodModel(
                        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        method.Name));
                }
            }
        }

        return factoryMethods;
    }

    private static INamespaceSymbol? GetNamespace(INamespaceSymbol root, string namespaceName)
    {
        var current = root;
        foreach (var part in namespaceName.Split('.'))
        {
            current = current.GetNamespaceMembers()
                .FirstOrDefault(member => string.Equals(member.Name, part, StringComparison.Ordinal));
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    private static bool IsFactoryMethod(IMethodSymbol method)
    {
        return method is
               {
                   DeclaredAccessibility: Accessibility.Public,
                   IsStatic: true,
                   Parameters.Length: 1
               } &&
               method.Name.StartsWith("Add", StringComparison.Ordinal) &&
               method.Name.EndsWith("ConstructorFactories", StringComparison.Ordinal) &&
               !string.Equals(method.Name, "AddConstructorFactories", StringComparison.Ordinal) &&
               method.Parameters[0].Type.ToDisplayString() == "Microsoft.Extensions.DependencyInjection.IServiceCollection" &&
               method.ReturnType.ToDisplayString() == "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    }

    private static void AppendRegistration(StringBuilder source, RegistrationModel registration)
    {
        var serviceTypeName = registration.ServiceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var implementationTypeName = registration.ImplementationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        source.Append("        services.Add");
        if (registration.ServiceName is { Length: > 0 })
        {
            source.Append("Keyed");
        }

        source.Append(registration.Lifetime);
        source.Append('<');
        source.Append(serviceTypeName);
        source.Append(">(");

        if (registration.ServiceName is { Length: > 0 })
        {
            source.Append(SymbolDisplay.FormatLiteral(registration.ServiceName, true));
            source.Append(", ");
        }

        if (registration.ServiceName is { Length: > 0 })
        {
            source.Append("(sp, _) => ");
        }
        else
        {
            source.Append("sp => ");
        }

        source.Append("new ");
        source.Append(implementationTypeName);
        source.Append('(');

        for (var i = 0; i < registration.Parameters.Length; i++)
        {
            if (i > 0)
            {
                source.Append(", ");
            }

            var parameterTypeName = registration.Parameters[i].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            source.Append("sp.GetRequiredService<");
            source.Append(parameterTypeName);
            source.Append(">()");
        }

        source.AppendLine("));");
        source.AppendLine();
    }

    private static bool IsServiceAttribute(AttributeData attribute)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass is null)
        {
            return false;
        }

        var originalDefinition = attributeClass.OriginalDefinition.ToDisplayString();
        return string.Equals(originalDefinition, ServiceAttributeMetadataName, StringComparison.Ordinal) ||
               string.Equals(attributeClass.MetadataName, "ServiceAttribute`1", StringComparison.Ordinal);
    }

    private static bool IsAttribute(AttributeData attribute, string metadataName)
    {
        var attributeClass = attribute.AttributeClass;
        return attributeClass is not null &&
               string.Equals(attributeClass.ToDisplayString(), metadataName, StringComparison.Ordinal);
    }

    private static string GetServiceLifetimeName(int value)
    {
        switch (value)
        {
            case 0:
                return "Singleton";
            case 1:
                return "Scoped";
            case 2:
                return "Transient";
            default:
                return "Singleton";
        }
    }

    private static string SanitizeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(SyntaxFacts.IsIdentifierPartCharacter(ch) ? ch : '_');
        }

        if (builder.Length == 0 || !SyntaxFacts.IsIdentifierStartCharacter(builder[0]))
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    private sealed class RegistrationModel
    {
        public RegistrationModel(
            ITypeSymbol serviceType,
            INamedTypeSymbol implementationType,
            ImmutableArray<IParameterSymbol> parameters,
            string lifetime,
            string? serviceName)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Parameters = parameters;
            Lifetime = lifetime;
            ServiceName = serviceName;
        }

        public ITypeSymbol ServiceType { get; }
        public INamedTypeSymbol ImplementationType { get; }
        public ImmutableArray<IParameterSymbol> Parameters { get; }
        public string Lifetime { get; }
        public string? ServiceName { get; }
    }

    private sealed class FactoryMethodModel
    {
        public FactoryMethodModel(string typeName, string methodName)
        {
            TypeName = typeName;
            MethodName = methodName;
        }

        public string TypeName { get; }
        public string MethodName { get; }
    }

    private sealed class FactoryMethodModelComparer : IEqualityComparer<FactoryMethodModel>
    {
        public static readonly FactoryMethodModelComparer Instance = new();

        public bool Equals(FactoryMethodModel? x, FactoryMethodModel? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return string.Equals(x.TypeName, y.TypeName, StringComparison.Ordinal) &&
                   string.Equals(x.MethodName, y.MethodName, StringComparison.Ordinal);
        }

        public int GetHashCode(FactoryMethodModel obj)
        {
            unchecked
            {
                return ((obj.TypeName.GetHashCode() * 397) ^ obj.MethodName.GetHashCode());
            }
        }
    }
}
