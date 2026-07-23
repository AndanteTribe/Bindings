# Source Generator 診断

診断を無効化せず、宣言または設計を修正する。生成コードが見つからない場合は、まず Unity Console と IDE の `BND` 診断を確認する。

| ID | レベル | 条件 | 修正 | 生成動作 |
| --- | --- | --- | --- | --- |
| `BND001` | Error | `[ViewModel]` 型名に `ViewModel` が含まれない。 | ViewModel なら `SomethingViewModel` へ改名し、データ型などへ誤って付けた場合は `[ViewModel]` を外す。 | 対象 ViewModel の補助ソースと View の両方を生成しない。他の ViewModel の生成は継続する。 |
| `BND002` | Error | `[Schema]` の `id` が `-1` 未満。 | `id` を省略するか、`0` 以上へ変更する。 | 不正 id を未指定の `-1` として扱い、生成を継続する。 |
| `BND003` | Error | 同じ View フィールドを共有する Schema に異なる非空 `tooltip` がある。 | 同じ `id` グループの tooltip を同一文字列へ統一するか、1か所だけに指定する。 | 最初の非空 tooltip を使い、生成を継続する。 |
| `BND004` | Error | ViewModel または親型のアクセシビリティを生成コードで表せない。 | 型・親型を通常の C# アクセシビリティへ直し、すべての親型を `partial` にする。 | 対象 ViewModel と View の両方を生成しない。他の ViewModel の生成は継続する。 |
| `BND005` | Warning | `[ViewModel]` 型のフィールドに Unity の `[SerializeField]` または `[SerializeReference]` が付いている。 | Unity のシリアライズ対象から外し、ViewModel をコードまたは DI で実行時に構築・代入する。 | 対象 ViewModel と View の生成を継続する。 |

## BND005 とシリアライズ

`BND005` は対象の `[SerializeField]` または `[SerializeReference]` の位置に報告される。両方を同じ ViewModel 型フィールドへ付けた場合は、それぞれが診断対象になる。

ViewModel 型に `[System.Serializable]` が付いていても、Unity のデシリアライズ後に `_publisher` などの生成されたランタイム状態が正しく初期化される保証はない。`[SerializeReference]` への置き換えを回避策にしない。

ユーザーが ViewModel 型そのものへ `[System.Serializable]` を付けただけでは `BND005` は報告されない。これは Unity のフィールドシリアライズを使わない独自シリアライザーなどを許容するためであり、Bindings が Unity による ViewModel の永続化をサポートすることを意味しない。Generator 自身が付ける `[System.Serializable]` は `UNITY_EDITOR` の場合だけ有効になる。

## 診断がないのにコンパイルできない場合

次を順に確認する。

1. `[ViewModel]` 型とネスト元の型がすべて `partial` か確認する。
2. `[Schema]` フィールドから生成されるプロパティ名が既存メンバーと衝突していないか確認する。
3. `[Required]` プロパティが生成コンストラクタから代入可能か確認する。
4. `bindingPath` のコンポーネント型とメンバーが、割り当てる ViewModel 値の型に対応するか確認する。
5. `[Schema]` メソッドのシグネチャが UnityEvent の `AddListener` に渡せるか確認する。
6. `[ViewModel(requireBindImplementation: true)]` の場合、View partial 型に `IView.BindAsync(CancellationToken)` が実装されているか確認する。
7. VContainer API を使う場合、VContainer パッケージがインストールされ `ENABLE_VCONTAINER` が有効か確認する。
8. Unity の再コンパイル後、生成された `{ViewModel}.g.cs` と `{View}.g.cs` を読み、エラー位置の生成パターンを確認する。生成ファイル自体は編集しない。

## 実行時エラー

`Binder.Initialize` が `No view found for view model of type ...` を送出する場合は、次を確認する。

- Binder Inspector の Views に対応する生成 View を追加したか。
- 初期化した ViewModel の具体型と生成 View の型が一致するか。
- ViewModel の namespace または型名変更後に、古い View を Inspector に保持していないか。

UI が更新されない場合は、次を確認する。

- 初期化後に `Binder.Run()` を呼んだか、**Run On Start** を有効にしたか。
- 外部コードから生成プロパティを更新したか。
- バッキングフィールドを直接更新した場合に `PublishRebindMessage()` を呼んだか。
- Schema の `id` により、意図した UI コンポーネントフィールドを共有できているか。
- Canvas 描画直前まで再バインドが遅延することを考慮しているか。
