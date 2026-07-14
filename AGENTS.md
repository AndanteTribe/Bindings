# 開発エージェント向けガイド

## 基本方針

- この `AGENTS.md` は日本語で記述する。リポジトリ内では `README_JA.md` も日本語とし、それ以外のドキュメント、コード内コメント、XML ドキュメントコメント、診断メッセージなどは基本的に英語で記述する。
- 作業開始前に `git status --short` を確認し、既存の変更や未追跡ファイルを勝手に変更・削除しない。
- 命名規則、インデント、改行、`using` の配置などは、リポジトリ直下の `.editorconfig` に従う。Unity 配下では `src/Bindings.Unity/.editorconfig` の追加設定も適用される。

## プロジェクト構成

このリポジトリには、主に .NET の Source Generator プロジェクトと Unity プロジェクトがある。

- `Bindings.slnx`
  - .NET 側のソリューション。
  - `src/Bindings.SourceGenerator`: Source Generator 本体（`netstandard2.0`）。
  - `src/Bindings.Tests`: Source Generator の xUnit テスト（`net8.0`）。
  - `src/Bindings.Sample`: Source Generator の利用サンプル（`net8.0`）。
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
- `README.md` と `README_JA.md` の診断一覧へ同じIDと実装どおりのSeverityを追記する。
- `docs/ViewModelGenerator-Spec.md` の診断条件、Severity、診断後に生成を継続するか中止するかを更新する。診断に関連する生成仕様が変わる場合は、その節も更新する。
- 診断のID、Severity、主要なメッセージ内容、生成継続または中止の挙動を自動テストで検証する。通常のC#コードから到達しない防御分岐も、Roslynのエラーシンボルなどを使って可能な限り検証する。
- `DiagnosticDocumentationTests` が全 `DiagnosticDescriptor` と英語・日本語READMEの記載を照合するため、READMEを更新せずにテスト側の期待値を弱めない。
- Releaseビルドで analyzer release tracking を含む警告が0件であることと、Release構成の全テストが成功することを確認する。

## Source Generator の Release ビルドと Unity への DLL 配置

Source Generator は次のコマンドで Release ビルドする。

```powershell
dotnet build src/Bindings.SourceGenerator/Bindings.SourceGenerator.csproj --configuration Release
```

`src/Bindings.SourceGenerator/Directory.Build.targets` の `DllCopy` ターゲットにより、ビルド後に次の DLL だけが Unity パッケージへ自動コピーされる。

- コピー元: `src/Bindings.SourceGenerator/bin/Release/netstandard2.0/Bindings.SourceGenerator.dll`
- コピー先: `src/Bindings.Unity/Packages/jp.andantetribe.bindings/Runtime/Bindings.SourceGenerator.dll`

PDB や依存 DLL はこのターゲットのコピー対象ではない。Source Generator を変更した場合は、T4 の再生成後に Release ビルドを実行し、Unity 側の DLL が更新されたかを `git diff` または `git status` で確認する。リリース時の一連の処理については `.github/workflows/release.yml` を正とする。

## テストと検証

- Source Generator の変更後は、少なくとも次を実行する。

```powershell
dotnet test src/Bindings.Tests/Bindings.Tests.csproj --configuration Release
```

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
