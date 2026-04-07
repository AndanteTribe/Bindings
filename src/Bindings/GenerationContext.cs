namespace Bindings;

public sealed class GenerationContext
{
    /// <summary>
    /// Class name of the target type.
    /// </summary>
    public readonly string ClassName;

    /// <summary>
    /// Namespace of the target type. Empty string when the type is in the global namespace.
    /// </summary>
    public readonly string Namespace;

    /// <summary>
    /// Whether the target is a struct. When false, the target is a class.
    /// </summary>
    public readonly bool IsStruct;

    /// <summary>
    /// Whether the target is a struct with the readonly modifier.
    /// When true, no set accessor is generated for [Schema] field properties.
    /// </summary>
    public readonly bool IsReadOnly;

    /// <summary>
    /// Argument of ViewModelAttribute (requireBindImplementation).
    /// </summary>
    public readonly bool RequireBindImplementation;

    /// <summary>
    /// Whether <see cref="System.SerializableAttribute"/> is already applied to the type.
    /// </summary>
    public readonly bool AlreadySerializable;

    /// <summary>
    /// Fully-qualified type name and field name (or property name) of each field/property
    /// annotated with ModelAttribute, in declaration order.
    /// </summary>
    public readonly (string TypeFullName, string FieldName)[] Models;

    /// <summary>
    /// Attribute arguments for each field/property annotated with SchemaAttribute, in declaration order.
    /// Multiple entries are created when multiple [Schema] attributes are applied to the same member.
    /// </summary>
    public readonly (string FieldName, string FieldTypeName, string BindingPath, int Id, string Format)[] SchemaFields;

    /// <summary>
    /// Attribute arguments for each method annotated with SchemaAttribute, in declaration order.
    /// Multiple entries are created when multiple [Schema] attributes are applied to the same method.
    /// Format is omitted because it is not needed for methods.
    /// </summary>
    public readonly (string MethodName, string BindingPath, int Id)[] SchemaMethods;

    public GenerationContext(
        string className,
        string @namespace,
        bool isStruct,
        bool isReadOnly,
        bool requireBindImplementation,
        bool alreadySerializable,
        (string TypeFullName, string FieldName)[] models,
        (string FieldName, string FieldTypeName, string BindingPath, int Id, string Format)[] schemaFields,
        (string MethodName, string BindingPath, int Id)[] schemaMethods)
    {
        ClassName = className;
        Namespace = @namespace;
        IsStruct = isStruct;
        IsReadOnly = isReadOnly;
        RequireBindImplementation = requireBindImplementation;
        AlreadySerializable = alreadySerializable;
        Models = models;
        SchemaFields = schemaFields;
        SchemaMethods = schemaMethods;
    }
}