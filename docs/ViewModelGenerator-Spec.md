# ViewModelGenerator — Source Generator Specification

## 概要 (Overview)

`[ViewModel]` アノテーションが付与されたクラスを解析し、以下の2ファイルを自動生成する Roslyn `IIncrementalGenerator`。

| 生成ファイル | 内容 |
|---|---|
| `{ClassName}.ViewModel.g.cs` | ViewModel の partial クラス（バックグラウンドロジック） |
| `{ViewClassName}.g.cs` | View の sealed partial クラス（Unity UI バインド） |

---

## 1. 入力：ユーザー記述コード

### 1.1 使用属性

| 属性 | 対象 | 意味 |
|---|---|---|
| `[ViewModel]` | クラス | このクラスを ViewModel として扱う |
| `[ViewModel(requireBindImplementation: true)]` | クラス | `BindAsync` を自動生成せず、ユーザーが実装する |
| `[Model]` | フィールド | DI コンストラクタ引数として注入するモデル |
| `[Schema(bindingPath)]` | フィールド・メソッド | UI コンポーネントとのバインド対象を宣言する |
| `[Schema(bindingPath, id: N)]` | フィールド・メソッド | 同 `id` を持つスキーマは View 内で同一コンポーネントにバインド |
| `[Schema(bindingPath, format: "N0")]` | フィールド | テキスト表示時の書式指定（TMPro 系のみ有効） |

### 1.2 入力サンプル（CountViewModel.cs より抜粋）

```csharp
[ViewModel]
public partial class CountViewModel1
{
    [Model]
    private readonly CountModel _model;

    [SerializeField]
    [Schema(PathResolver.TMPro.TMP_Text.text)]
    private int _count;

    [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
    public void Increment() { ... }

    [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
    public void Decrement() { ... }

    partial void OnPostBind() { ... }
}
```

---

## 2. 生成ルール：ViewModel (`{ClassName}.ViewModel.g.cs`)

### 2.1 クラス宣言

```csharp
#nullable enable

namespace {Namespace}
{
    [global::System.Serializable]          // ※ユーザーが既に付与している場合は省略
    public partial class {ClassName} : IViewModel
    {
```

**未解決事項 Q1:** `IViewModel` の参照は `global::Bindings.IViewModel` にすべきか、`IViewModel` のままでよいか？

### 2.2 自動生成フィールド

```csharp
private readonly global::Bindings.IMvvmPublisher _publisher;
```

### 2.3 公開プロパティ（`[Schema]` フィールドごと）

`_fieldName` → プロパティ名：先頭アンダースコアを除去し、1文字目を大文字化。例: `_count` → `Count`

```csharp
public {FieldType} {PropertyName}
{
    get => {_fieldName};
    set
    {
        {_fieldName} = value;
        PublishRebindMessage();
    }
}
```

### 2.4 コンストラクタ

引数の順序：`[Model]` フィールドの宣言順 → 最後に `IMvvmPublisher publisher`

```csharp
public {ClassName}({ModelType} {modelParamName}, ..., global::Bindings.IMvvmPublisher publisher)
{
    {_modelField} = {modelParamName};
    ...
    _publisher = publisher;
}
```

**未解決事項 Q2:** `[Model]` フィールドのコンストラクタパラメータ名の命名規則は？  
現在の素案：フィールド名の先頭アンダースコアを除去（`_model` → `model`、`_model2` → `model2`）  
→ 確認が必要

### 2.5 ヘルパーメソッド

```csharp
public void NotifyCompletedBind() => OnPostBind();

partial void OnPostBind();

[global::System.Runtime.CompilerServices.MethodImpl(
    global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
private void PublishRebindMessage()
{
    _publisher.PublishRebindMessage<{ClassName}>();
}
```

---

## 3. 生成ルール：View (`{ViewClassName}.g.cs`)

### 3.1 View クラス名の命名規則

