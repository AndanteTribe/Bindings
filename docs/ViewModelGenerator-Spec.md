# ViewModelGenerator — Source Generator Specification

## 概要 (Overview)

`[ViewModel]` アノテーションが付与されたクラスを解析し、以下の2ファイルを自動生成する Roslyn `IIncrementalGenerator`。

| 生成ファイル | 内容 |
|---|---|
| `{ClassName}.g.cs` | ViewModel の partial クラス（バックグラウンドロジック） |
| `{ViewClassName}.g.cs` | View の sealed partial クラス（Unity UI バインド） |

> **原則:** 生成コード内のすべての型参照は `global::` プレフィックスを付ける。これはユーザー定義の型名と衝突しないようにするためである。

---

## 1. 入力：ユーザー記述コード

### 1.1 使用属性

| 属性 | 対象 | 意味 |
|---|---|---|
| `[ViewModel]` | クラス | このクラスを ViewModel として扱う |
| `[ViewModel(requireBindImplementation: true)]` | クラス | `BindAsync` を自動生成せず、ユーザーが実装する |
| `[Model]` | フィールド | DI コンストラクタ引数として注入するモデル |
| `[Schema(bindingPath)]` | フィールド・メソッド | UI コンポーネントとのバインド対象を宣言する |
| `[Schema(bindingPath, id: N)]` | フィールド・メソッド | 同じ `id` を持つスキーマは View 内で同一コンポーネントにバインド |
| `[Schema(bindingPath, format: "N0")]` | フィールド | `TMPro.TMP_Text.text` バインド時の書式指定文字列 |

### 1.2 バインディングパス (`bindingPath`) の解析

`SchemaAttribute` は `[CallerArgumentExpression]` を使い、ユーザーが `[Schema(PathResolver.TMPro.TMP_Text.text)]` のように書いた場合、`"Resolver."` 以降の文字列（例: `"TMPro.TMP_Text.text"`）を `BindingPath` プロパティとして保持する。  
SourceGenerator では `AttributeData.ConstructorArguments` からこの文字列値をそのまま読み取ることができる。

#### パスの分割規則

バインディングパス文字列を **最後の `.` で分割** する。

| バインディングパス | 型部分（最後の `.` より前） | メンバ名（最後の `.` より後） |
|---|---|---|
| `"TMPro.TMP_Text.text"` | `"TMPro.TMP_Text"` | `"text"` |
| `"UnityEngine.UI.Button.onClick"` | `"UnityEngine.UI.Button"` | `"onClick"` |
| `"UnityEngine.UI.Toggle.interactable"` | `"UnityEngine.UI.Toggle"` | `"interactable"` |

- **完全修飾型名（フィールド宣言用）:** `global::{型部分}`  
  例: `global::TMPro.TMP_Text`、`global::UnityEngine.UI.Button`
- **メンバアクセス（BindAll 内）:** `_field.{メンバ名}`  
  例: `_button.onClick`、`_toggle.interactable`

### 1.3 入力サンプル（CountViewModel.cs より抜粋）

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

## 2. フィールド名からの識別子変換規則

[CommunityToolkit MVVM `[ObservableProperty]`](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty) の命名規則に準拠する。ユーザーが `[Model]` または `[Schema]` に付与したフィールド名から、コンストラクタ引数名やプロパティ名を導出する。

### 2.1 正規化アルゴリズム（CommunityToolkit 準拠）

以下のステップを順に適用する:

1. 先頭の `_` をすべて取り除く（`TrimStart('_')`）  
2. ステップ1の結果が `m_` で始まる場合、`m_` を取り除く

```text
_count      →（1）count    →（2）該当なし → count
__count     →（1）count    →（2）該当なし → count
m_count     →（1）変化なし →（2）count              → count
_m_count    →（1）m_count  →（2）count              → count
m_Count     →（1）変化なし →（2）Count              → Count
__m_count   →（1）m_count  →（2）count              → count
count       →（1）変化なし →（2）該当なし → count
```

> **注意:** フィールド名が `_` や `m_` のみから構成される、または正規化後が空になるケースはコンパイルエラーとして扱う。

