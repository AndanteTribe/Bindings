# ViewModelGenerator — Source Generator Specification

## 概要 (Overview)

`[ViewModel]` アノテーションが付与されたクラスまたは構造体を解析し、以下の2ファイルを自動生成する Roslyn `IIncrementalGenerator`。

| 生成ファイル | 内容 |
|---|---|
| `{FullyQualifiedViewModelName}.g.cs` | ViewModel の partial クラス（バックグラウンドロジック） |
| `{FullyQualifiedViewName}.g.cs` | View の sealed partial クラス（Unity UI バインド） |

> **原則:** 生成コード内のすべての型参照は `global::` プレフィックスを付ける。これはユーザー定義の型名と衝突しないようにするためである。

---

## 1. 入力：ユーザー記述コード

### 1.1 使用属性

| 属性 | 対象 | 意味 |
|---|---|---|
| `[ViewModel]` | クラス・構造体 | この型を ViewModel として扱う |
| `[ViewModel(requireBindImplementation: true)]` | クラス・構造体 | `BindAsync` を自動生成せず、ユーザーが実装する |
| `[Required]` | フィールド | 生成されるコンストラクタに引数として追加するフィールド |
| `[Schema(bindingPath)]` | フィールド・メソッド | UI コンポーネントとのバインド対象を宣言する（`id` のデフォルトは `-1` = 未指定） |
| `[Schema(bindingPath, id: N)]` | フィールド・メソッド | `N ≥ 0`: 同じ `id` を持つスキーマは View 内で同一コンポーネントにバインド。`N < -1`: `BND002` エラー |
| `[Schema(bindingPath, format: "N0")]` | フィールド | `TMPro.TMP_Text.text` バインド時の書式指定文字列 |
| `[Schema(bindingPath, tooltip: "text")]` | フィールド・メソッド | Unity Inspector に表示するツールチップ文字列。View コンポーネントフィールドに `[global::UnityEngine.Tooltip("text")]` が付与される。同一 View フィールドに対して異なる tooltip が指定された場合は `BND003` エラー |

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
| `"UnityEngine.RectTransform.rect.size"` | `"UnityEngine.RectTransform"` | `"rect.size"` |

- **完全修飾型名（フィールド宣言用）:** `global::{型部分}`  
  例: `global::TMPro.TMP_Text`、`global::UnityEngine.UI.Button`
- **メンバアクセス（BindAll 内）:** `_field.{メンバ名}`  
  例: `_incrementButton.onClick`、`_enabledToggle.interactable`

`"UnityEngine.RectTransform.rect.size"` は多段パスの例外であり、View のコンポーネントフィールド型が `global::UnityEngine.RectTransform` になるよう型部分を正規化する。同じ `id` を持つ `UnityEngine.RectTransform` の他のバインディング（例: `sizeDelta`）とは同一コンポーネントフィールドにグループ化する。

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
| コンストラクタ引数名（`[Required]`） | 正規化後の先頭を小文字化 | `_service` → `service` → `service` |
| | | `_service2` → `service2` → `service2` |
| | | `m_Service` → `Service` → `service` |
| | | `myService` → `myService` → `myService`（先頭はすでに小文字） |

---

## 3. アナライザー診断（Analyzer Diagnostics）

Roslyn SourceGenerator プロジェクト内にアナライザーを同梱し、以下の診断を出力する。

| 診断 ID | レベル | 条件 | メッセージ（案） |
|---|---|---|---|
| `BND001` | Error | `[ViewModel]` が付与された型名に `"ViewModel"` が含まれない | `Type '{ClassName}' is annotated with [ViewModel] but its name does not contain "ViewModel". No source will be generated for this ViewModel.` |
| `BND002` | Error | `[Schema]` の `id` に `-1` 未満の値が指定された | `[Schema] id value {id} is invalid. Use id >= 0 for explicit grouping, or omit id (defaults to -1) for auto-numbering.` |
| `BND003` | Error | 同一 View コンポーネントフィールドに対して複数の `[Schema]` エントリが異なる非空 `tooltip` 文字列を指定した | `View field '{fieldName}' has conflicting tooltip values from multiple [Schema] entries with the same id. Only the first tooltip will be used.` |
| `BND004` | Error | `[ViewModel]` 型またはいずれかの親型のアクセシビリティを生成コードで表現できない | `Type '{TypeName}' has unsupported accessibility '{Accessibility}'. No source will be generated for ViewModel '{ViewModelName}'.` |
| `BND005` | Warning | `[ViewModel]` が付与された型のフィールドに Unity の `[SerializeField]` または `[SerializeReference]` が付与されている | `Field '{fieldName}' uses [{SerializationAttributeName}] with ViewModel type '{ViewModelTypeName}'. Unity deserialization may leave generated runtime state uninitialized, even when the type is marked [Serializable]. Construct or assign the ViewModel at runtime instead.` |

