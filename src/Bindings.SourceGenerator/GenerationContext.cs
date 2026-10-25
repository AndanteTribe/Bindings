using Microsoft.CodeAnalysis;

namespace Bindings;

public sealed class GenerationContext(
    string className,
    string @namespace,
    bool canGenerate,
    string accessibility,
    (string Accessibility, string TypeKeyword, string TypeName)[] containingTypes,
    bool isStruct,
    bool isReadOnly,
    bool requireBindImplementation,
    bool alreadySerializable,
    (string TypeFullName, string FieldName, string TypeOfName)[] requiredFields,
    (string FieldName, string FieldTypeName, string BindingPath, int Id, string Format, string Tooltip)[] schemaFields,
    (string MethodName, string BindingPath, int Id, string Tooltip)[] schemaMethods,
    (DiagnosticDescriptor Descriptor, Location Location, string[] Args)[] diagnostics,
    bool generateVContainerInjector = false)
{
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
    public readonly (string Accessibility, string TypeKeyword, string TypeName)[] ContainingTypes = containingTypes;

    /// <summary>
    /// Whether the target is a struct. When false, the target is a class.
    /// </summary>
    public readonly bool IsStruct = isStruct;

    /// <summary>
    /// Whether the target is a struct with the readonly modifier.
    /// When true, no set accessor is generated for [Schema] field properties.
    /// </summary>
    public readonly bool IsReadOnly = isReadOnly;

    /// <summary>
    /// Argument of ViewModelAttribute (requireBindImplementation).
    /// </summary>
    public readonly bool RequireBindImplementation = requireBindImplementation;

    /// <summary>
    /// Whether <see cref="System.SerializableAttribute"/> is already applied to the type.
    /// </summary>
    public readonly bool AlreadySerializable = alreadySerializable;

    /// <summary>Whether a constructor-only VContainer injector can be generated safely.</summary>
    public readonly bool GenerateVContainerInjector = generateVContainerInjector;

    /// <summary>
    /// Fully-qualified declaration type name, member name, and typeof-compatible type name
    /// of each required field or property, in declaration order.
    /// TypeOfName excludes nullable reference annotations while preserving nullable value types.
    /// </summary>
    public readonly (string TypeFullName, string FieldName, string TypeOfName)[] RequiredFields = requiredFields;

    /// <summary>
    /// Attribute arguments for each field annotated with SchemaAttribute, in declaration order.
    /// Multiple entries are created when multiple [Schema] attributes are applied to the same member.
    /// </summary>
    public readonly (string FieldName, string FieldTypeName, string BindingPath, int Id, string Format, string Tooltip)[] SchemaFields = schemaFields;

    /// <summary>
    /// Attribute arguments for each method annotated with SchemaAttribute, in declaration order.
    /// Multiple entries are created when multiple [Schema] attributes are applied to the same method.
    /// Format is omitted because it is not needed for methods.
    /// </summary>
    public readonly (string MethodName, string BindingPath, int Id, string Tooltip)[] SchemaMethods = schemaMethods;

    /// <summary>
    /// Diagnostics collected during metadata extraction (BND001, BND002, BND004).
    /// These are reported in RegisterSourceOutput so they appear in the IDE/build output.
    /// </summary>
    public readonly (DiagnosticDescriptor Descriptor, Location Location, string[] Args)[] Diagnostics = diagnostics;
}