### 2.2 各識別子の導出

| 用途 | 変換 | 例（入力 → 正規化後 → 出力） |
|---|---|---|
| ViewModel プロパティ名 | 正規化後の先頭を大文字化 | `_count` → `count` → `Count` |
| | | `m_interactable` → `interactable` → `Interactable` |
| | | `m_Count` → `Count` → `Count`（すでに大文字） |
| | | `__value` → `value` → `Value` |
| | | `count` → `count` → `Count` |
| コンストラクタ引数名（`[Model]`） | 正規化後の先頭を小文字化 | `_model` → `model` → `model` |
| | | `_model2` → `model2` → `model2` |
| | | `m_Model` → `Model` → `model` |
| | | `myModel` → `myModel` → `myModel`（先頭はすでに小文字） |

---

## 3. アナライザー診断（Analyzer Diagnostics）

Roslyn SourceGenerator プロジェクト内にアナライザーを同梱し、以下の診断を出力する。

| 診断 ID | レベル | 条件 | メッセージ（案） |
|---|---|---|---|
| `BND001` | Error | `[ViewModel]` が付与されたクラス名に `"ViewModel"` が含まれない | `'ClassName' must contain 'ViewModel' in its name to use [ViewModel].` |

> **理由:** View クラス名は ViewModel クラス名中の `"ViewModel"` を `"View"` に置換して導出するため、`"ViewModel"` を含まない名前では View ファイルを生成できない。

---

## 4. 生成ルール：ViewModel (`{ClassName}.g.cs`)

### 4.1 クラス宣言

```csharp
#nullable enable

namespace {Namespace}
{
    [global::System.Serializable]          // ※ユーザーが既に [System.Serializable] を付与している場合は省略
    public partial class {ClassName} : global::Bindings.IViewModel
    {
```

### 4.2 自動生成フィールド

```csharp
private readonly global::Bindings.IMvvmPublisher _publisher;
```

### 4.3 公開プロパティ（`[Schema]` フィールドごと、宣言順）

```csharp
public {FieldType} {PropertyName}
{
    get => {fieldName};
    set
    {
        {fieldName} = value;
        PublishRebindMessage();
    }
}
```

プロパティ名の導出は「2.2 各識別子の導出」に従う。

### 4.4 コンストラクタ

引数の順序：`[Model]` フィールドの宣言順 → 最後に `global::Bindings.IMvvmPublisher publisher`

```csharp
public {ClassName}(
    global::{ModelType1} {paramName1},
    global::{ModelType2} {paramName2},
    ...,
    global::Bindings.IMvvmPublisher publisher)
{
    {fieldName1} = {paramName1};
    {fieldName2} = {paramName2};
    ...
    _publisher = publisher;
}
```

`[Model]` フィールドが0個の場合はモデル引数なし（`publisher` のみ）。

### 4.5 ヘルパーメソッド

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

## 5. 生成ルール：View (`{ViewClassName}.g.cs`)

### 5.1 View クラス名の命名規則

ViewModel クラス名中の `"ViewModel"` を `"View"` に置換する。

| ViewModel クラス名 | View クラス名 |
|---|---|
| `CountViewModel1` | `CountView1` |
| `CountViewModel6` | `CountView6` |
| `MyFeatureViewModel` | `MyFeatureView` |

クラス名に `"ViewModel"` が含まれない場合はアナライザー診断 `BND001` を出力し、生成を中断する（セクション3参照）。

### 5.2 生成ファイル名

- ViewModel: `{ClassName}.g.cs` （例: `CountViewModel1.g.cs`）
- View: `{ViewClassName}.g.cs` （例: `CountView1.g.cs`）

### 5.3 クラス宣言

View クラスには常に `[global::System.Serializable]` を付与する（Unity の `Binder` コンポーネントが `[SerializeReference]` でシリアライズするため）。

```csharp
#nullable enable

namespace {Namespace}
{
    [global::System.Serializable]
    public sealed partial class {ViewClassName} : global::Bindings.IView<global::{Namespace}.{ClassName}>
    {
```