**素案:** `{ClassName}` 中の `ViewModel` を `View` に置換。  
例: `CountViewModel1` → `CountView1`

**未解決事項 Q3:** ViewModel 名が `ViewModel` を含まない場合（例: `CountVM`）はどう命名するか？  
→ エラー? サフィックス `View` を単純追加? 確認が必要

### 3.2 クラス宣言

```csharp
#nullable enable

namespace {Namespace}
{
    [global::System.Serializable]
    public sealed partial class {ViewClassName} : IView<global::{Namespace}.{ClassName}>
    {
```

### 3.3 _viewModel フィールド

```csharp
[global::System.NonSerialized]
private global::{Namespace}.{ClassName} _viewModel = null!;
```

### 3.4 UI コンポーネントフィールド

`[Schema(bindingPath)]` のバインディングパスからコンポーネント型と対象プロパティ/イベントを解決する。

#### 3.4.1 バインディングパスの解析

`PathResolver` の定数値の形式:  
`"{Namespace}.{ClassName}.{MemberName}"` (例: `"TMPro.TMP_Text.text"`, `"UnityEngine.UI.Button.onClick"`)

- `{Namespace}` = 先頭から最後の2つを除いたセグメント（例: `"TMPro"`, `"UnityEngine.UI"`）
- `{ClassName}` = 後ろから2番目のセグメント（例: `"TMP_Text"`, `"Button"`, `"Toggle"`）
- `{MemberName}` = 最後のセグメント（例: `"text"`, `"onClick"`, `"interactable"`）

完全修飾型名: `global::{Namespace}.{ClassName}`

#### 3.4.2 View フィールド名の命名規則

**素案:**

1. コンポーネント型ごとに出現順でグループ化する
2. フィールド名ベース = クラス名の最後のアンダースコア以降を小文字化
   - 例: `TMP_Text` → `_text`、`Button` → `_button`、`Toggle` → `_toggle`
3. 同一型が複数ある場合の連番付与ルール:
   - `id = 0`（デフォルト）のスキーマは各々個別のコンポーネント → 出現順に `_button1`, `_button2`, ... と連番
   - `id = N`（N > 0）のスキーマは同じ `id` のものが同一コンポーネントを共有 → `_button{N}` という名前

**未解決事項 Q4:** 同型コンポーネントが1つだけの場合（かつ `id=0`）でも連番を付けるべきか？  
→ 現行サンプルでは `_text` のように連番なし。一方 `_button1`, `_button2` は複数あるので連番あり。  
→ 「1つなら番号なし、複数なら全て番号あり」か？

**未解決事項 Q5:** `id = N` かつ同型が `id=0` のものと混在する場合の連番はどうなるか？

#### 3.4.3 View フィールド宣言

```csharp
[global::UnityEngine.SerializeField]
private {ComponentFullType} {_fieldName} = null!;
```

### 3.5 Initialize メソッド

```csharp
void global::Bindings.IView<global::{Namespace}.{ClassName}>.Initialize(
    global::{Namespace}.{ClassName} viewModel)
{
    _viewModel = viewModel;
}
```

### 3.6 BindAsync メソッド

`requireBindImplementation: false`（デフォルト）の場合のみ生成する。

```csharp
global::System.Threading.Tasks.ValueTask global::Bindings.IView.BindAsync(
    global::System.Threading.CancellationToken _)
{
    BindAll();
    return default;
}
```

### 3.7 BindAll メソッド

```csharp
private void BindAll()
{
    // 1. [Schema] フィールドのバインド（宣言順）
    // 2. [Schema] メソッドのイベントバインド（宣言順、コンポーネントごとに RemoveAllListeners → AddListener）
    OnPostBind();
    _viewModel.NotifyCompletedBind();
}
```

#### フィールドバインドの生成ルール

