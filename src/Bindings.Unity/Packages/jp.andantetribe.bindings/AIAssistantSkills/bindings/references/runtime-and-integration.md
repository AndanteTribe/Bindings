# ランタイム API と統合

## 目次

- [Binder のセットアップ](#binder-のセットアップ)
- [Binder API](#binder-api)
- [View API](#view-api)
- [ViewModel から View へのメッセージ](#viewmodel-から-view-へのメッセージ)
- [VContainer](#vcontainer)
- [TextMeshProExtensions](#textmeshproextensions)
- [Editor Preview と DebugBindMessage](#editor-preview-と-debugbindmessage)

## Binder のセットアップ

1. シーンの GameObject に `Binder` を追加する。
2. Inspector の **Add View** から生成 View を追加する。
3. 生成 View に表示される UI コンポーネントフィールドを割り当てる。
4. ViewModel を生成コンストラクタで作成し、`binder.Initialize(viewModel)` を呼ぶ。
5. `binder.Run()` を呼ぶか、Inspector の **Run On Start** を有効にする。

```csharp
using Bindings;
using UnityEngine;

public sealed class GameEntryPoint : MonoBehaviour
{
    [SerializeField]
    private Binder _binder = null!;

    private void Start()
    {
        var model = new CounterModel();
        var viewModel = new CounterViewModel(model, _binder);
        _binder.Initialize(viewModel);
        _binder.Run();
    }
}
```

`Binder` は `IMvvmPublisher` を実装する。`Initialize` は ViewModel と互換性のある View が見つからない場合に `InvalidOperationException` を送出する。

## Binder API

| メンバー | 動作 |
| --- | --- |
| `Initialize(IViewModel viewModel)` | 互換性のある登録済み View を初期化する。 |
| `Initialize(IReadOnlyList<IViewModel> viewModels)` | 複数 ViewModel を順に初期化する。VContainer からも注入される。 |
| `Run()` | 以前の実行をキャンセルし、登録済み View の `BindAsync` を開始する。 |
| `IMvvmPublisher.PublishRebindMessage<T>()` | 対応 View の再バインドを要求する。生成プロパティ setter から使用される。 |
| `IMvvmPublisher.Publish<T>(T message)` | 登録済み View の同期・非同期 subscriber へメッセージを配信する。 |

再バインド要求は内部キューへ入り、`Canvas.preWillRenderCanvases` で実行される。同一フレーム内の連続した値変更に対して、不要な即時 UI 更新を避けられる。即時反映を前提に後続処理を書かない。

## View API

| 型 | メンバー | 用途 |
| --- | --- | --- |
| `IView` | `ValueTask BindAsync(CancellationToken)` | ViewModel の値を UI へ反映する。 |
| `IView` | `bool CanBind(IViewModel)` | ViewModel との互換性を判定する。 |
| `IView` | `void Initialize(IViewModel)` | ViewModel を割り当てる。 |
| `IView<T>` | `void Initialize(T)` | 型付き ViewModel を割り当てる。`CanBind` と非型付き `Initialize` の既定実装も提供する。 |
| `IViewModel` | メンバーなし | Binder が扱う ViewModel のマーカー。生成 ViewModel が実装する。 |

通常はこれらを直接実装せず、`[ViewModel]` から生成される View を使用する。既存 MonoBehaviour を View にしたい場合や、生成バインディングで表現できない UI 更新が必要な場合にだけ独自実装を検討する。

## ViewModel から View へのメッセージ

データ再バインドでは表現しにくいダイアログ、アニメーション、効果音などは `IMvvmPublisher.Publish` と subscriber を使用する。

```csharp
public readonly struct ShowDialogMessage
{
    public ShowDialogMessage(string text) => Text = text;

    public string Text { get; }
}

[ViewModel]
public partial class DialogViewModel
{
    [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
    public void Confirm()
    {
        _publisher.Publish(new ShowDialogMessage("続行しますか？"));
    }
}

public sealed partial class DialogView : IMvvmSubscriber<ShowDialogMessage>
{
    void IMvvmSubscriber<ShowDialogMessage>.OnReceivedMessage(ShowDialogMessage message)
    {
        // Show the dialog with message.Text.
    }
}
```

非同期処理には `IAsyncMvvmSubscriber<T>` を使用する。

```csharp
public sealed partial class DialogView : IAsyncMvvmSubscriber<PlayAnimationMessage>
{
    async ValueTask IAsyncMvvmSubscriber<PlayAnimationMessage>.OnReceivedMessageAsync(
        PlayAnimationMessage message,
        CancellationToken cancellationToken)
    {
        await PlayAnimationAsync(message, cancellationToken);
    }
}
```

非同期 subscriber へ渡される token は `Binder` の GameObject の破棄に連動する。キャンセルを伝播し、破棄後に Unity オブジェクトへアクセスしない。

## VContainer

VContainer パッケージが存在すると `ENABLE_VCONTAINER` が自動定義され、`RegisterViewModel<T>` が有効になる。

```csharp
using Bindings;
using VContainer;
using VContainer.Unity;

public sealed class GameLifetimeScope : LifetimeScope
{
    [SerializeField]
    private Binder _binder = null!;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(new CounterModel());
        builder.RegisterViewModel<CounterViewModel>(_binder);
        builder.RegisterComponent(_binder);
    }
}
```

`RegisterViewModel<T>(binder, lifetime)` の既定 lifetime は `Lifetime.Scoped` で、`T` を `IViewModel` として登録し、生成コンストラクタの `IMvvmPublisher` に Binder を渡す。Binder の `Initialize(IReadOnlyList<IViewModel>)` は `[Inject]` 対象になるため、手動 `Initialize` は不要になる。

## TextMeshProExtensions

`TMPro.TMP_Text.text` の生成バインディングは `TextMeshProExtensions.SetValue` を呼ぶ。文字列、`bool`、`char`、整数型、浮動小数点型、`decimal`、`DateTime`、`DateTimeOffset`、`Guid`、`TimeSpan` と書式文字列を扱う。

通常は生成コード経由で使用する。手動で使用する場合も `TMP_Text.text = value.ToString()` より `SetValue` を優先すると、一時文字列や配列の割り当てを抑えられる。対応しない型は文字列へ明示変換するか、適切な表示用プロパティを ViewModel に用意する。

## Editor Preview と DebugBindMessage

Binder Inspector の **Preview** では一時 ViewModel を表示し、**Invoke** で値バインドだけを確認できる。生成 View は Editor、Development Build、または `DISABLE_DEBUGTOOLKIT` が未定義の環境で `DebugBindMessage` を購読する。

Preview の再バインドでは値のみを反映し、UnityEvent の listener は再登録しない。`DISABLE_DEBUGTOOLKIT` を定義したビルドでは、このデバッグ用 subscriber に依存しない。
