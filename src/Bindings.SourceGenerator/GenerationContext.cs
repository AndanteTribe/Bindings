using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Bindings;

public sealed class GenerationContext(
    string className,
    string @namespace,
    bool canGenerate,
    string accessibility,
    IReadOnlyList<(string Accessibility, string TypeKeyword, string TypeName)> containingTypes,
    GenerationContext.ViewModelTypeKind type,
    bool requireBindImplementation,
    IReadOnlyList<(string TypeFullName, string FieldName, string TypeOfName)> requiredFields,
    IReadOnlyList<(string FieldName, string FieldTypeName, GenerationContext.BindingInfo Binding)> schemaFields,
    IReadOnlyList<(string MethodName, string ActionField, GenerationContext.BindingInfo Binding)> schemaMethods,
    IReadOnlyList<(string MemberName, string ActionType, string MethodName, bool IsStatic)> schemaActions,
    IReadOnlyList<Diagnostic> diagnostics,
    bool generateVContainerInjector = false)
{
    /// <summary>
    /// Binding arguments of a Schema attribute. Format is empty for method bindings.
    /// </summary>
    public readonly struct BindingInfo(string bindingPath, int id, string format, string tooltip)
    {
        public readonly string BindingPath = bindingPath;
        public readonly int Id = id;
        public readonly string Format = format;
        public readonly string Tooltip = tooltip;
    }

    /// <summary>
    /// Supported ViewModel declaration kinds, including struct mutability.
    /// </summary>
    public enum ViewModelTypeKind : byte
    {
        Invalid = 0,
        Class = 1,
        Struct = 2,
        ReadOnlyStruct = 3,
    }

    /// <summary>
    /// Class name of the target type.
    /// </summary>
    public readonly string ClassName = className;

    /// <summary>
    /// Namespace of the target type. Empty string when the type is in the global namespace.
    /// </summary>
    public readonly string Namespace = @namespace;

    /// <summary>
    /// Whether source generation can continue after validating the target and containing types.
    /// Diagnostics are still reported when this is <see langword="false"/>.
    /// </summary>
    public readonly bool CanGenerate = canGenerate;

    /// <summary>
    /// Declared C# accessibility keyword of the target type.
    /// </summary>
    public readonly string Accessibility = accessibility;

    /// <summary>
    /// Chain of containing types (from outermost to innermost) when the ViewModel is a nested type.
    /// Empty when the ViewModel is a top-level type.
    /// Each entry holds the declared accessibility keyword, type keyword ("class" or "struct"),
    /// and simple type name.
    /// </summary>
    public readonly IReadOnlyList<(string Accessibility, string TypeKeyword, string TypeName)> ContainingTypes = containingTypes;

    /// <summary>
    /// Declaration kind of the target, including struct mutability.
    /// </summary>
    public readonly ViewModelTypeKind Type = type;

    /// <summary>
    /// Argument of ViewModelAttribute (requireBindImplementation).
    /// </summary>
    public readonly bool RequireBindImplementation = requireBindImplementation;

    /// <summary>Whether a constructor-only VContainer injector can be generated safely.</summary>
    public readonly bool GenerateVContainerInjector = generateVContainerInjector;

    /// <summary>
    /// Fully-qualified declaration type name, member name, and typeof-compatible type name
    /// of each required field or property, in declaration order.
    /// TypeOfName excludes nullable reference annotations while preserving nullable value types.
    /// </summary>
    public readonly IReadOnlyList<(string TypeFullName, string FieldName, string TypeOfName)> RequiredFields = requiredFields;

    /// <summary>
    /// Attribute arguments for each field annotated with SchemaAttribute, in declaration order.
    /// Multiple entries are created when multiple [Schema] attributes are applied to the same member.
    /// </summary>
    public readonly IReadOnlyList<(string FieldName, string FieldTypeName, BindingInfo Binding)> SchemaFields = schemaFields;

    /// <summary>
    /// Attribute arguments for each method annotated with SchemaAttribute, in declaration order.
    /// Multiple entries are created when multiple [Schema] attributes are applied to the same method.
    /// Binding.Format is empty because it is not needed for methods.
    /// </summary>
    public readonly IReadOnlyList<(string MethodName, string ActionField, BindingInfo Binding)> SchemaMethods = schemaMethods;

    public readonly IReadOnlyList<(string MemberName, string ActionType, string MethodName, bool IsStatic)> SchemaActions = schemaActions;

    /// <summary>
    /// Diagnostics collected during metadata extraction (BND001, BND002, BND004).
    /// These are reported in RegisterSourceOutput so they appear in the IDE/build output.
    /// </summary>
    public readonly IReadOnlyList<Diagnostic> Diagnostics = diagnostics;
}