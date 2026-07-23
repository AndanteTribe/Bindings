---
name: bindings
description: Unity/C# の uGUI で Bindings を使い、ViewModel と View の自動バインディング、Binder の設定、PathResolver と [ViewModel]・[Required]・[Schema] の利用、再バインド、View へのメッセージ配信、VContainer 統合を実装または修正するときに使用する。Bindings、Binder、生成 View、PublishRebindMessage、IMvvmSubscriber が指定された場合にも使用する。
license: MIT
metadata:
  author: AndanteTribe
  version: "0.3.4"
required_packages:
  com.unity.ugui: ">=2.0.0"
---

# Bindings

Repository: https://github.com/AndanteTribe/Bindings  
Namespace: `Bindings`  
Package name: `jp.andantetribe.bindings`

## 採用基準

Unity 6000.0 以降の uGUI で、C# の ViewModel から UI プロパティや UnityEvent を宣言的にバインドするときに使用する。Roslyn Source Generator により ViewModel の補助メンバーとシリアライズ可能な View が生成され、値の変更を Canvas 描画直前にまとめて反映する。

UI Toolkit のデータバインディング、一般的なリアクティブストリーム、Unity を使わない .NET アプリには適用しない。

## 基本原則

- `[ViewModel]` を付ける型と、そのネスト元の型を `partial` にする。
- ViewModel の型名に `ViewModel` を含める。生成 View 名は型名中の `ViewModel` を `View` に置換して決まる。
- UI の値は `[Schema(PathResolver....)]` を付けたフィールドで宣言し、イベントは `[Schema(PathResolver....)]` を付けたメソッドで宣言する。文字列パスより `PathResolver` を優先する。
- 生成された `*.g.cs`、`PathResolver.cs`、`Bindings.SourceGenerator.dll` を利用側から編集しない。変更はユーザー記述の partial 型へ加える。
- 値の変更には原則として生成プロパティを使用する。ViewModel 内で複数のバッキングフィールドをまとめて直接変更する場合は、最後に `PublishRebindMessage()` を1回呼ぶ。
- ViewModel の生成コンストラクタには `[Required]` のメンバーを宣言順に渡し、最後に `IMvvmPublisher` を渡す。通常は `Binder` を publisher として使用する。
- View の生成後、`Binder` の Inspector で View を追加し、生成された UI コンポーネントフィールドを割り当てる。

## 作業手順

1. インストール済みの Bindings と Unity のバージョン、既存の ViewModel、`Binder` の初期化方法、DI コンテナの有無を確認する。
2. ViewModel やバインディング宣言を扱う場合は [references/viewmodel-and-bindings.md](references/viewmodel-and-bindings.md) を読む。
3. `Binder`、独自 View、メッセージング、TextMeshPro、VContainer を扱う場合は [references/runtime-and-integration.md](references/runtime-and-integration.md) を読む。
4. `BND001` から `BND005`、または生成コードの欠落を扱う場合は [references/diagnostics.md](references/diagnostics.md) を読む。
5. ユーザー記述の partial 型だけを編集し、Unity のコンパイル完了後に生成された View 型と Inspector の割り当てを確認する。
6. Source Generator の診断を解消し、初回バインド、値変更後の再バインド、UnityEvent、必要ならメッセージ購読を Play Mode で確認する。

## 実装上の注意

- 生成プロパティの setter は常に再バインドを要求する。値が同じ場合の比較は生成されないため、必要なら呼び出し側で変更判定する。
- バッキングフィールドを直接変更しても自動では再バインドされない。生成プロパティを使うか、変更後に `PublishRebindMessage()` を呼ぶ。
- 同じ UI コンポーネントへ複数の値やイベントを結び付けるときは、同じコンポーネント型かつ同じ `id >= 0` を使用する。
- `id` を省略した各 `[Schema]` は、同じコンポーネント型でも別々の View フィールドとして扱われる。
- `format` は `TMPro.TMP_Text.text` のフィールドバインドだけに適用される。
- `[ViewModel(requireBindImplementation: true)]` は、生成 View の `IView.BindAsync` を独自実装する必要がある場合だけ使用する。
- ViewModel 型を Unity の `[SerializeField]` または `[SerializeReference]` で保持しない。`[Serializable]` を付けても生成されたランタイム状態の初期化は保証されないため、ViewModel は実行時に構築または代入する。
