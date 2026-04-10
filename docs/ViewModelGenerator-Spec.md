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
| `[Required]` | フィールド | DI コンストラクタ引数として注入するモデル |
| `[Schema(bindingPath)]` | フィールド・メソッド | UI コンポーネントとのバインド対象を宣言する（`id` のデフォルトは `-1` = 未指定） |
| `[Schema(bindingPath, id: N)]` | フィールド・メソッド | `N ≥ 0`: 同じ `id` を持つスキーマは View 内で同一コンポーネントにバインド。`N < -1`: `BND002` エラー |
| `[Schema(bindingPath, format: "N0")]` | フィールド | `TMPro.TMP_Text.text` バインド時の書式指定文字列 |
| `[Schema(bindingPath, tooltip: "text")]` | フィールド・メソッド | Unity Inspector に表示するツールチップ文字列。View コンポーネントフィールドに `[global::UnityEngine.Tooltip("text")]` が付与される。同一 View フィールドに対して異なる tooltip が指定された場合は `BND003` 警告 |

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
    [Required]
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

[CommunityToolkit MVVM `[ObservableProperty]`](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty) の命名規則に準拠する。ユーザーが `[Required]` または `[Schema]` に付与したフィールド名から、コンストラクタ引数名やプロパティ名を導出する。

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
| | | `count` → `count` → `Count`（プレフィックスなし、先頭小文字 → 大文字化） |
| | | `m_interactable` → `interactable` → `Interactable` |
| | | `m_Count` → `Count` → `Count`（すでに大文字） |
| | | `__value` → `value` → `Value` |
| コンストラクタ引数名（`[Required]`） | 正規化後の先頭を小文字化 | `_model` → `model` → `model` |
| | | `_model2` → `model2` → `model2` |
| | | `m_Model` → `Model` → `model` |
| | | `myModel` → `myModel` → `myModel`（先頭はすでに小文字） |

---

## 3. アナライザー診断（Analyzer Diagnostics）

Roslyn SourceGenerator プロジェクト内にアナライザーを同梱し、以下の診断を出力する。

| 診断 ID | レベル | 条件 | メッセージ（案） |
|---|---|---|---|
| `BND001` | Error | `[ViewModel]` が付与されたクラス名に `"ViewModel"` が含まれない | `Type '{ClassName}' is annotated with [ViewModel] but its name does not contain "ViewModel". No View will be generated.` |
| `BND002` | Error | `[Schema]` の `id` に `-1` 未満の値が指定された | `[Schema] id value {id} is invalid. Use id >= 0 for explicit grouping, or omit id (defaults to -1) for auto-numbering.` |
| `BND003` | Warning | 同一 View コンポーネントフィールドに対して複数の `[Schema]` エントリが異なる非空 `tooltip` 文字列を指定した | `View field '{fieldName}' has conflicting tooltip values from multiple [Schema] entries with the same id. Only the first tooltip will be used.` |

> **理由:** View クラス名は ViewModel クラス名中の `"ViewModel"` を `"View"` に置換して導出するため、`"ViewModel"` を含まない名前では View ファイルを生成できない。

---

## 4. 生成ルール：ViewModel (`{ClassName}.g.cs`)

### 4.1 クラス/構造体宣言

```csharp
#nullable enable

namespace {Namespace}
{
    [global::System.Serializable]          // ※ユーザーが既に [System.Serializable] を付与している場合は省略
    public partial class {ClassName} : global::Bindings.IViewModel    // クラスの場合
    // public partial struct {ClassName} : global::Bindings.IViewModel  // 通常 struct の場合
    // public partial struct {ClassName} : global::Bindings.IViewModel  // readonly struct の場合
    {
```

> **注意:** 対象が `readonly struct` の場合でも `struct` キーワードのみを使用する（`readonly` は不要）。ユーザーが記述した `readonly` 修飾子は partial 側で保持される。

### 4.2 自動生成フィールド

```csharp
private readonly global::Bindings.IMvvmPublisher _publisher;
```

### 4.3 公開プロパティ（`[Schema]` フィールドごと、宣言順）

**通常のクラスまたは通常の struct の場合** (`get` + `set`):

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

**`readonly struct` の場合** (`get` のみ):

`readonly struct` ではフィールドへの書き込みができないため、`set` アクセサを生成しない。

```csharp
public {FieldType} {PropertyName}
{
    get => {fieldName};
}
```

プロパティ名の導出は「2.2 各識別子の導出」に従う。

### 4.4 コンストラクタ

引数の順序：`[Required]` フィールドの宣言順 → 最後に `global::Bindings.IMvvmPublisher publisher`

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

`[Required]` フィールドが0個の場合はモデル引数なし（`publisher` のみ）。

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

`id` のデフォルト値は `-1`（未指定）。ユーザーが `id ≥ 0` を指定した場合は明示的 id。同一コンポーネント型（型部分が同じ）のスキーマをグループ化し、以下の3ケースで処理する。

