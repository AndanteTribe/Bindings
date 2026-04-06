using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bindings;

/// <summary>
/// Source generator that processes classes annotated with [ViewModel] and generates:
/// <list type="bullet">
///   <item>A partial ViewModel class with public properties, constructor, and helper methods.</item>
///   <item>A sealed partial View class that binds to the ViewModel.</item>
/// </list>
/// </summary>
[Generator]
public class ViewModelGenerator : IIncrementalGenerator
{
    private const string ViewModelAttributeFullName = "Bindings.ViewModelAttribute";
    private const string ModelAttributeFullName = "Bindings.ModelAttribute";
    private const string SchemaAttributeFullName = "Bindings.SchemaAttribute";
    private const string SerializableAttributeFullName = "System.SerializableAttribute";

    // ──────────────────────────────────────────────────────────
    //  Data structures
    // ──────────────────────────────────────────────────────────

    private sealed class ViewModelInfo
    {
        public string Namespace { get; }
        public string ClassName { get; }
        public bool RequireBindImplementation { get; }
        public bool AlreadySerializable { get; }
        public ImmutableArray<ModelField> Models { get; }
        public ImmutableArray<FieldSchemaInfo> FieldSchemas { get; }
        public ImmutableArray<MethodSchemaInfo> MethodSchemas { get; }

        public ViewModelInfo(
            string namespaceName,
            string className,
            bool requireBindImplementation,
            bool alreadySerializable,
            ImmutableArray<ModelField> models,
            ImmutableArray<FieldSchemaInfo> fieldSchemas,
            ImmutableArray<MethodSchemaInfo> methodSchemas)
        {
            Namespace = namespaceName;
            ClassName = className;
            RequireBindImplementation = requireBindImplementation;
            AlreadySerializable = alreadySerializable;
            Models = models;
            FieldSchemas = fieldSchemas;
            MethodSchemas = methodSchemas;
        }
    }

    private sealed class ModelField
    {
        /// <summary>Fully-qualified type name, e.g. "global::Bindings.Sample.CountModel"</summary>
        public string TypeFullName { get; }
        /// <summary>Field name as written, e.g. "_model"</summary>
        public string FieldName { get; }
        /// <summary>Constructor parameter name, e.g. "model"</summary>
        public string ParamName { get; }

        public ModelField(string typeFullName, string fieldName, string paramName)
        {
            TypeFullName = typeFullName;
            FieldName = fieldName;
            ParamName = paramName;
        }
    }

    private sealed class FieldSchemaInfo
    {
        /// <summary>Backing field name, e.g. "_count"</summary>
        public string FieldName { get; }
        /// <summary>Generated public property name, e.g. "Count"</summary>
        public string PropertyName { get; }
        /// <summary>Fully-qualified field type, e.g. "int"</summary>
        public string TypeFullName { get; }
        /// <summary>Binding path, e.g. "TMPro.TMP_Text.text"</summary>
        public string BindingPath { get; }
        public int Id { get; }
        public string Format { get; }

        public FieldSchemaInfo(string fieldName, string propertyName, string typeFullName, string bindingPath, int id, string format)
        {
            FieldName = fieldName;
            PropertyName = propertyName;
            TypeFullName = typeFullName;
            BindingPath = bindingPath;
            Id = id;
            Format = format;
        }
    }

    private sealed class MethodSchemaInfo
    {
        /// <summary>Method name, e.g. "Increment"</summary>
        public string MethodName { get; }
        /// <summary>Binding path, e.g. "UnityEngine.UI.Button.onClick"</summary>
        public string BindingPath { get; }
        public int Id { get; }

        public MethodSchemaInfo(string methodName, string bindingPath, int id)
        {
            MethodName = methodName;
            BindingPath = bindingPath;
            Id = id;
        }
    }

