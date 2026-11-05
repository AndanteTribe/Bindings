# 開発エージェント向けガイド

## 基本方針

- この `AGENTS.md`、`README_JA.md`、`docs/diagnostics/*.ja.md` は日本語で記述する。それ以外のドキュメント、コード内コメント、XML ドキュメントコメントなどは基本的に英語で記述する。診断メッセージは英語を既定とし、日本語リソースも管理する。
- 作業開始前に `git status --short` を確認し、既存の変更や未追跡ファイルを勝手に変更・削除しない。
- 命名規則、インデント、改行、`using` の配置などは、リポジトリ直下の `.editorconfig` に従う。Unity 配下では `src/Bindings.Unity/.editorconfig` の追加設定も適用される。

## プロジェクト構成

このリポジトリには、主に .NET の Source Generator プロジェクトと Unity プロジェクトがある。

- `Bindings.slnx`
  - .NET 側のソリューション。
  - `src/Bindings.SourceGenerator`: Source Generator 本体（`netstandard2.0`）。
  - `src/Bindings.Tests`: Source Generator の xUnit テスト（`net8.0`）。
- `src/Bindings.Unity`
  - Unity プロジェクト。使用する Unity バージョンは `ProjectSettings/ProjectVersion.txt` を確認する。
  - 配布対象の UPM パッケージ本体は `Packages/jp.andantetribe.bindings` にある。
  - Unity が生成する `.csproj` と `.sln` は `.gitignore` の対象であり、原則として手動編集・コミットしない。

## T4 テンプレートの更新

`src/Bindings.SourceGenerator/GeneratorCore` には、次の T4 テンプレートと生成済み C# ファイルがある。

- `ViewModelTemplate.tt` → `ViewModelTemplate.cs`
- `ViewTemplate.tt` → `ViewTemplate.cs`

生成済みの `.cs` はリポジトリで管理されている。テンプレートのロジックを変更するときは、生成済み `.cs` だけを直接編集せず、対応する `.tt` を変更してから明示的に再生成すること。`Bindings.SourceGenerator.csproj` には `TransformOnBuild` の設定もあるが、環境差を避けるため、`.github/workflows/release.yml` と同じ次のコマンドをリポジトリルートから実行する。

リリース CI は .NET SDK `10.0.x` を使用するため、T4 の再生成と Release ビルドも原則として同じ SDK 系で行う。

```powershell
dotnet tool restore
dotnet tool run t4 -- src/Bindings.SourceGenerator/GeneratorCore/ViewModelTemplate.tt -c Bindings.GeneratorCore.ViewModelTemplate -o src/Bindings.SourceGenerator/GeneratorCore/ViewModelTemplate.cs
dotnet tool run t4 -- src/Bindings.SourceGenerator/GeneratorCore/ViewTemplate.tt -c Bindings.GeneratorCore.ViewTemplate -o src/Bindings.SourceGenerator/GeneratorCore/ViewTemplate.cs
```

T4 ツールは `.config/dotnet-tools.json` で固定されている。再生成後は、対応する `.tt` と `.cs` の差分を確認し、意図しない生成差分がないことを確認する。

## 診断ルールの追加・変更

Source Generator の診断を追加または変更するときは、実装だけで完了とせず、次の項目を同じ変更に含める。