> **理由:** View クラス名は ViewModel クラス名中の `"ViewModel"` を `"View"` に置換して導出するため、`"ViewModel"` を含まない名前では View ファイルを生成できない。

`BND001` が発生した ViewModel については、ViewModel ソースと View ソースの両方を生成しない。

`BND004` が発生した ViewModel については、Generator 全体を例外で停止させず、その ViewModel の ViewModel ソースと View ソースの両方を生成しない。

`BND005` は `[SerializeField]` または `[SerializeReference]` 属性の位置に報告する。ViewModel 型の `[System.Serializable]` は Generator からは `UNITY_EDITOR` でのみ付与される。ユーザーが明示的に `[System.Serializable]` を付与した場合でも、Unity のデシリアライズ後に Generator が必要とするランタイム状態が正しく初期化されることは保証しないため、ViewModel は実行時に構築または代入する。対象 ViewModel の ViewModel ソースと View ソースの生成は継続する。ViewModel 型以外のフィールドは対象外とする。

ユーザーによる ViewModel 型への `[System.Serializable]` の付与だけでは診断を報告しない。これは独自シリアライザーなど Unity のフィールドシリアライズを使用しない用途を許容するためであり、Bindings が Unity による ViewModel の永続化をサポートすることを意味しない。BND005 は、ViewModel 型のフィールドへ `[SerializeField]` または `[SerializeReference]` が付与された時点で、ViewModel 型自身の `[System.Serializable]` の有無にかかわらず報告する。

---

## 4. 生成ルール：ViewModel (`{FullyQualifiedViewModelName}.g.cs`)

### 4.1 クラス/構造体宣言

対象型の宣言済みアクセシビリティを、生成する ViewModel の partial 宣言へそのまま反映する。ViewModel がネスト型の場合は、外側から内側までのすべての親型についても型種別、単純名、宣言済みアクセシビリティを再現する。親型は生成側でも partial 宣言するため、ユーザー側の対応する親型も partial でなければならない。

| `Microsoft.CodeAnalysis.Accessibility` | 生成する C# キーワード |
|---|---|
| `NotApplicable` | 対応外。`BND004` を報告し、この ViewModel の生成を中止 |
| `Private` | `private` |
| `ProtectedAndInternal` | `private protected` |
| `Protected` | `protected` |
| `Internal` | `internal` |
| `ProtectedOrInternal` | `protected internal` |
| `Public` | `public` |

上表にない未知の値も `BND004` の対象とする。トップレベル型でC#として使用できないアクセシビリティが記述された場合は入力コード自体のコンパイル診断に委ね、Generator はRoslynから得た値を上表どおり処理する。

```csharp
#nullable enable

namespace {Namespace}
{
    // ネスト型の場合、外側から内側まで親型ごとに繰り返す
    {ContainingTypeAccessibility} partial {ContainingTypeKeyword} {ContainingTypeName}
    {
#if UNITY_EDITOR
        [global::System.Serializable]          // ※ユーザーが既に [System.Serializable] を付与している場合は、この条件付き属性全体を省略
#endif
        {Accessibility} partial class {ClassName} : global::Bindings.IViewModel    // クラスの場合
        // {Accessibility} partial struct {ClassName} : global::Bindings.IViewModel  // 通常 struct の場合
        // {Accessibility} partial struct {ClassName} : global::Bindings.IViewModel  // readonly struct の場合
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
    global::{Type1} {paramName1},
    global::{Type2} {paramName2},
    ...,
    global::Bindings.IMvvmPublisher publisher)
{
    {fieldName1} = {paramName1};
    {fieldName2} = {paramName2};
    ...
    _publisher = publisher;
}
```