    /// <summary>Represents a unique UI component field in the generated View class.</summary>
    private sealed class ViewComponent
    {
        /// <summary>Fully-qualified component type, e.g. "global::UnityEngine.UI.Button"</summary>
        public string FullTypeName { get; }
        /// <summary>Base field name (lowercase class name without TMP_ prefix), e.g. "button"</summary>
        public string BaseName { get; }
        /// <summary>Assigned View field name, e.g. "_button1"</summary>
        public string FieldName { get; set; } = "";
        /// <summary>Whether the component type lives in the TMPro namespace.</summary>
        public bool IsTMPro { get; }
        /// <summary>The member name from the binding path, e.g. "text", "onClick", "interactable"</summary>
        public string MemberName { get; }

        public List<FieldSchemaInfo> FieldSchemas { get; } = new List<FieldSchemaInfo>();
        public List<MethodSchemaInfo> MethodSchemas { get; } = new List<MethodSchemaInfo>();

        public ViewComponent(string fullTypeName, string baseName, bool isTMPro, string memberName)
        {
            FullTypeName = fullTypeName;
            BaseName = baseName;
            IsTMPro = isTMPro;
            MemberName = memberName;
        }
    }

    // ──────────────────────────────────────────────────────────
    //  IIncrementalGenerator
    // ──────────────────────────────────────────────────────────

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
                transform: GetViewModelInfo)
            .Where(static info => info is not null);

        context.RegisterSourceOutput(provider, static (ctx, info) => GenerateCode(ctx, info!));
    }

    // ──────────────────────────────────────────────────────────
    //  Metadata extraction
    // ──────────────────────────────────────────────────────────

    private static ViewModelInfo? GetViewModelInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(classDecl, cancellationToken) is not INamedTypeSymbol classSymbol)
            return null;

        // Must have [ViewModel] attribute
        var viewModelAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == ViewModelAttributeFullName);
        if (viewModelAttr is null)
            return null;

        bool requireBindImplementation = false;
        if (viewModelAttr.ConstructorArguments.Length > 0 && viewModelAttr.ConstructorArguments[0].Value is bool b)
            requireBindImplementation = b;

        bool alreadySerializable = classSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == SerializableAttributeFullName);

        string namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? ""
            : classSymbol.ContainingNamespace.ToDisplayString();

        string className = classSymbol.Name;

        // [Model] fields
        var models = classSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == ModelAttributeFullName))
            .Select(f => new ModelField(
                f.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                f.Name,
                StripLeadingUnderscore(f.Name)))
            .ToImmutableArray();

        // [Schema] fields (a field may have multiple [Schema] attributes)
        var fieldSchemas = classSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .SelectMany(f => f.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == SchemaAttributeFullName)
                .Select(a =>
                {
                    var (bindingPath, id, format) = ReadSchemaArgs(a);
                    return new FieldSchemaInfo(
                        f.Name,
                        FieldToPropertyName(f.Name),
                        f.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        bindingPath,
                        id,
                        format);
                }))
            .ToImmutableArray();

        // [Schema] methods
        var methodSchemas = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .SelectMany(m => m.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == SchemaAttributeFullName)
                .Select(a =>
                {
                    var (bindingPath, id, _) = ReadSchemaArgs(a);
                    return new MethodSchemaInfo(m.Name, bindingPath, id);
                }))
            .ToImmutableArray();

        return new ViewModelInfo(namespaceName, className, requireBindImplementation, alreadySerializable, models, fieldSchemas, methodSchemas);
    }

    private static (string bindingPath, int id, string format) ReadSchemaArgs(AttributeData attr)
    {
        string bindingPath = "";
        int id = 0;
        string format = "";

        var args = attr.ConstructorArguments;
        if (args.Length > 0 && args[0].Value is string bp) bindingPath = bp;
        if (args.Length > 1 && args[1].Value is int i) id = i;
        if (args.Length > 2 && args[2].Value is string f) format = f;

        foreach (var named in attr.NamedArguments)
        {
            if (named.Key == "Id" && named.Value.Value is int ni) id = ni;
            else if (named.Key == "Format" && named.Value.Value is string nf) format = nf;
        }

        return (bindingPath, id, format);
    }

    // ──────────────────────────────────────────────────────────
    //  Code generation dispatch
    // ──────────────────────────────────────────────────────────

    private static void GenerateCode(SourceProductionContext context, ViewModelInfo info)
    {
        var vmCode = GenerateViewModel(info);
        context.AddSource($"{info.ClassName}.ViewModel.g.cs", SourceText.From(vmCode, Encoding.UTF8));

        var viewClassName = info.ClassName.Replace("ViewModel", "View");
        var viewCode = GenerateView(info, viewClassName);
        context.AddSource($"{viewClassName}.g.cs", SourceText.From(viewCode, Encoding.UTF8));
    }

    // ──────────────────────────────────────────────────────────
    //  ViewModel code generation
    // ──────────────────────────────────────────────────────────

    private static string GenerateViewModel(ViewModelInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        bool hasNamespace = !string.IsNullOrEmpty(info.Namespace);
        if (hasNamespace)
        {
            sb.AppendLine($"namespace {info.Namespace}");
            sb.AppendLine("{");
        }

        string indent = hasNamespace ? "    " : "";

        // [Serializable] attribute (only if not already present on user class)
        if (!info.AlreadySerializable)
            sb.AppendLine($"{indent}[global::System.Serializable]");

        sb.AppendLine($"{indent}public partial class {info.ClassName} : global::Bindings.IViewModel");
        sb.AppendLine($"{indent}{{");

        string i2 = indent + "    ";

        // _publisher field
        sb.AppendLine($"{i2}private readonly global::Bindings.IMvvmPublisher _publisher;");

        // Public properties for each [Schema] field
        foreach (var schema in info.FieldSchemas)
        {
            sb.AppendLine();
            sb.AppendLine($"{i2}public {schema.TypeFullName} {schema.PropertyName}");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i2}    get => {schema.FieldName};");
            sb.AppendLine($"{i2}    set");
            sb.AppendLine($"{i2}    {{");
            sb.AppendLine($"{i2}        {schema.FieldName} = value;");
            sb.AppendLine($"{i2}        PublishRebindMessage();");
            sb.AppendLine($"{i2}    }}");
            sb.AppendLine($"{i2}}}");
        }

        // Constructor
        sb.AppendLine();
        var ctorParams = BuildConstructorParams(info);
        sb.AppendLine($"{i2}public {info.ClassName}({ctorParams})");
        sb.AppendLine($"{i2}{{");
        foreach (var model in info.Models)
            sb.AppendLine($"{i2}    {model.FieldName} = {model.ParamName};");
        sb.AppendLine($"{i2}    _publisher = publisher;");
        sb.AppendLine($"{i2}}}");

        // NotifyCompletedBind
        sb.AppendLine();
        sb.AppendLine($"{i2}public void NotifyCompletedBind() => OnPostBind();");

        // partial void OnPostBind
        sb.AppendLine();
        sb.AppendLine($"{i2}partial void OnPostBind();");

        // PublishRebindMessage
        sb.AppendLine();
        sb.AppendLine($"{i2}[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{i2}private void PublishRebindMessage()");
        sb.AppendLine($"{i2}{{");
        sb.AppendLine($"{i2}    _publisher.PublishRebindMessage<{info.ClassName}>();");
        sb.AppendLine($"{i2}}}");

        sb.AppendLine($"{indent}}}");

        if (hasNamespace)
            sb.AppendLine("}");

        return sb.ToString();
    }

    private static string BuildConstructorParams(ViewModelInfo info)
    {
        var parts = new List<string>();
        foreach (var model in info.Models)
            parts.Add($"{model.TypeFullName} {model.ParamName}");
        parts.Add("global::Bindings.IMvvmPublisher publisher");
        return string.Join(", ", parts);
    }

    // ──────────────────────────────────────────────────────────
    //  View code generation
    // ──────────────────────────────────────────────────────────

    private static string GenerateView(ViewModelInfo info, string viewClassName)
    {
        var components = ComputeViewComponents(info);
        string fqViewModel = string.IsNullOrEmpty(info.Namespace)
            ? $"global::{info.ClassName}"
            : $"global::{info.Namespace}.{info.ClassName}";

        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        bool hasNamespace = !string.IsNullOrEmpty(info.Namespace);
        if (hasNamespace)
        {
            sb.AppendLine($"namespace {info.Namespace}");
            sb.AppendLine("{");
        }

        string indent = hasNamespace ? "    " : "";
        string i2 = indent + "    ";
        string i3 = i2 + "    ";

        sb.AppendLine($"{indent}[global::System.Serializable]");
        sb.AppendLine($"{indent}public sealed partial class {viewClassName} : global::Bindings.IView<{fqViewModel}>");
        sb.AppendLine($"{indent}{{");

        // _viewModel field
        sb.AppendLine($"{i2}[global::System.NonSerialized]");
        sb.AppendLine($"{i2}private {fqViewModel} _viewModel = null!;");

        // UI component fields (field-schema components first, then method-schema components)
        foreach (var comp in components)
        {
            sb.AppendLine();
            sb.AppendLine($"{i2}[global::UnityEngine.SerializeField]");
            sb.AppendLine($"{i2}private {comp.FullTypeName} {comp.FieldName} = null!;");
        }

        // Initialize
        sb.AppendLine();
        sb.AppendLine($"{i2}void global::Bindings.IView<{fqViewModel}>.Initialize({fqViewModel} viewModel)");
        sb.AppendLine($"{i2}{{");
        sb.AppendLine($"{i3}_viewModel = viewModel;");
        sb.AppendLine($"{i2}}}");

        // BindAsync (only when NOT requireBindImplementation)
        if (!info.RequireBindImplementation)
        {
            sb.AppendLine();
            sb.AppendLine($"{i2}global::System.Threading.Tasks.ValueTask global::Bindings.IView.BindAsync(global::System.Threading.CancellationToken _)");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}BindAll();");
            sb.AppendLine($"{i3}return default;");
            sb.AppendLine($"{i2}}}");
        }

        // BindAll
        sb.AppendLine();
        sb.AppendLine($"{i2}private void BindAll()");
        sb.AppendLine($"{i2}{{");
        AppendBindAllBody(sb, i3, info, components);
        sb.AppendLine($"{i3}OnPostBind();");
        sb.AppendLine($"{i3}_viewModel.NotifyCompletedBind();");
        sb.AppendLine($"{i2}}}");

        // partial void OnPostBind
        sb.AppendLine();
        sb.AppendLine($"{i2}partial void OnPostBind();");

        sb.AppendLine($"{indent}}}");

        // Debug subscriber section
        sb.AppendLine();
        sb.AppendLine($"#if UNITY_EDITOR || DEVELOPMENT_BUILD || !DISABLE_DEBUGTOOLKIT");
        sb.AppendLine($"{indent}public sealed partial class {viewClassName} : global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{i2}void global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>.OnReceivedMessage(global::Bindings.DebugBindMessage message)");
        sb.AppendLine($"{i2}{{");
        sb.AppendLine($"{i3}message.BindTo(this);");
        AppendDebugBindBody(sb, i3, info, components);
        sb.AppendLine($"{i3}OnPostBind();");
        sb.AppendLine($"{i3}_viewModel.NotifyCompletedBind();");
        sb.AppendLine($"{i2}}}");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine($"#endif");

        if (hasNamespace)
            sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Appends the data-binding and event-binding statements inside BindAll().
    /// Order: field schemas first, then method schemas (grouped by shared component).
    /// </summary>
    private static void AppendBindAllBody(StringBuilder sb, string indent, ViewModelInfo info, List<ViewComponent> components)
    {
        // Field schema bindings first
        foreach (var comp in components)
        {
            foreach (var schema in comp.FieldSchemas)
                AppendFieldBinding(sb, indent, comp, schema);
        }

        // Method schema bindings (event bindings), grouped by component
        // Track which components have already had RemoveAllListeners called
        var removedListeners = new HashSet<ViewComponent>();
        foreach (var comp in components)
        {
            foreach (var schema in comp.MethodSchemas)
            {
                if (!removedListeners.Contains(comp))
                {
                    sb.AppendLine($"{indent}{comp.FieldName}.{comp.MemberName}.RemoveAllListeners();");
                    removedListeners.Add(comp);
                }
                sb.AppendLine($"{indent}{comp.FieldName}.{comp.MemberName}.AddListener(_viewModel.{schema.MethodName});");
            }
        }
    }

    /// <summary>
    /// Appends only the data-binding statements (no event bindings) for the debug subscriber.
    /// </summary>
    private static void AppendDebugBindBody(StringBuilder sb, string indent, ViewModelInfo info, List<ViewComponent> components)
    {
        foreach (var comp in components)
        {
            foreach (var schema in comp.FieldSchemas)
                AppendFieldBinding(sb, indent, comp, schema);
        }
    }

    private static void AppendFieldBinding(StringBuilder sb, string indent, ViewComponent comp, FieldSchemaInfo schema)
    {
        if (comp.IsTMPro)
        {
            // TextMeshPro: use TextMeshProExtensions.SetValue
            if (!string.IsNullOrEmpty(schema.Format))
                sb.AppendLine($"{indent}global::Bindings.TextMeshProExtensions.SetValue({comp.FieldName}, _viewModel.{schema.PropertyName}, \"{schema.Format}\");");
            else
                sb.AppendLine($"{indent}global::Bindings.TextMeshProExtensions.SetValue({comp.FieldName}, _viewModel.{schema.PropertyName});");
        }
        else
        {
            // Direct property assignment
            sb.AppendLine($"{indent}{comp.FieldName}.{comp.MemberName} = _viewModel.{schema.PropertyName};");
        }
    }

    // ──────────────────────────────────────────────────────────
    //  View component computation
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Computes the ordered list of unique UI components to appear in the View.
    /// Field-schema components come first (in declaration order), then method-schema components.
    /// </summary>
    private static List<ViewComponent> ComputeViewComponents(ViewModelInfo info)
    {
        // typeKey -> { id -> ViewComponent }
        // id == 0 means "unique per schema occurrence"; id > 0 means shared by all schemas with same id
        var typeComponentMap = new Dictionary<string, Dictionary<object, ViewComponent>>();
        // Ordered list of all unique components (field-schema components first)
        var allComponents = new List<ViewComponent>();

        ViewComponent GetOrCreateComponent(string typeKey, int id, string fullTypeName, string baseName, bool isTMPro, string memberName)
        {
            if (!typeComponentMap.TryGetValue(typeKey, out var idMap))
            {
                idMap = new Dictionary<object, ViewComponent>();
                typeComponentMap[typeKey] = idMap;
            }

            // For id=0: each call creates a fresh component (unique key = new object())
            // For id>0: reuse the same component for all schemas with the same id
            object idKey = id == 0 ? (object)new object() : id;

            if (!idMap.TryGetValue(idKey, out var comp))
            {
                comp = new ViewComponent(fullTypeName, baseName, isTMPro, memberName);
                idMap[idKey] = comp;
                allComponents.Add(comp);
            }
            return comp;
        }

        // Process field schemas
        foreach (var schema in info.FieldSchemas)
        {
            var (ns, typeName, member) = ParseBindingPath(schema.BindingPath);
            string typeKey = $"{ns}.{typeName}";
            string fullTypeName = $"global::{ns}.{typeName}";
            string baseName = GetBaseFieldName(typeName);
            bool isTMPro = ns.StartsWith("TMPro");

            var comp = GetOrCreateComponent(typeKey, schema.Id, fullTypeName, baseName, isTMPro, member);
            comp.FieldSchemas.Add(schema);
        }

        // Process method schemas
        foreach (var schema in info.MethodSchemas)
        {
            var (ns, typeName, member) = ParseBindingPath(schema.BindingPath);
            string typeKey = $"{ns}.{typeName}";
            string fullTypeName = $"global::{ns}.{typeName}";
            string baseName = GetBaseFieldName(typeName);
            bool isTMPro = ns.StartsWith("TMPro");

            var comp = GetOrCreateComponent(typeKey, schema.Id, fullTypeName, baseName, isTMPro, member);
            comp.MethodSchemas.Add(schema);
        }

        // Assign field names
        AssignViewFieldNames(allComponents);

        return allComponents;
    }

    /// <summary>
    /// Assigns the <see cref="ViewComponent.FieldName"/> for each component.
    /// Rules:
    /// <list type="bullet">
    ///   <item>Components grouped by base name.</item>
    ///   <item>id=0 components: sequential numbering if multiple; no suffix if exactly one and no id>0 siblings.</item>
    ///   <item>id>0 components: always use the id value as suffix.</item>
    /// </list>
    /// </summary>
    private static void AssignViewFieldNames(List<ViewComponent> components)
    {
        // Group by (baseName) → preserve insertion order
        var groups = new Dictionary<string, List<ViewComponent>>();
        foreach (var comp in components)
        {
            if (!groups.TryGetValue(comp.BaseName, out var list))
            {
                list = new List<ViewComponent>();
                groups[comp.BaseName] = list;
            }
            list.Add(comp);
        }

        foreach (var kvp in groups)
        {
            var baseName = kvp.Key;
            var group = kvp.Value;

            // Separate id=0 (auto-numbered) from id>0 (explicit id as suffix)
            var id0 = group.Where(c => !HasExplicitId(c)).ToList();
            var idN = group.Where(c => HasExplicitId(c)).ToList();

            bool useNumberSuffix = id0.Count > 1 || idN.Count > 0;

            int seq = 1;
            foreach (var comp in id0)
                comp.FieldName = useNumberSuffix ? $"_{baseName}{seq++}" : $"_{baseName}";

            foreach (var comp in idN)
                comp.FieldName = $"_{baseName}{GetExplicitId(comp)}";
        }
    }

    // We track whether a component was created with an explicit (non-zero) id by checking
    // all its attached schemas. If any schema has id>0, this component has an explicit id.
    private static bool HasExplicitId(ViewComponent comp)
    {
        foreach (var s in comp.FieldSchemas)
            if (s.Id != 0) return true;
        foreach (var s in comp.MethodSchemas)
            if (s.Id != 0) return true;
        return false;
    }

    private static int GetExplicitId(ViewComponent comp)
    {
        foreach (var s in comp.FieldSchemas)
            if (s.Id != 0) return s.Id;
        foreach (var s in comp.MethodSchemas)
            if (s.Id != 0) return s.Id;
        return 0;
    }

    // ──────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a binding path such as "TMPro.TMP_Text.text" into (namespace, typeName, memberName).
    /// </summary>
    private static (string ns, string typeName, string member) ParseBindingPath(string path)
    {
        var parts = path.Split('.');
        if (parts.Length < 2)
            return ("", path, "");

        string member = parts[parts.Length - 1];
        string typeName = parts[parts.Length - 2];
        string ns = string.Join(".", parts, 0, parts.Length - 2);
        return (ns, typeName, member);
    }

    /// <summary>
    /// Derives the View field base name from a component class name.
    /// Examples: "TMP_Text" → "text", "Button" → "button", "Toggle" → "toggle".
    /// </summary>
    private static string GetBaseFieldName(string typeName)
    {
        // Strip TMP_-style prefix (everything up to and including the last underscore)
        int underscoreIdx = typeName.LastIndexOf('_');
        string basePart = underscoreIdx >= 0 ? typeName.Substring(underscoreIdx + 1) : typeName;
        return basePart.Length == 0 ? typeName.ToLowerInvariant()
            : char.ToLowerInvariant(basePart[0]) + basePart.Substring(1);
    }

    private static string StripLeadingUnderscore(string name)
        => name.Length > 0 && name[0] == '_' ? name.Substring(1) : name;

    private static string FieldToPropertyName(string fieldName)
    {
        var stripped = StripLeadingUnderscore(fieldName);
        if (stripped.Length == 0) return fieldName;
        return char.ToUpperInvariant(stripped[0]) + stripped.Substring(1);
    }
}
