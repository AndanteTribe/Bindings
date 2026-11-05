# BNDP001: 生成された自身への再バインド用ヘルパーを使用してください

[English](BNDP001.md) | 日本語

| 項目 | 内容 |
| --- | --- |
| カテゴリ | Bindings.Practices |
| 標準重大度 | Error |
| 既定で有効 | はい |
| Code Fix | あり |

## メッセージ

> '{0}' の再バインドメッセージを直接発行する代わりに、PublishRebindMessage() を使用してください

`{0}` は現在のViewModel型です。

## 原因

等価な引数なしの `PublishRebindMessage()` ヘルパーがあるにもかかわらず、`[ViewModel]` が自身の `_publisher` フィールドから自身の正確な型への再バインドを直接通知しています。型引数の重複記述を避けるため、ヘルパーを使用します。

## 違反例

```csharp
[ViewModel]
public partial class CounterViewModel
{
    public void Refresh() => _publisher.PublishRebindMessage<CounterViewModel>();
}
```

## 修正例

```csharp
public void Refresh() => PublishRebindMessage();
```

Bindingsがヘルパーを生成します。Schemaの生成setterはすでにヘルパーを呼ぶため、その後に重複して通知する必要はありません。

## 対象範囲と例外

- 呼び出し先が、引数なし・void戻り値の `Bindings.IMvvmPublisher.PublishRebindMessage<T>()` であり、`T` が現在のViewModel型と一致する場合が対象です。
- レシーバーは、同じ型で宣言された、そのインスタンス自身の非staticな `IMvvmPublisher` 型の `_publisher` フィールドである必要があります。
- ローカル変数・プロパティ・別インスタンス・条件付きアクセス・レシーバーの明示的変換・別Publisher・別の型引数を経由する呼び出しは対象外です。プリプロセッサーディレクティブを含む呼び出しも除外します。
- 引数なし・非ジェネリック・void戻り値のインスタンスヘルパーが一意に存在し、その本体全体が等価な自己再バインドの1回の呼び出しである必要があります。式形式、または式文1つのブロック形式に対応します。存在しないヘルパーや、動作が異なるヘルパーは推奨しません。
- ヘルパー自身はシンボルの同一性で除外し、再帰呼び出しに置換されることを防ぎます。
- 生成コードも手書きコードと同じ条件で解析・報告します。ファイル名や生成コードのマーカーによる免除はありません。実際のフレームワークシンボルで判定します。

## Code Fix

IDEの明示的な操作で `PublishRebindMessage()` に置換します。名前の隠蔽がある場合は `this.PublishRebindMessage()` にします。置換前のレシーバー・型引数・引数リストのコメントや空白情報を保持し、ヘルパーと置換後の呼び出しが引き続き正しく解決されることを再確認します。

ドキュメント・プロジェクト・ソリューション単位のFix Allに対応します。操作が表示されるかどうかはIDEのRoslyn統合に依存します。

## ソース生成と設定

ソース生成は継続します。作者が推奨する設計・書き方の診断であり、既定ではErrorです。修正・抑制・重大度の変更を行うまでコンパイルは失敗します。

```editorconfig
[*.cs]
dotnet_diagnostic.BNDP001.severity = suggestion
```

[診断一覧](../../README_JA.md#ベストプラクティス診断) · [重大度の設定](../../README_JA.md#重大度の設定)