- `src/Bindings.SourceGenerator/DiagnosticDescriptors.cs` に英語のタイトルとメッセージを定義し、ID、Category、Severity が既存ルールと整合することを確認する。
- 想定外の入力で例外を投げて Generator 全体を失敗させず、診断を報告する。生成を継続できない場合は、該当する ViewModel の生成だけを安全に中止する。
- `src/Bindings.SourceGenerator/AnalyzerReleases.Shipped.md` の既存リリース表へルールを追加する。リリース追跡警告を無視しない。
- `README.md` と `README_JA.md` の診断一覧へ同じIDと実装どおりのSeverityを追記し、それぞれ英語・日本語の診断ページへリンクする。
- `docs/diagnostics/{ID}.md` と `{ID}.ja.md` を同時に追加・更新する。タイトル・メッセージは対応するリソースに合わせ、カテゴリー・既定のSeverity・既定の有効状態・Code Fixの有無・原因・修正方法・対象範囲と例外・診断後の生成継続または中止を記載する。日英ページを相互リンクし、READMEへ戻る導線を設ける。生成コードの検査対象・対象外は診断ごとに明記する。
- 通常の使い方・生成コードの仕様は両README、個別診断の詳細は診断ページ、開発時の規約はこのAGENTS.mdを更新する。実装手順だけの説明はコード内コメントやテストで管理し、重複した仕様書を追加しない。
- 診断のID、Severity、主要なメッセージ内容、生成継続または中止の挙動を自動テストで検証する。通常のC#コードから到達しない防御分岐も、Roslynのエラーシンボルなどを使って可能な限り検証する。
- `DiagnosticDocumentationTests` は全 `DiagnosticDescriptor` と両README・日英診断ページ・ヘルプURLの整合性を照合する。ドキュメントを更新せずにテスト側の期待値を弱めない。診断ページのファイル名はIDと一致させる。
- Releaseビルドで analyzer release tracking を含む警告が0件であることと、Release構成の全テストが成功することを確認する。

## Roslyn の検査が有効になる実装構造

- `src/Bindings.SourceGenerator/Roslyn.ruleset` で、使用中の `Microsoft.CodeAnalysis.Analyzers` の全 RS ルールを Error にしている。警告抑制や対象外の補助クラスへの移動で回避せず、検査対象になる形で実装する。
- `Initialize` に解析設定とコールバックの登録フローを明記する。登録を別メソッドへ委譲しない。コールバックから呼ぶ判定用の private メソッドは利用してよいが、診断報告は Analyzer 自身の型に置き、報告する `DiagnosticDescriptors` のフィールドを明示する。
- コンパイル・シンボル単位の状態は、それぞれの Start コールバックのローカルに閉じ込める。Analyzer や共有ヘルパーのインスタンスフィールド・static フィールドに Compilation、SemanticModel、シンボル、操作を保持しない。
- 共有の拡張メソッドには解決済みのシンボル・操作・値を渡す。Compilation や解析 Context を渡して情報取得・登録・診断報告を隠さない。Code Fix の Document API 呼び出しと CodeAction 生成は CodeFixProvider 内に置く。
- 診断定義は `DiagnosticDescriptors.cs` のフィールドで直接生成する。不透明なファクトリーを介さず、文言は `DiagnosticResources.resx` の LocalizableResourceString、ヘルプリンク、カテゴリーを示す customTags を指定する。英語の既存文言は意図せず変更しない。現在のMicrosoft.CodeAnalysis.Analyzers 3.3.4では、生成を静的コンストラクターへ移すとリリース追跡が認識できずRS2003になる。
- 診断 ID・カテゴリーを追加する際は `DiagnosticCategoryAndIdRanges.txt` も更新する。リリース表と `.resx` は使用中の公式 Analyzer パッケージが AdditionalFiles に追加するため、csproj で二重登録しない。
- Generatorのsource-outputコールバックが診断報告とAddSourceを所有し、描画用ヘルパーは生成データを受け取ってソースを返す。型参照は名前衝突を避けるため完全修飾名を使用する。
- コールバックやpartialファイルの順序に依存しない実装・テストにする。エラーシンボルや不足した参照でAnalyzer全体を例外停止させない。Code Fixは古くなった診断を再検証し、診断IDをCodeActionのequivalence keyに使用する。

## 診断ヘルプURLとコミットSHA

