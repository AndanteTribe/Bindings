# Bindings
[![dotnet-test](https://github.com/AndanteTribe/Bindings/actions/workflows/dotnet-test.yml/badge.svg)](https://github.com/AndanteTribe/Bindings/actions/workflows/dotnet-test.yml)
[![Releases](https://img.shields.io/github/release/AndanteTribe/Bindings.svg)](https://github.com/AndanteTribe/Bindings/releases)
[![openupm](https://img.shields.io/npm/v/jp.andantetribe.bindings?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/jp.andantetribe.bindings/)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/Bindings.svg)](./LICENSE)

[English](README.md) | 日本語

## 概要
**Bindings** は、uGUI をベースにした Unity 向け MVVM 自動 UI バインディングフレームワークです。

Roslyn ソースジェネレーターを利用し、アノテーションを付与した C# コードから ViewModel と View の partial クラスを自動生成することで、ボイラープレートを排除し、UI とデータモデルを常に同期させます。

`PublishRebindMessage()` によって発行されたバインド要求は内部でキューに積まれ、Canvas レンダリング直前（`Canvas.preWillRenderCanvases`）に一括実行されます。これにより、1フレーム内での無駄な更新が回避されます。

## 要件
- Unity 6000.0 以上
- Unity uGUI 2.0.0 以上

## インストール
`Window > Package Manager` から Package Manager ウィンドウを開き、`[+] > Add package from git URL` を選択して以下の URL を入力します。

```
https://github.com/AndanteTribe/Bindings.git?path=src/Bindings.Unity/Packages/jp.andantetribe.bindings
```

## クイックスタート

### 1. ViewModel を定義する

`partial` クラスに `[ViewModel]` を付与します。`[Required]` でコンストラクタ引数を宣言し、`[Schema]` で UI バインドを宣言します。

```csharp
using Bindings;
using UnityEngine;

namespace MyApp
{
    [ViewModel]
    public partial class CounterViewModel
    {
        [Required]
        private readonly CounterModel _model;

        [SerializeField]
        [Schema(PathResolver.TMPro.TMP_Text.text)]
        private int _count;

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Increment()
        {
            Count += 1;
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Decrement()
        {
            Count -= 1;
        }

        partial void OnPostBind()
        {
            _model.Count = Count;
        }
    }
}
```

ソースジェネレーターにより以下のファイルが自動生成されます。
- `MyApp.CounterViewModel.g.cs` — プロパティ、コンストラクタ、パブリッシャー配線を含む partial ViewModel クラス。
- `MyApp.CounterView.g.cs` — シリアライズされた UI コンポーネントフィールドとバインドロジックを含む sealed partial View クラス。

### 生成されるコード

**`MyApp.CounterViewModel.g.cs`**

```csharp
#nullable enable

namespace MyApp
{
#if UNITY_EDITOR
    [global::System.Serializable]
#endif
    public partial class CounterViewModel : global::Bindings.IViewModel
    {
        private readonly global::Bindings.IMvvmPublisher _publisher;

        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                PublishRebindMessage();
            }
        }

        public CounterViewModel(global::MyApp.CounterModel model, global::Bindings.IMvvmPublisher publisher)
        {
            _model = model;
            _publisher = publisher;
            OnPostConstruct();
        }

        public void NotifyCompletedBind() => OnPostBind();

        partial void OnPostBind();

        partial void OnPostConstruct();

        [global::System.Runtime.CompilerServices.MethodImpl(
            global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void PublishRebindMessage()
        {
            _publisher.PublishRebindMessage<CounterViewModel>();
        }
    }
}
```

**`MyApp.CounterView.g.cs`**

```csharp
#nullable enable

namespace MyApp
{
    [global::System.Serializable]
    public sealed partial class CounterView : global::Bindings.IView<global::MyApp.CounterViewModel>
    {
        [global::System.NonSerialized]
        private global::MyApp.CounterViewModel _viewModel = null!;

        [global::UnityEngine.SerializeField]
        private global::TMPro.TMP_Text _countText = null!;

        [global::UnityEngine.SerializeField]
        private global::UnityEngine.UI.Button _incrementButton = null!;

        [global::UnityEngine.SerializeField]
        private global::UnityEngine.UI.Button _decrementButton = null!;

        void global::Bindings.IView<global::MyApp.CounterViewModel>.Initialize(
            global::MyApp.CounterViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        global::System.Threading.Tasks.ValueTask global::Bindings.IView.BindAsync(
            global::System.Threading.CancellationToken _)
        {
            BindAll();
            return default;
        }

        private void BindAll()
        {
            global::Bindings.TextMeshProExtensions.SetValue(_countText, _viewModel.Count);
            _incrementButton.onClick.RemoveAllListeners();
            _incrementButton.onClick.AddListener(_viewModel.Increment);
            _decrementButton.onClick.RemoveAllListeners();
            _decrementButton.onClick.AddListener(_viewModel.Decrement);
            OnPostBind();
            _viewModel.NotifyCompletedBind();
        }

        partial void OnPostBind();
    }

#if UNITY_EDITOR || !DISABLE_DEBUGTOOLKIT
    public sealed partial class CounterView : global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>
    {
        void global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>.OnReceivedMessage(
            global::Bindings.DebugBindMessage message)
        {
            message.BindTo(this);
            global::Bindings.TextMeshProExtensions.SetValue(_countText, _viewModel.Count);
            OnPostBind();
            _viewModel.NotifyCompletedBind();
        }
    }
#endif
}
```

### 2. シーンに Binder を設定する

1. シーン内の GameObject に `Binder` コンポーネントを追加します。
2. Inspector で、生成された `CounterView` インスタンスを **Views** リストに割り当てます。
3. `binder.Initialize(viewModel)` で ViewModel を関連付け、`binder.Run()` を呼び出す（または **Run On Start** を有効にする）と UI がバインドされます。

```csharp
public class GameEntry : MonoBehaviour
{
    [SerializeField] private Binder _binder;

    private void Start()
    {
        var model = new CounterModel();
        var publisher = _binder; // Binder は IMvvmPublisher を実装しています
        var viewModel = new CounterViewModel(model, publisher);
        _binder.Initialize(viewModel);
    }
}
```

#### Binder Inspector の使い方

`Binder` コンポーネントにはカスタム Unity Inspector が付属しており、コードを書かずに View を登録できます。

**手順 1 — View の追加：名前空間の選択**

**Add View** ボタンをクリックすると、ドロップダウンが表示されます。登録したい View が属する名前空間を選択します。

![Add View – 名前空間の選択](https://github.com/user-attachments/assets/a9c4f6a7-9c4e-44b9-af94-5e830600e57e)

**手順 2 — View の追加：View の選択**

名前空間を選択すると、その名前空間に含まれるすべての View 型の一覧が表示されます。追加したい View をクリックします。

![Add View – View の選択](https://github.com/user-attachments/assets/f79c5a2b-2cac-46b2-889f-0cd3cd05f283)

**手順 3 — UI コンポーネントの割り当て**

選択した View が **Views** リストに登録されます。View に宣言された各 `[SerializeField]` が Inspector にスロットとして表示されるので、対応する UI GameObject をそれぞれのスロットにドラッグして割り当てます。View を削除するには、エントリ右上の **−** ボタンをクリックします。

![UI コンポーネントの割り当て](https://github.com/user-attachments/assets/fff8c683-2c58-47b3-b964-29e040337fc9)

**手順 4 — Preview**

**Add View** ボタンの下にある **Preview** ボタンをクリックすると、登録されている各 View に紐づく ViewModel オブジェクトが表示されます。Inspector から直接値を入力し、**Invoke** ボタンを押すとバインドが適用されるので、UI が想定通りに更新されるか検証できます。

![Preview と Invoke](https://github.com/user-attachments/assets/2d34fa59-19ec-4e8b-8c82-10608e63b287)

## 属性一覧

### `[ViewModel]`

`partial` クラスまたは構造体に付与し、ViewModel としてマークしてソース生成をトリガーします。

| パラメータ | 型 | デフォルト | 説明 |
|-----------|-----|-----------|------|
| `requireBindImplementation` | `bool` | `false` | `true` の場合、`BindAsync` の自動生成をスキップします。ユーザーが partial View クラスに手動で実装する必要があります。 |

### `[Required]`

フィールドまたはプロパティに付与し、生成されるコンストラクタの引数として追加します。生成されるコンストラクタはそのフィールドの型の引数を受け取り、代入します。

ViewModel クラスでは、生成コンストラクタはすべての `[Required]` メンバと内部の `IMvvmPublisher` を代入した後に `partial void OnPostConstruct();` を呼び出します。コンストラクタと競合する独自実装を追加する代わりに、このフックを partial クラス側で実装して一度だけ必要な初期化を行ってください。

### `[Schema(bindingPath, id, format, tooltip)]`

フィールドまたはメソッドに付与し、UI コンポーネントにバインドします。同一メンバに複数回付与できます。

| パラメータ | 型 | デフォルト | 説明 |
|-----------|-----|-----------|------|
| `bindingPath` | `string` | 必須 | バインド先の UI コンポーネントメンバを `PathResolver` 経由で指定します（例: `PathResolver.TMPro.TMP_Text.text`）。 |
| `id` | `int` | `-1` | 複数の `[Schema]` を同一 View コンポーネントフィールドにグループ化します。`id >= 0` で明示的にグループ化し、`-1` は自動採番です。`-1` 未満は `BND002` を発生させます。 |
| `format` | `string` | `""` | `TMPro.TMP_Text.text` バインド時に適用する書式文字列（例: `"N0"`）。それ以外のバインディングパスでは無視されます。 |
| `tooltip` | `string` | `""` | 生成された View コンポーネントフィールドに表示する Unity Inspector のツールチップテキスト。 |

#### GameObject のアクティブ状態バインド

`bindingPath` に `UnityEngine.GameObject.activeSelf`（または `PathResolver.UnityEngine.GameObject.activeSelf`）を指定した場合、生成される View コードでは `GameObject.SetActive(bool)` を呼び出して GameObject のアクティブ状態を切り替えます。`activeSelf` は読み取り専用のプロパティであるため、内部的に `SetActive` にマッピングして正しく状態を変更します。

例:

```csharp
[Schema(PathResolver.UnityEngine.GameObject.activeSelf)]
private bool _isVisible;
```

生成されるバインディングでは `_isVisibleGameObject.SetActive(_viewModel.IsVisible);`（または相当する生成ヘルパー）が呼ばれ、アクティブ状態が反映されます。

#### RectTransform の計算後サイズバインド

現在のアンカーを維持しながら `RectTransform` の計算後サイズへ `Vector2` をバインドする場合は、`PathResolver.UnityEngine.RectTransform.rect.size` を指定します。

```csharp
[Schema(PathResolver.UnityEngine.RectTransform.rect.size)]
private Vector2 _size;
```

生成される View は、水平軸へ `Size.x`、垂直軸へ `Size.y` を渡して `RectTransform.SetSizeWithCurrentAnchors` をそれぞれ1回呼び出します。この動作は明示的にこのパスを選んだ場合だけ有効であり、`PathResolver.UnityEngine.RectTransform.sizeDelta` は引き続き `RectTransform.sizeDelta` へ直接代入します。

## `IMvvmSubscriber<T>` によるメッセージング

自動 UI 再バインドに加え、ViewModel は任意のメッセージを、オプトインした View に送信できます。これにより、ViewModel を View の具体的な型に依存させることなく、ダイアログ表示やアニメーション再生などのイベントをプッシュできます。

**仕組み:**

1. メッセージ型を定義します（通常は `readonly struct`）。
2. ViewModel から `_publisher.Publish(new MyMessage(...))` を呼び出します。
3. partial View クラスに `IMvvmSubscriber<MyMessage>` を実装し、`OnReceivedMessage` でメッセージを処理します。

```csharp
// 1. メッセージを定義する
public readonly struct ShowDialogMessage
{
    public readonly string Text;
    public ShowDialogMessage(string text) => Text = text;
}

// 2. ViewModel からパブリッシュする
[ViewModel]
public partial class MyViewModel
{
    [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
    public void OnConfirm()
    {
        _publisher.Publish(new ShowDialogMessage("よろしいですか？"));
    }
}

// 3. View で受信する（生成コードと同じ partial クラスに記述）
public sealed partial class MyView : IMvvmSubscriber<ShowDialogMessage>
{
    void IMvvmSubscriber<ShowDialogMessage>.OnReceivedMessage(ShowDialogMessage message)
    {
        // message.Text を使ってダイアログを表示する
    }
}
```

`Binder` は、Views リスト内で該当の `IMvvmSubscriber<T>` を実装しているすべての View にメッセージを配信します。

## アナライザー診断

誤った使い方を検出し、修正すべき箇所をコンパイル時に報告します。

| ID | レベル | 条件 |
|----|--------|------|
| `BND001` | エラー | `[ViewModel]` を付与した型の名前に `ViewModel` が含まれていない。名前に `ViewModel` を含める。 |
| `BND002` | エラー | `[Schema]` の `id` に `-1` 未満の値が指定された。 |
| `BND003` | エラー | 複数の `[Schema]` エントリが同一 View フィールドに対して異なるツールチップ値を指定した。 |
| `BND004` | エラー | `[ViewModel]` 型またはその親型のアクセシビリティを生成コードで表現できない。 |
| `BND005` | 警告 | `[ViewModel]` 型のフィールドに `[SerializeField]` または `[SerializeReference]` が付与されている。ViewModel は Unity のデシリアライズに任せず、実行時に生成・代入する。 |
| `BND006` | エラー | `IView` を実装するクラスが `MonoBehaviour` を継承している。View は通常の C# クラスとして実装する。 |
| `BND007` | エラー | `[ViewModel]` 型の `[Schema]` フィールドを直接読み書きしている。生成されるプロパティを使用する。 |

### ベストプラクティス診断

`BNDP` は Bindings の作者が推奨する設計・書き方を示す診断です。既定ではエラーです。プロジェクトの方針に合わせて重大度を変更してください。

| ID | レベル | 推奨事項 |
|----|--------|----------|
| `BNDP001` | エラー | 自分自身への再バインド要求には、等価な `_publisher.PublishRebindMessage<T>()` ではなく、生成された `PublishRebindMessage()` を使用する。 |
| `BNDP002` | エラー | View 内で ViewModel を直接生成せず、View の外で生成して受け渡す。 |
| `BNDP003` | エラー | 子 ViewModel を保持・公開するだけで自身の処理に使わない場合、その所有を親 ViewModel の外へ移す。 |
| `BNDP004` | エラー | 引数を自分自身の公開 Schema プロパティへそのまま代入するだけのメソッドを作らず、プロパティへ直接代入する。 |

### 重大度の設定

`.editorconfig`（推奨）またはコード分析ルールセットファイル（`.ruleset`）で、診断ごとに重大度を設定します。`.ruleset` はコンパイラに明示的に指定する必要があります。例えば `.editorconfig` で `BNDP001` を IDE の提案に留める場合は、次のように指定します。

```ini
[*.cs]
dotnet_diagnostic.BNDP001.severity = suggestion
```

無効にする場合は `none` を指定します。ベストプラクティス診断をまとめて無効にするには、`dotnet_analyzer_diagnostic.category-Bindings.Practices.severity = none` を指定してください。

## 単軸仮想スクロール

`SingleAxisVirtualScrollViewModel` と `SingleAxisVirtualScrollView` を組み合わせて、固定サイズの項目を縦または横方向に並べたリストを表示できます。表示に必要な数の項目 Binder だけを生成・再利用するため、項目数が多いリストに適しています。

- **`SingleAxisVirtualScrollViewModel`**：表示する項目とその順序を受け取り、コードからスクロール位置を指定します。
- **`SingleAxisVirtualScrollView`**：`ScrollRect` と項目プレハブを使い、表示範囲に応じて項目 Binder を再利用します。

この2つは組み込みの型なので、スクロール用の `[ViewModel]` クラスを定義する必要はありません。各項目には、通常の ViewModel と View を使用できます。

VContainer による登録にも対応しています。詳しくは [VContainer サポート：単軸仮想スクロールの登録](#単軸仮想スクロールの登録)を参照してください。

### 1. 項目プレハブとスクロール領域を設定する

1. 項目用の Binder プレハブを作成し、**Views** に項目 ViewModel に対応する View を登録します。プレハブの `RectTransform` には、スクロール軸方向に正の固定サイズを設定してください。
2. シーンの Canvas 配下に、Unity 標準の **UI > Scroll View** でスクロール領域を作成します。ルートの **Scroll View** に `ScrollRect` コンポーネントがあり、その下に **Viewport > Content** があります。
3. **Scroll View** を選択し、`ScrollRect` コンポーネントの **Viewport** に子の **Viewport** の `RectTransform`、**Content** にその下の **Content** の `RectTransform` を設定します。標準メニューから作成した場合は通常、設定済みです。同じ `ScrollRect` の **Vertical** または **Horizontal** のどちらか一方だけを有効にします。
4. リスト全体を管理する外側の Binder の **Views** に `SingleAxisVirtualScrollView` を追加します。
5. Binder の Inspector で、その `SingleAxisVirtualScrollView` の **Item Prefab** に項目用 Binder プレハブを、**Scroll Rect** にシーン内の **Scroll View** の `ScrollRect` コンポーネントを割り当てます。`ScrollRect` を項目プレハブへ追加するわけではありません。
6. 必要に応じて **Spacing** で項目間隔を指定します。Content のサイズと項目の位置は View が設定するため、それらを上書きする `LayoutGroup` や `ContentSizeFitter` との併用は避けてください。

### 2. 項目を作成して ViewModel を関連付ける

表示順に項目を作成し、`SingleAxisVirtualScrollViewModel` に渡します。各項目には専用の Publisher を用意し、その Publisher を渡して生成した項目 ViewModel を `Register` します。外側の Binder を項目 ViewModel に直接渡さないでください。

```csharp
// models is a CounterModel[] in display order; binder is the outer Binder.
var publishers = new ScrollItemMvvmPublisher[models.Length];
for (var i = 0; i < models.Length; i++)
{
    var publisher = ScrollItemMvvmPublisher.Create(out var container);
    container.Register(new CounterViewModel(models[i], publisher));
    publishers[i] = publisher;
}

var scrollViewModel = new SingleAxisVirtualScrollViewModel(publishers, binder);
binder.Initialize(scrollViewModel);
binder.Run();
```

1項目に複数の ViewModel を持たせる場合は、その項目の `container` に対して `Register` を複数回呼び出します。項目 ViewModel を外側の Binder に登録する必要はありません。表示時に項目 Binder へ自動的に割り当てられます。

### 3. コードからスクロールする

`ScrollRatio` に代入すると、指定位置へのスクロールを要求できます。0 が上端／左端、1 が下端／右端です。

```csharp
scrollViewModel.ScrollRatio = 0.5f; // Scroll to the middle.
```

値は 0〜1 に制限され、`NaN` は指定できません。取得時はユーザー操作によるスクロールも含め、View から反映された現在位置を返します。View をバインドする前は、最後に要求した位置を保持します。

### 非表示の項目の扱い

項目が非表示になると項目 Binder は別の項目に再利用されますが、項目 ViewModel は保持されます。非表示中のメッセージは配信されないため、再表示時にバインドで復元したい状態は項目 ViewModel に保存してください。

## VContainer サポート

[VContainer](https://github.com/hadashiA/VContainer) を使用する場合は、`RegisterBinder` で Binder と ViewModel をまとめて登録できます。`Initialize` は自動呼び出しされますが、`Binder.Run()` は利用側で呼ぶか、**Run On Start** を有効にしてください。

`WithViewModel<T>()` による登録では、Bindings 側の Source Generator が生成したコードにより、`VContainer.SourceGenerator.dll` 利用時と同様にコンストラクターを直接呼び出す最適化も行います。VContainer の Source Generator を別途導入する必要はありません。対象条件などは [WithViewModel の生成コードによる最適化](#withviewmodel-の生成コードによる最適化)を参照してください。

### Binder と ViewModel の登録

```csharp
public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private Binder _binder;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(new CounterModel());
        using (var registration = builder.RegisterBinder(_binder))
        {
            registration.WithViewModel<CounterViewModel>().AsSelf();
            // Add more ViewModels to this same registration scope as needed.
        }
    }
}
```

`WithViewModel<T>()` は同じスコープに複数回呼べます。Binder は、そのスコープに追加した ViewModel だけを追加順に受け取り、各 ViewModel には Binder が `IMvvmPublisher` として渡されます。

- 戻り値は VContainer の `RegistrationBuilder` です。`AsSelf`・`As<IViewModel>`・`Keyed`・`WithParameter` などを使用できます。サービスのインターフェイスは自動追加されません。
- ViewModel の Lifetime は既定で `Lifetime.Scoped` です。`WithViewModel<T>(lifetime)` で変更できます。
- **コンテナを Build する前に `using` ブロックを終了**してください。登録はブロック終了時に確定し、例外で抜けた場合もロールバックしません。登録スコープのコピー・ボックス化・`default` での作成・二重破棄は避けてください。
- 複数の Binder を同じコンテナに登録する場合は、`RegisterBinder(binder, keyed: true)` で Binder 自身をキーにできます（VContainer 1.19.0 以降）。

### 単軸仮想スクロールの登録

`WithScrollViewModel<TItem>(getItems, configureItem)` で、`SingleAxisVirtualScrollViewModel` と各項目の ViewModel をまとめて登録できます。Inspector の設定は[単軸仮想スクロール](#単軸仮想スクロール)と同じです。

```csharp
// In LifetimeScope.Configure; _binder is the outer scroll Binder.
builder.RegisterInstance<CounterModel[]>(models);
using (var registration = builder.RegisterBinder(_binder))
{
    registration.WithScrollViewModel<CounterModel>(
        resolver => resolver.Resolve<CounterModel[]>(),
        (itemRegistration, model) =>
        {
            itemRegistration.WithViewModel<CounterViewModel>()
                .WithParameter(model);
        })
        .AsSelf();
}
```

- **`getItems`**：スクロール ViewModel の生成時にコンテナから呼び出され、表示順の `IReadOnlyList<TItem>` を返します。
- **`configureItem`**：各項目の登録を行います。`itemRegistration.WithViewModel<T>()` に `WithParameter` などで項目データを渡します。1項目に複数の ViewModel を登録することもできます。

項目ごとに子コンテナと専用 Publisher が自動作成されるため、`ScrollItemMvvmPublisher.Create` や項目 ViewModel の手動生成は不要です。コールバックへ渡された `itemRegistration` の確定・破棄はライブラリが行うため、利用側で `using` や `Dispose` を追加しないでください。

子コンテナは項目が画面外へ出ても保持され、所有するコンテナの破棄時にまとめて破棄されます。仮想化されるのは項目の表示用 Binder であり、項目 ViewModel の生成ではありません。`AsSelf()` を指定すると、コンテナから `SingleAxisVirtualScrollViewModel` を解決して `ScrollRatio` を操作できます。

### WithViewModel の生成コードによる最適化

Bindings の Source Generator は、ViewModel のコンストラクターを直接呼び出すコードを生成します。`WithViewModel<T>()` による登録ではこの生成コードを使用し、`VContainer.SourceGenerator.dll` 利用時と同様に、コンストラクターをリフレクションで呼び出す処理を省きます。VContainer の Source Generator 自体は不要です。

この最適化は **`WithViewModel<T>()` による登録に限って適用**されます。VContainer の通常の `Register<T>()` などには自動適用されません。

対象は `[ViewModel]` 付きのトップレベルの非ジェネリックな具象クラスで、独自の基底クラス・明示的なコンストラクター・`[Inject]` メンバーを持たない型です。対象外の型は VContainer 標準の生成・インジェクション処理を使用します。

VContainer の Source Generator を併用すると、Bindings の生成コンストラクターが見えず、不正なコンストラクター呼び出しが生成される場合があります。その場合は、ユーザー側の ViewModel 宣言に `[VContainer.InjectIgnore]` を付与してください。

## ライセンス
このライブラリは MIT ライセンスで公開しています。
