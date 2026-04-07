namespace Bindings;

public sealed class GenerationContext
{
    /// <summary>
    /// 対象クラスのクラス名.
    /// </summary>
    public readonly string ClassName;

    /// <summary>
    /// 対象クラスの名前空間. グローバル名前空間の場合は空文字列.
    /// </summary>
    public readonly string Namespace;

    /// <summary>
    /// 対象が構造体かどうか. false の場合はクラス.
    /// </summary>
    public readonly bool IsStruct;

    /// <summary>
    /// 対象が readonly 修飾子を持つ構造体かどうか.
    /// true の場合、[Schema] フィールドの公開プロパティには set アクセサを生成しない.
    /// </summary>
    public readonly bool IsReadOnly;

    /// <summary>
    /// ViewModelAttributeの引数.
    /// </summary>
    public readonly bool RequireBindImplementation;

    /// <summary>
    /// すでに<see cref="System.SerializableAttribute"/>を付与されているかどうか.
    /// </summary>
    public readonly bool AlreadySerializable;

    /// <summary>
    /// ModelAttributeが付与されたフィールド変数 or プロパティの型のフルネームと変数名（またはプロパティ名）.
    /// 宣言順.
    /// </summary>
    public readonly (string TypeFullName, string FieldName)[] Models;

    /// <summary>
    /// SchemaAttributeが付与されたフィールド変数 or プロパティごとの引数情報.
    /// 同一メンバーに複数の[Schema]が付与された場合は複数エントリになる. 宣言順.
    /// </summary>
    public readonly (string FieldName, string FieldTypeName, string BindingPath, int Id, string Format)[] SchemaFields;

    /// <summary>
    /// SchemaAttributeが付与されたメソッドごとの引数情報.
    /// 同一メソッドに複数の[Schema]が付与された場合は複数エントリになる. 宣言順.
    /// Format は不要なので除外.
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