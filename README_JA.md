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
            _count += 1;
            PublishRebindMessage();
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Decrement()
        {
            _count -= 1;
            PublishRebindMessage();
        }

        partial void OnPostBind()
        {
            _model.Count = _count;
        }
    }
}
```

ソースジェネレーターにより以下のファイルが自動生成されます。
- `CounterViewModel.g.cs` — プロパティ、コンストラクタ、パブリッシャー配線を含む partial ViewModel クラス。
- `CounterView.g.cs` — シリアライズされた UI コンポーネントフィールドとバインドロジックを含む sealed partial View クラス。

### 生成されるコード

**`CounterViewModel.g.cs`**

```csharp
#nullable enable

namespace MyApp
{
    [global::System.Serializable]
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
        }

        public void NotifyCompletedBind() => OnPostBind();

        partial void OnPostBind();

        [global::System.Runtime.CompilerServices.MethodImpl(
            global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void PublishRebindMessage()
        {
            _publisher.PublishRebindMessage<CounterViewModel>();
        }
    }
}
```

**`CounterView.g.cs`**

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
        private global::TMPro.TMP_Text _text = null!;

        [global::UnityEngine.SerializeField]
        private global::UnityEngine.UI.Button _button1 = null!;

        [global::UnityEngine.SerializeField]
        private global::UnityEngine.UI.Button _button2 = null!;

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
            global::Bindings.TextMeshProExtensions.SetValue(_text, _viewModel.Count);
            _button1.onClick.RemoveAllListeners();
            _button1.onClick.AddListener(_viewModel.Increment);
            _button2.onClick.RemoveAllListeners();
            _button2.onClick.AddListener(_viewModel.Decrement);
            OnPostBind();
            _viewModel.NotifyCompletedBind();
        }

        partial void OnPostBind();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD || !DISABLE_DEBUGTOOLKIT
    public sealed partial class CounterView : global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>
    {
        void global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>.OnReceivedMessage(
            global::Bindings.DebugBindMessage message)
        {
            message.BindTo(this);
            global::Bindings.TextMeshProExtensions.SetValue(_text, _viewModel.Count);
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

**番外編 — Preview**

**Add View** ボタンの下にある **Preview** ボタンをクリックすると、登録されている各 View に紐づく ViewModel オブジェクトが表示されます。Inspector から直接値を入力し、**Invoke** ボタンを押すとバインドが適用されるので、UI が想定通りに更新されるか検証できます。

![Preview と Invoke](https://github.com/user-attachments/assets/2d34fa59-19ec-4e8b-8c82-10608e63b287)

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

## 属性一覧

### `[ViewModel]`

`partial` クラスまたは構造体に付与し、ViewModel としてマークしてソース生成をトリガーします。

| パラメータ | 型 | デフォルト | 説明 |
|-----------|-----|-----------|------|
| `requireBindImplementation` | `bool` | `false` | `true` の場合、`BindAsync` の自動生成をスキップします。ユーザーが partial View クラスに手動で実装する必要があります。 |

### `[Required]`

フィールドまたはプロパティに付与し、生成されるコンストラクタの引数として追加します。生成されるコンストラクタはそのフィールドの型の引数を受け取り、代入します。

### `[Schema(bindingPath, id, format, tooltip)]`

フィールドまたはメソッドに付与し、UI コンポーネントにバインドします。同一メンバに複数回付与できます。

| パラメータ | 型 | デフォルト | 説明 |
|-----------|-----|-----------|------|
| `bindingPath` | `string` | 必須 | バインド先の UI コンポーネントメンバを `PathResolver` 経由で指定します（例: `PathResolver.TMPro.TMP_Text.text`）。 |
| `id` | `int` | `-1` | 複数の `[Schema]` を同一 View コンポーネントフィールドにグループ化します。`id >= 0` で明示的にグループ化し、`-1` は自動採番です。`-1` 未満は `BND002` を発生させます。 |
| `format` | `string` | `""` | `TMPro.TMP_Text.text` バインド時に適用する書式文字列（例: `"N0"`）。それ以外のバインディングパスでは無視されます。 |
| `tooltip` | `string` | `""` | 生成された View コンポーネントフィールドに表示する Unity Inspector のツールチップテキスト。 |

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

| ID | レベル | 条件 |
|----|--------|------|
| `BND001` | エラー | `[ViewModel]` を付与したクラス名に `"ViewModel"` が含まれていない。 |
| `BND002` | エラー | `[Schema]` の `id` に `-1` 未満の値が指定された。 |
| `BND003` | 警告 | 複数の `[Schema]` エントリが同一 View フィールドに対して異なるツールチップ値を指定した。 |

## ライセンス
このライブラリは MIT ライセンスで公開しています。