`[Required]` フィールドが0個の場合は `publisher` 引数のみ。

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

## 5. 生成ルール：View (`{FullyQualifiedViewName}.g.cs`)

### 5.1 View クラス名の命名規則

ViewModel クラス名中の `"ViewModel"` を `"View"` に置換する。

| ViewModel クラス名 | View クラス名 |
|---|---|
| `CountViewModel1` | `CountView1` |
| `CountViewModel6` | `CountView6` |
| `MyFeatureViewModel` | `MyFeatureView` |

クラス名に `"ViewModel"` が含まれない場合はアナライザー診断 `BND001` を出力し、ViewModel の partial ソースと View ソースの両方を生成しない（セクション3参照）。

### 5.2 生成ファイル名

`{FullyQualifiedViewModelName}` と `{FullyQualifiedViewName}` は、それぞれ生成される ViewModel 型と View 型を識別する名前であり、名前空間、親型チェーン、型名をすべて `.` で連結する。

- ViewModel: `{FullyQualifiedViewModelName}.g.cs`（例: `Bindings.Sample.CountViewModel1.g.cs`）
- View: `{FullyQualifiedViewName}.g.cs`（例: `Bindings.Sample.CountView1.g.cs`）
- ネスト型の例: `Bindings.Sample.Outer.CountViewModel1.g.cs`、`Bindings.Sample.Outer.CountView1.g.cs`

### 5.3 クラス宣言

View クラスには常に `[global::System.Serializable]` を付与する（Unity の `Binder` コンポーネントが `[SerializeReference]` でシリアライズするため）。View のアクセシビリティには対象 ViewModel と同じキーワードを使用し、ネスト型の場合は ViewModel ソースと同じ親型チェーンを再現する。以下の `{ViewModelFullName}` は、名前空間とすべての親型を含む `global::` 付きの完全修飾名を表す。

```csharp
#nullable enable

namespace {Namespace}
{
    // ネスト型の場合、外側から内側まで親型ごとに繰り返す
    {ContainingTypeAccessibility} partial {ContainingTypeKeyword} {ContainingTypeName}
    {
        [global::System.Serializable]
        {Accessibility} sealed partial class {ViewClassName} : global::Bindings.IView<{ViewModelFullName}>
        {
```

### 5.4 _viewModel フィールド

```csharp
[global::System.NonSerialized]
private {ViewModelFullName} _viewModel = null!;
```

### 5.5 UI コンポーネントフィールド

#### 5.5.1 View フィールド名の命名規則

View フィールド名は、`[Schema]` を定義した ViewModel メンバー名と、バインディングパスの「型部分」から求めるコンポーネント名を連結して生成する。

ViewModel メンバー名は次のように正規化する。

- フィールド: 先頭の `_` をすべて除去し、続けて `m_` があれば除去した後、先頭を小文字化する
- メソッド: 先頭を小文字化する

コンポーネント名は従来どおり次のように求め、連結用に先頭を大文字化する。

1. 型部分からクラス名（最後のドット区切りセグメント）を取り出す
2. クラス名中の最後の `_` より後ろの部分を取り出す（末尾が `_` の場合はクラス名全体へフォールバック）
3. 先頭を大文字化する

最終的な候補名は `_{memberBase}{ComponentBase}`。

| ViewModel メンバー | バインディングパスの型部分 | フィールド名候補 |
|---|---|---|
| `_count` | `TMPro.TMP_Text` | `_countText` |
| `Increment` | `UnityEngine.UI.Button` | `_incrementButton` |
| `m_IsOn` | `UnityEngine.UI.Toggle` | `_isOnToggle` |

#### 5.5.2 コンポーネントの連番付与ルール

