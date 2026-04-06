namespace Bindings;

public sealed class GenerationContext
{
    /// <summary>
    /// ViewModelAttributeの引数
    /// </summary>
    public readonly bool RequireBindImplementation;

    /// <summary>
    /// すでに<see cref="System.SerializableAttribute"/>を付与されているかどうか.
    /// </summary>
    public readonly bool AlreadySerializable;

    /// <summary>
    /// ModelAttributeが付与されたフィールド変数 or プロパティの型のフルネームと変数名（またはプロパティ名）
    /// </summary>
    public readonly (string typeFullName, string name)[] Models;

    /// <summary>
    /// SchemaAttributeが付与されたフィールド変数 or プロパティの引数情報.
    /// </summary>
    public readonly (string bindingPath, int id, string format)[] Schemas;

    /// <summary>
    /// SchemaAttributeが付与された関数の引数情報. Formatは不要なので除外で.
    /// </summary>
    public readonly (string bindingPath, int id)[] SchemaMethods;

    public GenerationContext(
        bool requireBindImplementation,
        bool alreadySerializable,
        (string typeFullName, string name)[] models,
        (string bindingPath, int id, string format)[] schemas,
        (string bindingPath, int id)[] schemaMethods)
    {
        RequireBindImplementation = requireBindImplementation;
        AlreadySerializable = alreadySerializable;
        Models = models;
        Schemas = schemas;
        SchemaMethods = schemaMethods;
    }
}