### 5.4 _viewModel フィールド

```csharp
[global::System.NonSerialized]
private global::{Namespace}.{ClassName} _viewModel = null!;
```

### 5.5 UI コンポーネントフィールド

#### 5.5.1 View フィールド名の命名規則

バインディングパスの「型部分」から **クラス名**（最後のドット区切りセグメント）を取り出し、以下の変換を行う。

1. クラス名中の **最後の `_` より後ろの部分**を取り出す（`_` がなければクラス名全体）
2. 先頭を小文字化する
3. `_` をプレフィックスとして付ける

| バインディングパスの型部分 | クラス名 | フィールド名ベース |
|---|---|---|
| `TMPro.TMP_Text` | `TMP_Text` | `Text` → `_text` |
| `UnityEngine.UI.Button` | `Button` | `Button` → `_button` |
| `UnityEngine.UI.Toggle` | `Toggle` | `Toggle` → `_toggle` |

#### 5.5.2 コンポーネントの連番付与ルール

同一コンポーネント型（型部分が同じ）のスキーマをグループ化し、以下の3ケースで処理する。

**ケース A: 同一型のスキーマがすべて id=0（明示的な id なし）**

| 同一型の総スキーマ数 | フィールド名 |
|---|---|
| 1つ | `_{base}`（連番なし）例: `_text`、`_toggle` |
| 2つ以上 | `_{base}1`, `_{base}2`, ...（出現順に1から連番）例: `_button1`, `_button2` |

```
// ケース A-1: テキストが1つ → _text
[Schema("TMPro.TMP_Text.text")]

// ケース A-2: ボタンが2つ → _button1, _button2
[Schema("UnityEngine.UI.Button.onClick")]   // → _button1
[Schema("UnityEngine.UI.Button.onClick")]   // → _button2
```

**ケース B: 同一型のスキーマがすべて明示的 id（id > 0）**

- 同じ `id` を持つスキーマはすべて同一フィールドを共有する
- フィールド名: `_{base}{N}`（例: `_button1`、`_button2`）

```
// ケース B: id=1 と id=2 のボタン → _button1, _button2
[Schema("UnityEngine.UI.Button.onClick", id: 1)]  // → _button1
[Schema("UnityEngine.UI.Button.onClick", id: 1)]  // → _button1（同一フィールドを共有）
[Schema("UnityEngine.UI.Button.onClick", id: 2)]  // → _button2
```

**ケース C: id=0 と明示的 id が混在**

- id=0 のスキーマ → `_{base}0`（`0` 固定）
- id=N（N > 0）のスキーマ → `_{base}{N}`

```
// ケース C: id=0 と id=2 → _button0, _button2
[Schema("UnityEngine.UI.Button.onClick")]           // id=0 → _button0
[Schema("UnityEngine.UI.Button.onClick", id: 2)]    // id=2 → _button2
```

> **確認事項 Q-mixed:** ケース C において、id=0 のスキーマが**複数ある**場合（下記）はどう扱うか？  
> 同一フィールド `_button0` を共有する（= id=0 が「id=N に似たグループ」として振る舞う）？それともコンパイルエラー？  
> ```
> [Schema("UnityEngine.UI.Button.onClick")]           // id=0 → _button0 ?
> [Schema("UnityEngine.UI.Button.onClick")]           // id=0 → _button0 と共有? or エラー?
> [Schema("UnityEngine.UI.Button.onClick", id: 2)]    // id=2 → _button2
> ```

#### 5.5.3 フィールド宣言

```csharp
[global::UnityEngine.SerializeField]
private global::{TypePart} {_fieldName} = null!;
```

フィールドの順序: `[Schema]` フィールド（宣言順）→ `[Schema]` メソッド（宣言順）。同一コンポーネントが複数の `[Schema]` から参照される場合でもフィールドは1つ。

### 5.6 Initialize メソッド（インタフェース明示実装）

```csharp
void global::Bindings.IView<global::{Namespace}.{ClassName}>.Initialize(
    global::{Namespace}.{ClassName} viewModel)
{
    _viewModel = viewModel;
}
```

