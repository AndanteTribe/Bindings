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

ユーザーが `[Model]` または `[Schema]` に付与したフィールド名から、コンストラクタ引数名やプロパティ名を導出する。  
ユーザーが必ずしもアンダースコア形式を使うとは限らないため、以下のアルゴリズムを適用する。

### 2.1 正規化（ストリッピング）

1. 先頭の `_` を1つ取り除く（例: `_count` → `count`）  
2. 先頭の `m_` を取り除く（例: `m_count` → `count`、`_m_count` → `m_count` → ステップ1後に `m_count` → このケースは `_` 除去後に再判定）  
3. 上記に該当しない場合はフィールド名をそのまま使用（例: `count` → `count`）

### 2.2 各識別子の導出

| 用途 | 変換 | 例 |
|---|---|---|
| ViewModel プロパティ名 | 正規化後の先頭を大文字化 | `_count` → `Count`、`m_interactable` → `Interactable`、`count` → `Count` |
| コンストラクタ引数名（`[Model]`） | 正規化後の先頭を小文字化 | `_model` → `model`、`_model2` → `model2`、`myModel` → `myModel` |

**未解決事項 Q-naming:** `m_Count`（先頭大文字）や `__count`（二重アンダースコア）のようなエッジケースの扱いは？  
→ 詳細を詰める必要あり。

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

同一コンポーネント型（型部分が同じ）の複数スキーマを処理する際のフィールド名の決定ルール：

**ステップ1: id=N（N > 0）のスキーマを処理する**
- 同じ `id` を持つスキーマはすべて同一フィールドを参照する
- フィールド名: `_{base}{N}` （例: `_button1`、`_button3`）

**ステップ2: id=0（デフォルト）のスキーマを処理する**
- 同一型の全スキーマ（id=0 のもの + id=N のもの）を合わせた数に応じて：
  - **同一型のスキーマが全体で1つ（かつ id=0）:** 連番なし → `_{base}` （例: `_text`、`_toggle`）
  - **同一型のスキーマが全体で複数:** すべて連番あり
    - id=0 のものは出現順に **ステップ1で既に使われた番号と重複しない** 番号を割り当てる
    - id=0 のスキーマが複数ある場合は出現順に1から連番（ただし id=N で使われた番号はスキップ）

**id=0 と id=N が混在する場合の例:**

```
[Schema("UnityEngine.UI.Button.onClick")]          // id=0、1つ目 → _button0（0はデフォルト値として確定）
[Schema("UnityEngine.UI.Button.onClick", id: 2)]   // id=2       → _button2
```

> **未解決事項 Q-mixed:** id=0 と id=N が混在する場合の id=0 のフィールド名の番号は `0` で固定か、それとも最小の非衝突番号か？  
> 暫定素案: id=0 のものは `_button0` とし、id=N のものは `_button{N}` とする。

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
| Q-naming | `m_Count`（大文字）や `__count`（二重 `_`）などエッジケースの正規化ルールは？ | 未解決 |
| Q-mixed | id=0 と id=N が同一型コンポーネントで混在する場合、id=0 のフィールド名の番号は `0` 固定か否か？ | 暫定: `_type0` |