| コンポーネントの種類 | 生成コード |
|---|---|
| TMPro 系 (`TMPro.TMP_Text`, `TMPro.TextMeshProUGUI` など) + `format` なし | `global::Bindings.TextMeshProExtensions.SetValue({_field}, _viewModel.{Property});` |
| TMPro 系 + `format` あり | `global::Bindings.TextMeshProExtensions.SetValue({_field}, _viewModel.{Property}, "{format}");` |
| それ以外（`Toggle.interactable` など） | `{_field}.{memberName} = _viewModel.{Property};` |

**未解決事項 Q6:** TMPro 系かどうかの判定基準は namespace が `TMPro` で始まるかどうか？他に判定方法はあるか？

#### イベントバインドの生成ルール

```csharp
{_field}.{event}.RemoveAllListeners();
{_field}.{event}.AddListener(_viewModel.{MethodName});
// 同じコンポーネントに複数メソッドがバインドされる場合は RemoveAllListeners は1回だけ
{_field}.{event}.AddListener(_viewModel.{MethodName2});
```

### 3.8 partial void OnPostBind

```csharp
partial void OnPostBind();
```

### 3.9 デバッグ用サブスクライバ（条件付きコンパイル）

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD || !DISABLE_DEBUGTOOLKIT
    public sealed partial class {ViewClassName} : IMvvmSubscriber<global::Bindings.DebugBindMessage>
    {
        void IMvvmSubscriber<global::Bindings.DebugBindMessage>.OnReceivedMessage(
            global::Bindings.DebugBindMessage message)
        {
            message.BindTo(this);
            // [Schema] フィールドのデータバインドのみ（イベントバインドは行わない）
            OnPostBind();
            _viewModel.NotifyCompletedBind();
        }
    }
#endif
```

---

## 4. 全シナリオ対応表

| シナリオ | 入力の特徴 | ViewModel 生成の変化 | View 生成の変化 |
|---|---|---|---|
| 1 (simple) | 通常 | `[Serializable]` 自動付与 | `BindAsync` 生成あり |
| 2 (requireBindImplementation) | `[ViewModel(requireBindImplementation: true)]` | 変化なし | `BindAsync` 生成なし |
| 3 (alreadySerializable) | `[System.Serializable]` を既にユーザーが付与 | `[Serializable]` を重複付与しない | 変化なし |
| 4 (non model) | `[Model]` なし | コンストラクタに Model 引数なし | 変化なし |
| 5 (multi models) | 複数 `[Model]` | コンストラクタに複数 Model 引数 | 変化なし |
| 6 (same id pair) | 同一 `id` の `[Schema]` メソッドが複数 | 変化なし | 同一コンポーネントフィールドを共有 |
| 7 (format + non-text) | `format` 指定 + TMPro 以外のフィールドスキーマ | 変化なし | `SetValue` に format 引数追加、直接代入で非テキストをバインド |

---

## 5. 未解決事項・要確認事項まとめ

| # | 質問 |
|---|---|
| Q1 | 生成された ViewModel の `IViewModel` 参照は `global::Bindings.IViewModel` にすべきか？ |
| Q2 | `[Model]` フィールドのコンストラクタパラメータ名は「先頭アンダースコア除去」で良いか？ |
| Q3 | ViewModel クラス名が `ViewModel` という文字列を含まない場合、View クラス名はどうするか？ |
| Q4 | 同型コンポーネントが1つだけの場合（`id=0`）、フィールド名に連番を付けるか付けないか？ |
| Q5 | `id=N` と `id=0` が同型コンポーネントで混在する場合の連番ルールは？ |
| Q6 | TMPro 系コンポーネントの判定方法（namespace が `TMPro` で始まるかどうか）で良いか？ |
| Q7 | 生成ファイル名の命名規則は `{ClassName}.ViewModel.g.cs` / `{ViewClassName}.g.cs` で良いか？ |
| Q8 | 生成 View クラスに `[Serializable]` を常に付与する理由は何か（Unity の Inspector 要件のため？）？ |
