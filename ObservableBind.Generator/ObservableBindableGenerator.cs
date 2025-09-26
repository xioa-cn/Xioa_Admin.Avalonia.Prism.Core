using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Text;

namespace ObservableBind.Generator;

[Generator]
public class ObservableBindableGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 添加初始化诊断信息
        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource("ObservableBindableGenerator.Init.g.cs",
                "// ObservableBindableGenerator initialized\n" +
                "// This file indicates that the generator has been loaded correctly."));

        // 筛选带有属性的字段声明，增加更多过滤条件
        var fieldDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is FieldDeclarationSyntax field &&
                                               field.AttributeLists.Count > 0 &&
                                               !field.Modifiers.Any(m =>
                                                   m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind
                                                       .StaticKeyword)), // 排除静态字段
                transform: static (ctx, _) => ctx.Node as FieldDeclarationSyntax)
            .Where(static field => field != null);

        var compilationAndFields = context.CompilationProvider.Combine(fieldDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndFields, static (ctx, source) =>
            Execute(ctx, source.Left, source.Right));
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        System.Collections.Immutable.ImmutableArray<FieldDeclarationSyntax?> fields)
    {
        // 添加执行开始的诊断信息
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "OB0000",
                "Debug Info",
                "ObservableBindableGenerator executing with {0} fields",
                "ObservableBind.Generator",
                DiagnosticSeverity.Info,
                true),
            null,
            fields.Length));

        foreach (var field in fields.Where(f => f != null))
        {
            var model = compilation.GetSemanticModel(field!.SyntaxTree);
            // 获取字段符号（注意：FieldDeclaration可能包含多个变量）
            foreach (var variable in field!.Declaration.Variables)
            {
                var fieldSymbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;
                if (fieldSymbol == null) continue;

                // 检查字段是否为只读
                if (fieldSymbol.IsReadOnly)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "OB0003",
                            "ReadOnly field not supported",
                            "Field '{0}' is read-only and cannot be used with ObservableBindableProperty",
                            "ObservableBind.Generator",
                            DiagnosticSeverity.Error,
                            true),
                        variable.GetLocation(),
                        variable.Identifier.Text));
                    continue;
                }

                // 查找我们的自定义属性 - 使用EndsWith以适应不同的程序集名称
                var attribute = fieldSymbol.GetAttributes()
                    .FirstOrDefault(a =>
                        a.AttributeClass?.ToDisplayString().EndsWith("ObservableBindPropertyAttribute") == true);

                // 添加属性查找诊断信息
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "OB0000",
                        "Debug Info",
                        "Field '{0}' has attributes: {1}",
                        "ObservableBind.Generator",
                        DiagnosticSeverity.Info,
                        true),
                    variable.GetLocation(),
                    variable.Identifier.Text,
                    string.Join(", ",
                        fieldSymbol.GetAttributes().Select(a => a.AttributeClass?.ToDisplayString() ?? "unknown"))));

                if (attribute == null) continue;

                var classDecl = field.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                if (classDecl == null) continue;

                // 检查类是否为partial
                if (!classDecl.Modifiers.Any(m =>
                        m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "OB0001",
                            "Class must be partial",
                            "Class '{0}' must be partial to use ObservableBindableProperty",
                            "ObservableBind.Generator",
                            DiagnosticSeverity.Error,
                            true),
                        classDecl.GetLocation(),
                        classDecl.Identifier.Text));
                    continue;
                }

                // 检查类是否实现了必要的接口
                var classSymbol = model.GetDeclaredSymbol(classDecl) as ITypeSymbol;
                bool implementsINotifyPropertyChanged = classSymbol?.AllInterfaces.Any(i =>
                    i.ToDisplayString() == "System.ComponentModel.INotifyPropertyChanged") == true;

                bool implementsINotifyPropertyChanging = classSymbol?.AllInterfaces.Any(i =>
                    i.ToDisplayString() == "System.ComponentModel.INotifyPropertyChanging") == true;

                // 验证接口实现
                if (!implementsINotifyPropertyChanged)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "OB0002",
                            "Missing interface implementation",
                            "Class '{0}' must implement INotifyPropertyChanged to use ObservableBindableProperty",
                            "ObservableBind.Generator",
                            DiagnosticSeverity.Error,
                            true),
                        classDecl.GetLocation(),
                        classDecl.Identifier.Text));
                    continue;
                }

                // 检查是否存在OnPropertyChanged和OnPropertyChanging方法
                bool hasOnPropertyChangedMethod = HasOnPropertyChangedMethod(classSymbol, context, classDecl);
                bool hasOnPropertyChangingMethod = HasOnPropertyChangingMethod(classSymbol, context, classDecl);

                // 处理当前变量
                var propertyName = GetPropertyName(variable, attribute);
                var source = GeneratePropertySource(
                    context, field, variable, propertyName, attribute,
                    hasOnPropertyChangedMethod, hasOnPropertyChangingMethod, classDecl,compilation);

                // 生成唯一的文件名，考虑嵌套类
                var classNameWithNested = GetFullClassName(classDecl);
                var classNameSpace = GetFullNamespace(classDecl);
                context.AddSource(
                    $"{classNameSpace}.{classNameWithNested}_{PropertyName(variable.Identifier.Text)}.g.cs",
                    Microsoft.CodeAnalysis.Text.SourceText.From(source, Encoding.UTF8));
            }
        }
    }

    // 检查类是否有合适的OnPropertyChanged方法
    private static bool HasOnPropertyChangedMethod(ITypeSymbol? classSymbol, SourceProductionContext context,
        ClassDeclarationSyntax classDecl)
    {
        if (classSymbol == null) return false;

        var hasValidMethod = classSymbol.GetMembers("OnPropertyChanged")
            .OfType<IMethodSymbol>()
            .Any(m => m.Parameters.Length == 1 &&
                      m.Parameters[0].Type?.ToDisplayString() == "string" &&
                      m.DeclaredAccessibility == Accessibility.Protected);

        // 只记录信息，不报告错误
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "OB0004",
                "Method check",
                "Class '{0}' {1} a protected OnPropertyChanged(string) method",
                "ObservableBind.Generator",
                DiagnosticSeverity.Info,
                true),
            classDecl.GetLocation(),
            classDecl.Identifier.Text,
            hasValidMethod ? "has" : "does not have"));

        return hasValidMethod;
    }

    // 检查类是否有合适的OnPropertyChanging方法
    private static bool HasOnPropertyChangingMethod(ITypeSymbol? classSymbol, SourceProductionContext context,
        ClassDeclarationSyntax classDecl)
    {
        if (classSymbol == null) return false;

        var hasValidMethod = classSymbol.GetMembers("OnPropertyChanging")
            .OfType<IMethodSymbol>()
            .Any(m => m.Parameters.Length == 1 &&
                      m.Parameters[0].Type?.ToDisplayString() == "string" &&
                      m.DeclaredAccessibility == Accessibility.Protected);

        // 只记录信息，不报告错误
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "OB0005",
                "Method check",
                "Class '{0}' {1} a protected OnPropertyChanging(string) method",
                "ObservableBind.Generator",
                DiagnosticSeverity.Info,
                true),
            classDecl.GetLocation(),
            classDecl.Identifier.Text,
            hasValidMethod ? "has" : "does not have"));

        return hasValidMethod;
    }

    private static string GetPropertyName(VariableDeclaratorSyntax variable, AttributeData attribute)
    {
        // 安全地获取构造函数参数
        if (attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is string name &&
            !string.IsNullOrEmpty(name))
        {
            return name;
        }

        var fieldName = variable.Identifier.Text;

        return PropertyName(fieldName);
    }

    private static string PropertyName(string fieldName)
    {
        // 如果字段名以下划线开头，则移除下划线
        if (fieldName.StartsWith("_"))
        {
            fieldName = fieldName.Substring(1);
        }

        // 确保首字母大写（Pascal Case）
        if (fieldName.Length > 0)
        {
            return char.ToUpper(fieldName[0]) + (fieldName.Length > 1 ? fieldName.Substring(1) : "");
        }

        return fieldName;
    }

    public static string GetFullTypeName(TypeSyntax typeSyntax, SemanticModel semanticModel)
    {
        // 获取类型符号
        var typeInfo = semanticModel.GetTypeInfo(typeSyntax);
        ITypeSymbol? typeSymbol = typeInfo.Type;
    
        if (typeSymbol == null)
            return string.Empty;
        
        // 获取完整限定名（包含命名空间）
        string fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    
        // 对于泛型类型，ToDisplayString 会自动包含类型参数
        // 例如："System.Collections.Generic.List<System.String>"
    
        return fullName;
    }
    private static string GeneratePropertySource(
        SourceProductionContext context,
        FieldDeclarationSyntax field,
        VariableDeclaratorSyntax variable,
        string propertyName,
        AttributeData attribute,
        bool hasOnPropertyChangedMethod,
        bool hasOnPropertyChangingMethod,
        ClassDeclarationSyntax classDecl,
        Compilation compilation)
    {
        var fieldName = variable.Identifier.Text;
        var semanticModel = compilation.GetSemanticModel(field.SyntaxTree);
        string typeName = GetFullTypeName(field.Declaration.Type, semanticModel);
        //var typeName = field.Declaration.Type.ToString();

        // 安全地获取命名参数，提供默认值
        var includeSetter = GetNamedArgumentValue(attribute, "IncludeSetter", true);
        var includeOnChanged = GetNamedArgumentValue(attribute, "IncludeOnChangedMethod", true);
        var includeOnChanging = GetNamedArgumentValue(attribute, "IncludeOnChangingMethod", false);

        // 只有在类中存在相应方法时才调用
        includeOnChanged = includeOnChanged && hasOnPropertyChangedMethod;
        includeOnChanging = includeOnChanging && hasOnPropertyChangingMethod;

        // 添加诊断信息，帮助调试
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "OB0000",
                "Debug Info",
                "Generating property '{0}' for field '{1}' in class '{2}'",
                "ObservableBind.Generator",
                DiagnosticSeverity.Info,
                true),
            variable.GetLocation(),
            propertyName,
            fieldName,
            classDecl.Identifier.Text));

        var source = new StringBuilder();
        source.AppendLine("// <auto-generated/>");
        source.AppendLine("using System.ComponentModel;");
        source.AppendLine();

        // 处理命名空间
        var namespaceDecl = field.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();
        if (namespaceDecl != null)
        {
            source.AppendLine($"namespace {namespaceDecl.Name};");
            source.AppendLine();
        }

        // 处理嵌套类
        var classHierarchy = new List<ClassDeclarationSyntax>();
        var currentClass = classDecl;
        while (currentClass != null)
        {
            classHierarchy.Insert(0, currentClass);
            currentClass = currentClass.Parent as ClassDeclarationSyntax;
        }

        foreach (var cls in classHierarchy.Take(classHierarchy.Count - 1))
        {
            source.AppendLine($"partial class {cls.Identifier.Text}");
            source.AppendLine("{");
        }

        source.AppendLine($"public partial class {classHierarchy.Last().Identifier.Text}");
        source.AppendLine("{");
        source.AppendLine($"    public {typeName} {propertyName}");
        source.AppendLine("    {");
        source.AppendLine($"        get => {fieldName};");

        if (includeSetter)
        {
            source.AppendLine("        set");
            source.AppendLine("        {");
            // 只有值不同时才触发事件和设置值
            source.AppendLine(
                $"            if (!System.Collections.Generic.EqualityComparer<{typeName}>.Default.Equals({fieldName}, value))");
            source.AppendLine("            {");
            
            source.AppendLine($"                On{propertyName}Changing(value);");
            source.AppendLine($"                On{propertyName}Changing({fieldName}, value);");
            source.AppendLine($"                SetProperty(ref {fieldName}, value);");
            source.AppendLine($"                On{propertyName}Changed(value);");
            source.AppendLine($"                On{propertyName}Changed({fieldName}, value);");
            //source.AppendLine($"                {fieldName} = value;");
            // if (includeOnChanging)
            //     source.AppendLine($"                OnPropertyChanging(nameof({propertyName}));");
            // if (includeOnChanged)
            //     source.AppendLine($"                OnPropertyChanged(nameof({propertyName}));");
            source.AppendLine("            }");
            source.AppendLine("        }");
        }

        source.AppendLine("    }");
        
        source.AppendLine("");
        source.AppendLine($"   partial void On{propertyName}Changing({typeName} value);");
        
        source.AppendLine("");
        source.AppendLine($"   partial void On{propertyName}Changing({typeName}? oldValue, {typeName} newValue);");
        
        source.AppendLine("");
        source.AppendLine($"   partial void On{propertyName}Changed({typeName} value);");

        source.AppendLine("");
        source.AppendLine($"   partial void On{propertyName}Changed({typeName}? oldValue, {typeName} newValue);");

        source.AppendLine("}");

       

        // 关闭嵌套类的括号
        foreach (var _ in classHierarchy.Take(classHierarchy.Count - 1))
        {
            source.AppendLine("}");
        }

        return source.ToString();
    }

    private static bool GetNamedArgumentValue(AttributeData attribute, string argumentName, bool defaultValue)
    {
        var namedArg = attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == argumentName);
        if (namedArg.Key == argumentName && namedArg.Value.Value is bool value)
        {
            return value;
        }

        // 修改默认值，与 ObservableBindPropertyAttribute 保持一致
        if (argumentName == "IncludeOnChangedMethod")
        {
            return true; // 确保与属性定义一致
        }

        return defaultValue;
    }

    // 获取完整的类名（包括嵌套类）
    private static string GetFullClassName(ClassDeclarationSyntax classDecl)
    {
        var names = new List<string>();
        var current = classDecl;

        while (current != null)
        {
            names.Insert(0, current.Identifier.Text);
            current = current.Parent as ClassDeclarationSyntax;
        }

        return string.Join("_", names);
    }
    private static string GetFullNamespace(ClassDeclarationSyntax classDecl)
    {
        // 存储命名空间片段
        var nsParts = new List<string>();
    
        // 从类的父节点开始向上查找
        var current = classDecl.Parent;
    
        while (current != null)
        {
            // 处理常规命名空间声明
            if (current is NamespaceDeclarationSyntax namespaceDecl)
            {
                // 命名空间可能包含多个部分（如 A.B.C）
                var nameParts = GetNameParts(namespaceDecl.Name);
                nsParts.InsertRange(0, nameParts);
                break; // 命名空间声明的父级不会再有其他命名空间
            }
            // 处理文件级命名空间
            else if (current is FileScopedNamespaceDeclarationSyntax fileNsDecl)
            {
                var nameParts = GetNameParts(fileNsDecl.Name);
                nsParts.InsertRange(0, nameParts);
                break;
            }
        
            current = current.Parent;
        }
    
        return string.Join(".", nsParts);
    }
    
    private static IEnumerable<string> GetNameParts(NameSyntax name)
    {
        if (name is IdentifierNameSyntax identifier)
        {
            yield return identifier.Identifier.Text;
        }
        else if (name is QualifiedNameSyntax qualified)
        {
            // 递归获取左侧部分
            foreach (var part in GetNameParts(qualified.Left))
            {
                yield return part;
            }
            // 添加右侧部分
            yield return qualified.Right.Identifier.Text;
        }
    }
}