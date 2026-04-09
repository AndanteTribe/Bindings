# Bindings
[![dotnet-test](https://github.com/AndanteTribe/Bindings/actions/workflows/dotnet-test.yml/badge.svg)](https://github.com/AndanteTribe/Bindings/actions/workflows/dotnet-test.yml)
[![Releases](https://img.shields.io/github/release/AndanteTribe/Bindings.svg)](https://github.com/AndanteTribe/Bindings/releases)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/Bindings.svg)](./LICENSE)

English | [日本語](README_JA.md)

## Overview
**Bindings** is an MVVM-based automated UI binding framework for Unity built on uGUI.

It provides a Roslyn Source Generator that automatically generates ViewModel and View partial classes from annotated C# code, eliminating boilerplate and keeping UI bindings in sync with your data models.

## Requirements
- Unity 6000.0 or later
- Unity uGUI 2.0.0 or later

## Installation
Open `Window > Package Manager`, select `[+] > Add package from git URL`, and enter the following URL:

```
https://github.com/AndanteTribe/Bindings.git?path=src/Bindings.Unity/Packages/jp.andantetribe.bindings
```

## Quick Start

### 1. Define a ViewModel

Annotate a `partial` class with `[ViewModel]`. Use `[Model]` to inject dependencies and `[Schema]` to declare UI bindings.

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

The source generator automatically produces:
- `CounterViewModel.g.cs` — a partial ViewModel class with properties, constructor, and publisher wiring.
- `CounterView.g.cs` — a sealed partial View class with serialized UI component fields and binding logic.

### 2. Set up the Binder in the Scene

1. Add a `Binder` component to a GameObject in your scene.
2. In the Inspector, assign the generated `CounterView` instance to the **Views** list.
3. Call `binder.Initialize(viewModel)` to associate the ViewModel, then call `binder.Run()` (or enable **Run On Start**) to bind the UI.

```csharp
public class GameEntry : MonoBehaviour
{
    [SerializeField] private Binder _binder;

    private void Start()
    {
        var model = new CounterModel();
        var publisher = _binder; // Binder implements IMvvmPublisher
        var viewModel = new CounterViewModel(model, publisher);
        _binder.Initialize(viewModel);
    }
}
```

## Attributes

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[ViewModel]` | Class / Struct | Marks the type as a ViewModel and triggers source generation. |
| `[ViewModel(requireBindImplementation: true)]` | Class / Struct | Skips auto-generating `BindAsync`; the user must implement it manually. |
| `[Model]` | Field | Declares a constructor-injected model dependency. |
| `[Schema(bindingPath)]` | Field / Method | Binds the field or method to a UI component member. |
| `[Schema(bindingPath, id: N)]` | Field / Method | Groups multiple schemas onto the same UI component (id ≥ 0). |
| `[Schema(bindingPath, format: "N0")]` | Field | Applies a format string when binding to `TMPro.TMP_Text.text`. |
| `[Schema(bindingPath, tooltip: "text")]` | Field / Method | Sets the Unity Inspector tooltip on the generated View field. |

## Analyzer Diagnostics

| ID | Level | Condition |
|----|-------|-----------|
| `BND001` | Error | `[ViewModel]` class name does not contain `"ViewModel"`. |
| `BND002` | Error | `[Schema]` `id` is less than `-1`. |
| `BND003` | Warning | Multiple `[Schema]` entries assign conflicting tooltip values to the same View field. |

## License
This library is released under the MIT license.