`id` のデフォルト値は `-1`（未指定）。同一の型部分かつ同じ明示的 `id ≥ 0` を持つ `[Schema]` エントリは、従来どおり1つのコンポーネントフィールドを共有する。`id=-1` の各エントリは独立したコンポーネントとして扱う。

明示的 id で共有されるグループの候補名には、フィールド→メソッドの収集順で最初のエントリの ViewModel メンバー名を使う。id の値自体はフィールド名へ直接付加しない。

論理コンポーネントグループごとに 5.5.1 の候補名を求め、同じ候補名が1つだけなら連番を付けない。同じ候補名を持つ独立グループが複数ある場合のみ、出現順に `1`, `2`, ... を付ける。生成済みの別候補名と衝突する場合も、未使用の番号まで進める。

```
// メンバー名が異なる独立ボタン → 連番なし
[Schema("UnityEngine.UI.Button.onClick")]
public void Increment() {}  // → _incrementButton

[Schema("UnityEngine.UI.Button.onClick")]
public void Decrement() {}  // → _decrementButton

// 同じメンバーに同型の独立コンポーネントが2つ → 連番あり
[Schema("UnityEngine.UI.Button.onClick")]
[Schema("UnityEngine.UI.Button.onClick")]
public void Submit() {}     // → _submitButton1, _submitButton2

// 同じ明示的 id を共有 → 最初のメンバー名による1フィールド
[Schema("UnityEngine.UI.Button.onClick", id: 1)]
public void Accept() {}     // → _acceptButton

[Schema("UnityEngine.UI.Button.onClick", id: 1)]
public void Confirm() {}    // → _acceptButton（同一フィールドを共有）
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
- 異なる非空 tooltip 値が複数存在する場合は `BND003` エラーを出力する（最初の値を使用し続ける）。
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
| バインディングパスが **`"UnityEngine.GameObject.activeSelf"`** | `{_field}.SetActive(_viewModel.{Property});` |
| バインディングパスが **`"UnityEngine.RectTransform.rect.size"`** | `{_field}.SetSizeWithCurrentAnchors(global::UnityEngine.RectTransform.Axis.Horizontal, _viewModel.{Property}.x);` の後に、垂直軸と `.y` について同様に呼び出す |
| **それ以外すべて** | `{_field}.{member} = _viewModel.{Property};` |

> **補足:** `SetValue` 拡張メソッドは `TMPro.TMP_Text.text` の組み合わせに対してのみ用意されている。`GameObject.activeSelf` と `RectTransform.rect.size` は上記の専用 API を使用し、それ以外は直接代入を使う。`RectTransform.sizeDelta` は例外扱いせず、従来どおり直接代入する。

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
    {Accessibility} sealed partial class {ViewClassName} : global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>
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
| 1 | simple | 通常 | `UNITY_EDITOR` の場合のみ `[Serializable]` 自動付与 | `BindAsync` 生成あり |
| 2 | requireBindImplementation | `[ViewModel(requireBindImplementation: true)]` | なし | `BindAsync` 生成なし |
| 3 | alreadySerializable | ユーザーが `[System.Serializable]` 付与済み | 条件付きの `[Serializable]` を重複付与しない | なし |
| 4 | no required | `[Required]` なし | コンストラクタ引数は `publisher` のみ | なし |
| 5 | multi required | 複数 `[Required]` | コンストラクタに複数 `[Required]` 引数 | なし |
| 6 | same id pair | 同一 `id` の `[Schema]` メソッドが複数（ケース B） | なし | 同一コンポーネントフィールドを共有。`RemoveAllListeners` は1回のみ |
| 7 | format + non-text field | `format` 指定 + `TMPro.TMP_Text.text` 以外のフィールドスキーマ | なし | `SetValue` に `format` 引数追加、その他は直接代入 |
| 8 | readonly struct | `readonly partial struct` | `[Schema]` フィールドのプロパティは `get` のみ（`set` なし） | 変化なし |
| 9 | accessibility | `public`、`internal`、または各種アクセシビリティのネスト型 | 対象型と全親型のアクセシビリティ・型種別を維持 | ViewModel と同じアクセシビリティ・親型チェーンを維持 |

---

## 7. 未解決事項まとめ

現時点で未解決の事項はありません。
