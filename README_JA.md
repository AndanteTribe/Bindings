# Bindings
[![dotnet-test](https://github.com/AndanteTribe/Bindings/actions/workflows/dotnet-test.yml/badge.svg)](https://github.com/AndanteTribe/Bindings/actions/workflows/dotnet-test.yml)
[![Releases](https://img.shields.io/github/release/AndanteTribe/Bindings.svg)](https://github.com/AndanteTribe/Bindings/releases)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/Bindings.svg)](./LICENSE)

[English](README.md) | 日本語

## 概要
**Bindings** は、uGUI をベースにした Unity 向け MVVM 自動 UI バインディングフレームワークです。

Roslyn ソースジェネレーターを利用し、アノテーションを付与した C# コードから ViewModel と View の partial クラスを自動生成することで、ボイラープレートを排除し、UI とデータモデルを常に同期させます。

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

`partial` クラスに `[ViewModel]` を付与します。`[Model]` で依存関係を注入し、`[Schema]` で UI バインドを宣言します。

```csharp
using Bindings;
using UnityEngine;

namespace MyApp
{
    [ViewModel]
    public partial class CounterViewModel
    {
        [Model]
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

## 属性一覧

| 属性 | 対象 | 説明 |
|------|------|------|
| `[ViewModel]` | クラス / 構造体 | ViewModel としてマークし、ソース生成をトリガーします。 |
| `[ViewModel(requireBindImplementation: true)]` | クラス / 構造体 | `BindAsync` の自動生成をスキップし、ユーザーが手動で実装します。 |
| `[Model]` | フィールド | コンストラクタ注入するモデル依存関係を宣言します。 |
| `[Schema(bindingPath)]` | フィールド / メソッド | フィールドまたはメソッドを UI コンポーネントのメンバにバインドします。 |
| `[Schema(bindingPath, id: N)]` | フィールド / メソッド | 複数のスキーマを同一 UI コンポーネントにグループ化します（id ≥ 0）。 |
| `[Schema(bindingPath, format: "N0")]` | フィールド | `TMPro.TMP_Text.text` バインド時に書式文字列を適用します。 |
| `[Schema(bindingPath, tooltip: "text")]` | フィールド / メソッド | 生成された View フィールドに Unity Inspector のツールチップを設定します。 |

## アナライザー診断

| ID | レベル | 条件 |
|----|--------|------|
| `BND001` | エラー | `[ViewModel]` を付与したクラス名に `"ViewModel"` が含まれていない。 |
| `BND002` | エラー | `[Schema]` の `id` に `-1` 未満の値が指定された。 |
| `BND003` | 警告 | 複数の `[Schema]` エントリが同一 View フィールドに対して異なるツールチップ値を指定した。 |

## ライセンス
このライブラリは MIT ライセンスで公開しています。
