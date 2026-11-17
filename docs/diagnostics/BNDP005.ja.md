# BNDP005: ViewModel で UI コンポーネントを使用しないでください

[English](BNDP005.md) | 日本語

| 項目 | 内容 |
| --- | --- |
| カテゴリ | Bindings.Practices |
| 標準重大度 | Error |
| 既定で有効 | はい |
| Code Fix | なし |

## メッセージ

> ViewModel '{0}' が UI コンポーネント型 '{1}' を直接使用しています。UI コンポーネントへのアクセスは View に配置してください。

## 原因

ViewModel が `UnityEngine.EventSystems.UIBehaviour` またはその派生型を直接使用しています。

## 違反例

```csharp
[ViewModel]
public partial class CounterViewModel
{
    [Required] private readonly UnityEngine.UI.Text _label;
    public void Reset() => _label.text = "0";
}
```

## 修正例

Schema メンバーでデータを公開し、独自のコンポーネント操作は View に配置します。

```csharp
[ViewModel]
public partial class CounterViewModel
{
    [Schema(PathResolver.UnityEngine.UI.Text.text)]
    private int _count;
    public void Reset() => Count = 0;
}
```

## 対象範囲と例外

- `[ViewModel]` 付きの型、または `IViewModel` 実装型を検査します。継承経由の実装と、字句的に内包される補助型も対象です。
- 参照された名前・呼び出し結果・インデクサーの戻り値の解決済みの型を検査します。メンバー宣言やコンポーネントへのアクセスが対象で、推論型・配列・ジェネリック型引数・型パラメーター制約も含みます。
- 生成コードも手書きコードと同じ条件で検査します。
- ネストした `IView` 実装型内の UI 操作は許可します。
- リフレクションやデータフローから実行時の型を推定しません。`object` や無関係なインターフェイスとしてのみ参照されているオブジェクトは診断しません。

## ソース生成

ソース生成は継続します。

[診断一覧](../../README_JA.md#ベストプラクティス診断) · [重大度の設定](../../README_JA.md#重大度の設定)