> **id 共有の原則（非 -1 の場合）:** `[Schema]` フィールドと `[Schema]` メソッドが同じ型部分かつ同じ `id ≥ 0` を持つ場合、View 内で同一コンポーネントフィールドを共有する。例えば、フィールド `[Schema("Button.interactable", id: 1)]` とメソッド `[Schema("Button.onClick", id: 1)]` は両方とも `_button1` を参照する。

> **id=-1 の場合:** 各エントリは独立して扱われる（他の id=-1 エントリとの共有はない）。同一型内でそれぞれ別のフィールドに割り当てられる。

**ケース A: 同一型のスキーマがすべて id=-1（明示的 id なし）**

| 同一型の総スキーマ数 | フィールド名 |
|---|---|
| 1つ | `_{base}`（連番なし）例: `_text`、`_toggle` |
| 2つ以上 | `_{base}1`, `_{base}2`, ...（出現順に1から連番）例: `_button1`, `_button2` |

```
// ケース A-1: テキストが1つ → _text
[Schema("TMPro.TMP_Text.text")]

// ケース A-2: ボタンが2つ（両方 id=-1） → _button1, _button2（それぞれ独立したフィールド）
[Schema("UnityEngine.UI.Button.onClick")]   // → _button1
[Schema("UnityEngine.UI.Button.onClick")]   // → _button2
```

**ケース B: 同一型のスキーマがすべて明示的 id（id ≥ 0）**

- 同じ `id` を持つスキーマはすべて同一フィールドを共有する（`[Schema]` フィールドと `[Schema]` メソッドが混在してもよい）
- フィールド名: `_{base}{N}`（例: `_button0`、`_button1`、`_button2`）

```
// ケース B-1: id=1 と id=2 のボタン → _button1, _button2
[Schema("UnityEngine.UI.Button.onClick", id: 1)]  // → _button1
[Schema("UnityEngine.UI.Button.onClick", id: 1)]  // → _button1（同一フィールドを共有）
[Schema("UnityEngine.UI.Button.onClick", id: 2)]  // → _button2

// ケース B-2: フィールドとメソッドが同じ id → 共有
[Schema("UnityEngine.UI.Button.interactable", id: 1)]  // フィールド → _button1
[Schema("UnityEngine.UI.Button.onClick", id: 1)]        // メソッド → _button1（同一フィールドを共有）

// id=0 は明示的な 0 番グループとして扱われる
[Schema("UnityEngine.UI.Button.onClick", id: 0)]  // → _button0
```

**ケース C: id=-1（未指定）と明示的 id（≥ 0）が混在**

- 明示的 id（≥ 0）のスキーマ → `_{base}{N}`
- id=-1（未指定）のスキーマ → 明示的 id と重複しない最小の正整数を出現順に割り当て（1から順にスキップしながら）

```
// ケース C: 未指定2つ + id=2 のボタン → _button1, _button2（id=2と共有）, _button3
[Schema("UnityEngine.UI.Button.onClick")]           // id=-1 → 未使用の最小番 → _button1
[Schema("UnityEngine.UI.Button.onClick", id: 2)]    // id=2  → _button2
[Schema("UnityEngine.UI.Button.onClick")]           // id=-1 → 次の未使用番 → _button3
```

#### 5.5.3 フィールド宣言

`tooltip` が指定されている場合は `[global::UnityEngine.Tooltip]` を `[global::UnityEngine.SerializeField]` の直前に付与する。

```csharp
// tooltip が指定された場合
[global::UnityEngine.Tooltip("{tooltip}")]
[global::UnityEngine.SerializeField]
private global::{TypePart} {_fieldName} = null!;

// tooltip が指定されていない場合
[global::UnityEngine.SerializeField]
private global::{TypePart} {_fieldName} = null!;
```

フィールドの順序: `[Schema]` フィールド（宣言順）→ `[Schema]` メソッド（宣言順）。同一コンポーネントが複数の `[Schema]` から参照される場合でもフィールドは1つ。

#### 5.5.4 tooltip の決定規則

- 同一 View コンポーネントフィールドに紐付く複数の `[Schema]` エントリのうち、**最初に現れる非空 tooltip 値** を採用する。
- 異なる非空 tooltip 値が複数存在する場合は `BND003` 警告を出力する（最初の値を使用し続ける）。
- id=-1 のエントリはそれぞれ独立したフィールドになるため、tooltip の競合は発生しない。

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
| 4 | non model | `[Required]` なし | コンストラクタ引数は `publisher` のみ | なし |
| 5 | multi models | 複数 `[Required]` | コンストラクタに複数 Model 引数 | なし |
| 6 | same id pair | 同一 `id` の `[Schema]` メソッドが複数（ケース B） | なし | 同一コンポーネントフィールドを共有。`RemoveAllListeners` は1回のみ |
| 7 | format + non-text field | `format` 指定 + `TMPro.TMP_Text.text` 以外のフィールドスキーマ | なし | `SetValue` に `format` 引数追加、その他は直接代入 |
| 8 | readonly struct | `readonly partial struct` | `[Schema]` フィールドのプロパティは `get` のみ（`set` なし） | 変化なし |

---

## 7. 未解決事項まとめ

現時点で未解決の事項はありません。
