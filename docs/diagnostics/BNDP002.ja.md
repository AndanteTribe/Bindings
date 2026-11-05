# BNDP002: View 内で ViewModel を構築しないでください

[English](BNDP002.md) | 日本語

| 項目 | 内容 |
| --- | --- |
| カテゴリ | Bindings.Practices |
| 標準重大度 | Error |
| 既定で有効 | はい |
| Code Fix | なし |

## メッセージ

> View '{0}' が ViewModel '{1}' を構築しています。ViewModel は View の外で構築し、View に渡してください。

`{0}` は処理を所有するView型、`{1}` は構築するViewModel型です。

## 原因

Viewが所有するコードでViewModelを構築しています。構築と依存関係の所有はViewの外に置き、初期化時にインスタンスを受け渡します。

## 違反例

生成された `CounterView` と、Publisherだけを必要とするViewModelの場合です。

```csharp
public sealed partial class CounterView
{
    private CounterViewModel Create(IMvvmPublisher publisher)
        => new CounterViewModel(publisher);
}
```

## 修正例

起動用コンポーネントやVContainer登録など、外部の構築処理へ移します。

```csharp
// In the external bootstrap, not in the View:
var viewModel = new CounterViewModel(binder);
binder.Initialize(viewModel);
binder.Run();
```

所有者と注入方法の選択には設計判断が必要なため、自動Code Fixはありません。

## 対象範囲と例外

- Viewは実際の `Bindings.IView` を実装する型です。`IView<T>` と継承経由の実装も含みます。字句的に内包される補助型もViewが所有するコードとして扱い、最も近い外側のViewを診断に表示します。
- ViewModelは実際の `Bindings.IViewModel` を実装する型で、継承経由も含みます。`[ViewModel]` 属性だけでは判定しません。通常はソース生成が解析前にインターフェイスを追加します。
- 明示的な型指定とターゲット型による `new` を含みます。`new T()` は、基底型・インターフェイス制約や型パラメーター間の推移的な制約からIViewModelであると判定できる場合に対象です。
- 初期化子・コンストラクターとその初期化子・staticメソッド・アクセサー・ラムダ・ローカル関数も対象です。生成コードにも同じ規則を適用します。
- 通常のオブジェクト生成・配列確保・ViewModel要素を構築しないコレクション確保・`default`・View外での構築は対象外です。
- ファクトリー呼び出しとリフレクションは検出対象外です。
- 構築式全体に対して1件ずつ報告します。

## ソース生成と設定

ソース生成は継続します。作者が推奨する設計・書き方の診断であり、既定ではErrorです。修正・抑制・重大度の変更を行うまでコンパイルは失敗します。

[診断一覧](../../README_JA.md#ベストプラクティス診断) · [重大度の設定](../../README_JA.md#重大度の設定)