### 5.7 BindAsync メソッド

`requireBindImplementation: false`（デフォルト）の場合のみ生成する。

```csharp
global::System.Threading.Tasks.ValueTask global::Bindings.IView.BindAsync(
    global::System.Threading.CancellationToken _)
{
    BindAll();
    return default;
}
```

### 5.8 BindAll メソッド

`[Schema]` フィールドと `[Schema]` メソッドを宣言順に処理する。

```csharp
private void BindAll()
{
    // [Schema] フィールドのバインド（宣言順）
    {フィールドバインド...}
    // [Schema] メソッドのイベントバインド（宣言順）
    {イベントバインド...}
    OnPostBind();
    _viewModel.NotifyCompletedBind();
}
```

#### フィールドバインドの生成ルール

| 条件 | 生成コード |
|---|---|
| バインディングパスが **`"TMPro.TMP_Text.text"`** かつ `format` なし | `global::Bindings.TextMeshProExtensions.SetValue({_field}, _viewModel.{Property});` |
| バインディングパスが **`"TMPro.TMP_Text.text"`** かつ `format` あり | `global::Bindings.TextMeshProExtensions.SetValue({_field}, _viewModel.{Property}, "{format}");` |
| **それ以外すべて** | `{_field}.{member} = _viewModel.{Property};` |

> **補足:** `SetValue` 拡張メソッドは `TMPro.TMP_Text.text` の組み合わせに対してのみ用意されている。他のバインディングパス（他の TMPro 型を含む）に対する拡張メソッドは提供されないため、すべて直接代入を使う。

#### イベントバインドの生成ルール

同一コンポーネント・同一イベントに複数メソッドがバインドされる場合、`RemoveAllListeners()` は最初の1回のみ。

```csharp
{_field}.{event}.RemoveAllListeners();
{_field}.{event}.AddListener(_viewModel.{MethodName1});
{_field}.{event}.AddListener(_viewModel.{MethodName2});
```

### 5.9 partial void OnPostBind（View 側）

```csharp
partial void OnPostBind();
```

### 5.10 デバッグ用サブスクライバ（条件付きコンパイル）

データバインド（フィールド）のみ再バインドし、イベントバインド（メソッド）は行わない。

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD || !DISABLE_DEBUGTOOLKIT
    public sealed partial class {ViewClassName} : global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>
    {
        void global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>.OnReceivedMessage(
            global::Bindings.DebugBindMessage message)
        {
            message.BindTo(this);
            // [Schema] フィールドのデータバインドのみ（宣言順）
            {フィールドバインド...}
            OnPostBind();
            _viewModel.NotifyCompletedBind();
        }
    }
#endif
```

---

## 6. 全シナリオ対応表（CountViewModel.cs サンプル対応）

| # | シナリオ | 入力の特徴 | ViewModel 生成の変化点 | View 生成の変化点 |
|---|---|---|---|---|
| 1 | simple | 通常 | `[Serializable]` 自動付与 | `BindAsync` 生成あり |
| 2 | requireBindImplementation | `[ViewModel(requireBindImplementation: true)]` | なし | `BindAsync` 生成なし |
| 3 | alreadySerializable | ユーザーが `[System.Serializable]` 付与済み | `[Serializable]` 重複付与しない | なし |
| 4 | non model | `[Model]` なし | コンストラクタ引数は `publisher` のみ | なし |
| 5 | multi models | 複数 `[Model]` | コンストラクタに複数 Model 引数 | なし |
| 6 | same id pair | 同一 `id` の `[Schema]` メソッドが複数 | なし | 同一コンポーネントフィールドを共有 |
| 7 | format + non-text field | `format` 指定 + `TMPro.TMP_Text.text` 以外のフィールドスキーマ | なし | `SetValue` に `format` 引数追加、その他は直接代入 |

---

## 7. 未解決事項まとめ

| # | 質問 | 状態 |
|---|---|---|
| Q-mixed | ケース C（id=0 と id=N 混在）で id=0 スキーマが複数ある場合、同一フィールド `_type0` を共有するか、コンパイルエラーとするか？ | 要確認 |