- `DiagnosticDescriptor.HelpLinkUri` は `https://github.com/AndanteTribe/Bindings/blob/{sha}/docs/diagnostics/{ID}.md` を指す。日本語への切り替えはページ内の相互リンクで行う。
- Analyzer自身の `AssemblyInformationalVersionAttribute` を実行時に読み、SDKが `+` に続けて付与する40桁のGitコミットSHAを使用する。解析対象のCompilationや実行環境のGitを参照しない。SHAがない、または不正な形式の場合は `main` へフォールバックする。
- `IncludeSourceRevisionInInformationalVersion=true` を維持する。ヘルプURLのためのAnalyzerのVersion設定・package.jsonとの同期・独自のURL定数生成は不要。
- ローカルビルドもビルド元のコミットを参照し、未コミットの文書変更はリンク先に含まれない。リリース前に診断ページをmainへコミット・pushする。既存release.ymlはorigin/mainから作成したブランチでビルドするため、そのmainコミットの文書を参照でき、タグ公開を待つ必要はない。

## Source Generator の Release ビルドと Unity への DLL 配置

Source Generator は次のコマンドで Release ビルドする。

```powershell
dotnet build src/Bindings.SourceGenerator/Bindings.SourceGenerator.csproj --configuration Release
```

URLを変更した場合はSHAを含む通常ビルドと、SHAなしのビルドの両方で、アセンブリ属性と診断ヘルプURLを検証する。

`src/Bindings.SourceGenerator/Directory.Build.targets` の `DllCopy` ターゲットにより、ビルド後に次の DLL だけが Unity パッケージへ自動コピーされる。

- コピー元: `src/Bindings.SourceGenerator/bin/Release/netstandard2.0/Bindings.SourceGenerator.dll`
- コピー先: `src/Bindings.Unity/Packages/jp.andantetribe.bindings/Runtime/Bindings.SourceGenerator.dll`

日本語診断用のサテライト DLL も、`src/Bindings.SourceGenerator/bin/Release/netstandard2.0/ja/Bindings.SourceGenerator.resources.dll` から `src/Bindings.Unity/Packages/jp.andantetribe.bindings/Runtime/ja/Bindings.SourceGenerator.resources.dll` へコピーする。日本語リソースは `DiagnosticResources.ja.resx` に定義し、英語リソースとキー・書式引数を揃える。

PDB や依存 DLL はこのターゲットのコピー対象ではない。Source Generator を変更した場合は、T4 の再生成後に Release ビルドを実行し、Unity 側の DLL が更新されたかを `git diff` または `git status` で確認する。リリース時の一連の処理については `.github/workflows/release.yml` を正とする。

## テストと検証

- Source Generator の変更後は、少なくとも次を実行する。

```powershell
dotnet build Bindings.slnx --configuration Release
dotnet test src/Bindings.Tests/Bindings.Tests.csproj --configuration Release
```

- 命名・生成コード・診断の仕様を変更した場合は、テスト内の入力だけでなく、Unityの`Assets`・パッケージの`Samples~`、手書きpartial View、保存データ、文書内の参照も確認する。Source Generator単体のビルド成功をソリューション全体の検証の代わりにしない。
- 新機能、分岐、例外処理を追加した場合は、対応する自動テストも追加する。
- Source Generator のテストカバレッジは現在 90% 以上である。正常系だけでなく、境界値、分岐、異常系、診断出力も網羅するテストを追加し、変更後も全体のカバレッジを 90% 未満へ低下させない。
- Unity の `Assets` または `Packages` 配下でファイルやディレクトリを追加・移動・削除する場合は、対応する `.meta` ファイルも同時に管理する。CI では `.github/workflows/unity-meta-check.yml` により整合性が検査される。
- Unity の動作確認が必要な変更は、対象 Unity バージョンでプロジェクトを開き、コンパイルエラーがないことと関連機能の動作を確認する。

## 作業完了時のフォーマット

C# コードの変更と T4 の再生成が終わったら、作業完了前に Source Generator 側と Unity 側の両方へ `dotnet format` を実行する。

```powershell
dotnet format Bindings.slnx
dotnet format src/Bindings.Unity/Bindings.Unity.sln
```

`src/Bindings.Unity/Bindings.Unity.sln` は Unity が生成するため、存在しない場合は対象 Unity プロジェクトを開いて C# プロジェクトファイルを生成してから実行する。フォーマット後は差分を確認し、今回の作業と無関係なファイルが変更されていないことを確認する。フォーマットでコードが変わった場合は、関連するビルドとテストを再実行する。
