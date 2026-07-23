# ViewModel とバインディング宣言

## 目次

- [最小構成](#最小構成)
- [属性](#属性)
- [生成 ViewModel API](#生成-viewmodel-api)
- [Schema の引数](#schema-の引数)
- [バインディングパスと生成処理](#バインディングパスと生成処理)
- [イベントバインディング](#イベントバインディング)
- [バインド完了フック](#バインド完了フック)
- [手動 BindAsync](#手動-bindasync)
- [型と命名の制約](#型と命名の制約)

## 最小構成

```csharp
using Bindings;

namespace MyGame.UI
{
    [ViewModel]
    public partial class CounterViewModel
    {
        [Required]
        private readonly CounterModel _model;

        [Schema(PathResolver.TMPro.TMP_Text.text, format: "N0")]
        private int _count;

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Increment()
        {
            _count++;
            PublishRebindMessage();
        }

        partial void OnPostBind()
        {
            _model.Count = _count;
        }
    }
}
```

Generator は次を作成する。

- `CounterViewModel.g.cs`: `IViewModel` 実装、`Count` プロパティ、コンストラクタ、再バインド用ヘルパー。
- `CounterView.g.cs`: `IView<CounterViewModel>` 実装、UI コンポーネントのシリアライズフィールド、バインド処理。

生成された `*.g.cs` は編集しない。カスタマイズは同じ名前空間・同じ型名の partial 宣言へ記述する。

## 属性

| 属性 | 対象 | 動作 |
| --- | --- | --- |
| `[ViewModel]` | `partial class` または `partial struct` | ViewModel と View の生成を開始する。 |
| `[ViewModel(requireBindImplementation: true)]` | 同上 | View 側の `IView.BindAsync` の自動生成だけを止める。 |
| `[Required]` | フィールドまたはプロパティ | 生成コンストラクタの引数と代入へ追加する。 |
| `[Schema(...)]` | フィールド | 公開プロパティと UI への値バインドを生成する。 |
| `[Schema(...)]` | メソッド | UnityEvent の listener 登録を生成する。 |

`[Schema]` は同じフィールドまたはメソッドへ複数回指定できる。同じフィールドに複数指定しても生成 ViewModel プロパティは1つで、View のバインディングはすべて生成される。

## 生成 ViewModel API

| 生成メンバー | 動作 |
| --- | --- |
| `{Property}` | `[Schema]` フィールド名から `_` と `m_` を除き、先頭を大文字化する。通常型の setter は `PublishRebindMessage()` を呼ぶ。 |
| `{ViewModel}(..., IMvvmPublisher publisher)` | `[Required]` メンバーを宣言順に受け取り、最後に publisher を受け取る。 |
| `NotifyCompletedBind()` | View のバインド完了後に ViewModel 側の `OnPostBind()` を呼ぶ。 |
| `partial void OnPostBind()` | モデルへの書き戻しなど、View の反映完了後の処理を実装する。 |
| `PublishRebindMessage()` | 対応する View の再バインドを要求する private ヘルパー。partial 型内から呼べる。 |
| `_publisher` | 生成される private `IMvvmPublisher`。partial 型内から任意メッセージの `Publish` に使用できる。 |

`readonly struct` の ViewModel では、`[Schema]` フィールドの生成プロパティは getter のみになる。

ViewModel のメソッド内でも生成プロパティを使用できる。複数のバッキングフィールドをまとめて変更して再バインド要求を1回に抑えたい場合は、フィールドを直接変更した後に `PublishRebindMessage()` を1回呼ぶ。

Generator が ViewModel に付ける `[System.Serializable]` は `UNITY_EDITOR` の場合だけ有効になる。ユーザーが明示的に `[System.Serializable]` を付けた場合、Generator はこの条件付き属性を重複生成しない。明示的な属性は独自シリアライザーには利用できるが、Unity の `[SerializeField]` または `[SerializeReference]` による ViewModel の永続化を Bindings がサポートすることを意味しない。

## Schema の引数

| 引数 | 既定値 | 動作 |
| --- | --- | --- |
| `bindingPath` | 必須 | `PathResolver.<namespace>.<component>.<member>` を指定する。文字列でも指定できるが、型安全な `PathResolver` を優先する。 |
| `id` | `-1` | `0` 以上で同じ型・同じ id の Schema を同一 View コンポーネントへまとめる。`-1` は各 Schema を独立させる。 |
| `format` | `""` | `TMPro.TMP_Text.text` へ値を設定するときの書式文字列。 |
| `tooltip` | `""` | 生成 View フィールドへ Unity の `[Tooltip]` を付ける。 |

同じコンポーネントにプロパティとイベントを割り当てる例:

```csharp
[Schema(PathResolver.UnityEngine.UI.Button.interactable, id: 1, tooltip: "Submit")]
private bool _canSubmit;

[Schema(PathResolver.UnityEngine.UI.Button.onClick, id: 1, tooltip: "Submit")]
public void Submit()
{
    // Submit the current state.
}
```

両方が生成 View の `_button1` を共有する。`id` を省略すると別の Button フィールドになる。明示 id と省略 id が混在する場合、省略 id には1から未使用の正整数が順に割り当てられる。

## バインディングパスと生成処理

パスは基本的に最後の `.` でコンポーネント型とメンバーへ分割される。例えば `UnityEngine.UI.Toggle.interactable` は `UnityEngine.UI.Toggle` のフィールドを生成し、その `interactable` へ代入する。

次のパスには専用処理がある。

| パス | 生成処理 |
| --- | --- |
| `TMPro.TMP_Text.text` | `TextMeshProExtensions.SetValue` を使用し、割り当てを抑えて書式化する。 |
| `UnityEngine.GameObject.activeSelf` | 読み取り専用プロパティへの代入ではなく `GameObject.SetActive(bool)` を呼ぶ。 |
| `UnityEngine.RectTransform.rect.size` | X/Y それぞれに `SetSizeWithCurrentAnchors` を呼び、現在のアンカーを維持する。 |
| その他 | 生成 View フィールドの対象メンバーへ生成プロパティ値を直接代入する。 |

`UnityEngine.RectTransform.sizeDelta` は専用処理ではなく直接代入になる。

## イベントバインディング

メソッドの `[Schema]` は対象 UnityEvent に listener を登録する。メソッドのシグネチャは UnityEvent の `AddListener` に渡せる形にする。同一コンポーネント・同一イベントへ複数メソッドを結び付ける場合、生成コードは `RemoveAllListeners()` を1回呼んだ後、宣言順にすべて追加する。

生成コードが `RemoveAllListeners()` を使うため、同じ UnityEvent に Inspector や別コードから登録した listener を維持する必要がある場合は競合を避ける設計にする。

## バインド完了フック

- ViewModel の `partial void OnPostBind()` は View の反映後に呼ばれる。モデル同期などに使用する。
- View の `partial void OnPostBind()` は生成 View と同じ partial 型に実装し、UI 固有の後処理に使用する。
- デフォルトの処理順は、値の反映、イベント登録、View の `OnPostBind()`、ViewModel の `NotifyCompletedBind()`。

## 手動 BindAsync

アニメーションなどを含む独自の非同期バインドが必要な場合だけ `[ViewModel(requireBindImplementation: true)]` を使用し、生成 View と同じ partial 型で `IView.BindAsync(CancellationToken)` を実装する。生成される `BindAll()` は同じ partial 型から呼び出せる。

```csharp
using System.Threading;
using System.Threading.Tasks;
using Bindings;

namespace MyGame.UI
{
    public sealed partial class CounterView
    {
        ValueTask IView.BindAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BindAll();
            return default;
        }
    }
}
```

## 型と命名の制約

- 型名に大文字小文字を区別して `ViewModel` を含める。
- ネストした ViewModel の親型もすべて `partial` にする。
- ViewModel と親型のアクセシビリティは生成側で再現できるものにする。
- `[Schema]` フィールド名は `_count`、`m_count`、`count` などを使用できる。正規化後が空になる名前は避ける。
- 生成 View は ViewModel と同じ名前空間へ作られる。
