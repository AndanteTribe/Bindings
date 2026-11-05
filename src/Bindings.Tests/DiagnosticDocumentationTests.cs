using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Bindings.Tests;

public class DiagnosticDocumentationTests
{
    [Fact]
    public void ReadmesDocumentEveryDiagnosticWithImplementedSeverity()
    {
        var descriptorType = typeof(ViewModelGenerator).Assembly.GetType("Bindings.DiagnosticDescriptors");
        Assert.NotNull(descriptorType);

        var descriptors = descriptorType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.FieldType == typeof(DiagnosticDescriptor))
            .Select(static field => Assert.IsType<DiagnosticDescriptor>(field.GetValue(null)))
            .OrderBy(static descriptor => descriptor.Id)
            .ToArray();
        Assert.NotEmpty(descriptors);

        var repositoryRoot = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(repositoryRoot, "README.md"));
        var readmeJa = File.ReadAllText(Path.Combine(repositoryRoot, "README_JA.md"));

        foreach (var descriptor in descriptors)
        {
            Assert.Contains($"| [`{descriptor.Id}`](docs/diagnostics/{descriptor.Id}.md) | {descriptor.DefaultSeverity} |", readme);
            Assert.Contains($"| [`{descriptor.Id}`](docs/diagnostics/{descriptor.Id}.ja.md) | {ToJapaneseSeverity(descriptor.DefaultSeverity)} |", readmeJa);
        }
    }

    [Theory]
    [InlineData("en-US", ".md")]
    [InlineData("ja", ".ja.md")]
    public void DiagnosticPagesMatchMetadataAndLocalizedResources(string cultureName, string extension)
    {
        var descriptors = typeof(ViewModelGenerator).Assembly.GetType("Bindings.DiagnosticDescriptors")!
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.FieldType == typeof(DiagnosticDescriptor))
            .Select(static field => (DiagnosticDescriptor)field.GetValue(null)!)
            .ToArray();
        var fixableIds = new BindingsPracticesCodeFixProvider().FixableDiagnosticIds;
        var directory = Path.Combine(FindRepositoryRoot(), "docs", "diagnostics");
        var culture = CultureInfo.GetCultureInfo(cultureName);
        foreach (var descriptor in descriptors)
        {
            var page = File.ReadAllText(Path.Combine(directory, descriptor.Id + extension));
            Assert.StartsWith($"# {descriptor.Id}: {descriptor.Title.ToString(culture)}", page);
            Assert.Contains($"> {descriptor.MessageFormat.ToString(culture)}", page);
            if (cultureName == "ja")
            {
                Assert.Contains($"| カテゴリ | {descriptor.Category} |", page);
                Assert.Contains($"| 標準重大度 | {descriptor.DefaultSeverity} |", page);
                Assert.Contains($"| 既定で有効 | {(descriptor.IsEnabledByDefault ? "はい" : "いいえ")} |", page);
                Assert.Contains($"| Code Fix | {(fixableIds.Contains(descriptor.Id) ? "あり" : "なし")} |", page);
                Assert.Contains($"[English]({descriptor.Id}.md)", page);
                Assert.Contains("../../README_JA.md#", page);
            }
            else
            {
                Assert.Contains($"| Category | {descriptor.Category} |", page);
                Assert.Contains($"| Default severity | {descriptor.DefaultSeverity} |", page);
                Assert.Contains($"| Enabled by default | {(descriptor.IsEnabledByDefault ? "Yes" : "No")} |", page);
                Assert.Contains($"| Code fix | {(fixableIds.Contains(descriptor.Id) ? "Yes" : "None")} |", page);
                Assert.Contains($"[日本語]({descriptor.Id}.ja.md)", page);
                Assert.Contains("../../README.md#", page);
            }
        }

        var expectedFiles = descriptors.SelectMany(static descriptor => new[] { descriptor.Id + ".md", descriptor.Id + ".ja.md" });
        Assert.Equal(expectedFiles.OrderBy(static name => name), Directory.GetFiles(directory, "*.md").Select(Path.GetFileName).OrderBy(static name => name));
    }

    [Fact]
    public void DocumentationLinksResolveToExistingFilesAndSections()
    {
        var root = FindRepositoryRoot();
        var pages = Directory.GetFiles(Path.Combine(root, "docs", "diagnostics"), "*.md")
            .Concat(new[] { Path.Combine(root, "README.md"), Path.Combine(root, "README_JA.md") });
        foreach (var page in pages)
        {
            foreach (Match match in Regex.Matches(File.ReadAllText(page), @"\]\(([^\s)]+)\)"))
            {
                var link = match.Groups[1].Value;
                if (Uri.TryCreate(link, UriKind.Absolute, out _))
                {
                    continue;
                }

                var parts = link.Split(new[] { '#' }, 2);
                var target = parts[0].Length == 0 ? page : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(page)!, parts[0]));
                Assert.True(File.Exists(target), $"Missing link target in {page}: {link}");
                if (parts.Length == 2)
                {
                    var headings = File.ReadLines(target).Where(static line => line.StartsWith('#'))
                        .Select(static line => Regex.Replace(line.TrimStart('#').Trim().ToLowerInvariant(), @"[^\p{L}\p{M}\p{N}_\- ]", "").Replace(' ', '-'));
                    Assert.Contains(parts[1], headings);
                }
            }
        }
    }

    [Theory]
    [InlineData(null, "main")]
    [InlineData("", "main")]
    [InlineData("1.0.0", "main")]
    [InlineData("1.0.0+not-a-commit", "main")]
    [InlineData("1.0.0+metadata+0123456789abcdef0123456789abcdef01234567", "0123456789abcdef0123456789abcdef01234567")]
    [InlineData("1.0.0+0123456789abcdef0123456789abcdef01234567+metadata", "main")]
    [InlineData("1.0.0+g123456789abcdef0123456789abcdef01234567", "main")]
    [InlineData("1.0.0+0123456789abcdef0123456789abcdef0123456_", "main")]
    [InlineData("1.0.0+012345678901234567890123456789012345678", "main")]
    [InlineData("1.0.0+01234567890123456789012345678901234567890", "main")]
    [InlineData("1.0.0+0123456789012345678901234567890123456789/extra", "main")]
    [InlineData("1.0.0+0123456789abcdef0123456789abcdef01234567", "0123456789abcdef0123456789abcdef01234567")]
    [InlineData("2.0.0-preview.1+ABCDEF0123456789ABCDEF0123456789ABCDEF01", "ABCDEF0123456789ABCDEF0123456789ABCDEF01")]
    public void DocumentationReferenceUsesCommitShaOrFallsBackToMain(string? informationalVersion, string expected)
    {
        // Run the actual nested static constructor in a fresh assembly with controlled metadata.
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Bindings.SourceGenerator", "DiagnosticDescriptors.cs"));
        var declaration = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<ClassDeclarationSyntax>().Single(static type => type.Identifier.ValueText == "Documentation");
        var fixture = "using System; using System.Reflection; namespace Bindings; internal static class DiagnosticDescriptors { "
            + declaration.ToFullString() + " }";
        var attribute = informationalVersion == null ? "" :
            "[assembly: System.Reflection.AssemblyInformationalVersion(" + SymbolDisplay.FormatLiteral(informationalVersion, true) + ")]";
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "HelpLinkFixture_" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(fixture), CSharpSyntaxTree.ParseText(attribute) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        var field = assembly.GetType("Bindings.DiagnosticDescriptors")!
            .GetNestedType("Documentation", BindingFlags.NonPublic)!.GetField("BaseUrl")!;
        Assert.Equal($"https://github.com/AndanteTribe/Bindings/blob/{expected}/docs/diagnostics/", field.GetValue(null));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("ja")]
    [InlineData("ja-JP")]
    [InlineData("tr-TR")]
    public void DiagnosticResourcesAndMetadataAreComplete(string cultureName)
    {
        var assembly = typeof(ViewModelGenerator).Assembly;
        var descriptorType = assembly.GetType("Bindings.DiagnosticDescriptors")!;
        var resources = new ResourceManager("Bindings.DiagnosticResources", assembly);
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        Assert.False(string.IsNullOrWhiteSpace(version));
        var separator = version!.LastIndexOf('+');
        var candidate = separator >= 0 ? version.Substring(separator + 1) : "";
        var reference = candidate.Length == 40
            && candidate.All(static c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F')
                ? candidate
                : "main";
        foreach (var field in descriptorType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(static field => field.FieldType == typeof(DiagnosticDescriptor)))
        {
            var descriptor = (DiagnosticDescriptor)field.GetValue(null)!;
            var title = resources.GetString(field.Name + "Title", culture);
            var message = resources.GetString(field.Name + "Message", culture);
            Assert.False(string.IsNullOrWhiteSpace(title));
            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.IsType<LocalizableResourceString>(descriptor.Title);
            Assert.IsType<LocalizableResourceString>(descriptor.MessageFormat);
            Assert.Equal(title, descriptor.Title.ToString(culture));
            Assert.Equal(message, descriptor.MessageFormat.ToString(culture));
            if (culture.TwoLetterISOLanguageName == "ja")
            {
                Assert.NotEqual(resources.GetString(field.Name + "Title", CultureInfo.InvariantCulture), title);
                Assert.NotEqual(resources.GetString(field.Name + "Message", CultureInfo.InvariantCulture), message);
            }
            else
            {
                Assert.Equal(resources.GetString(field.Name + "Title", CultureInfo.InvariantCulture), title);
                Assert.Equal(resources.GetString(field.Name + "Message", CultureInfo.InvariantCulture), message);
            }
            Assert.Contains(descriptor.Category, descriptor.CustomTags);
            Assert.Equal(
                $"https://github.com/AndanteTribe/Bindings/blob/{reference}/docs/diagnostics/{descriptor.Id}.md",
                descriptor.HelpLinkUri);
        }
    }

    [Fact]
    public void JapaneseResourcesHaveEveryKeyAndPreserveFormatArguments()
    {
        var resources = new ResourceManager("Bindings.DiagnosticResources", typeof(ViewModelGenerator).Assembly);
        var english = resources.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
        var japanese = resources.GetResourceSet(CultureInfo.GetCultureInfo("ja"), true, false)!;
        Assert.NotNull(japanese);
        var englishKeys = english.Cast<System.Collections.DictionaryEntry>().Select(static entry => (string)entry.Key).OrderBy(static key => key).ToArray();
        var japaneseKeys = japanese.Cast<System.Collections.DictionaryEntry>().Select(static entry => (string)entry.Key).OrderBy(static key => key).ToArray();
        Assert.Equal(englishKeys, japaneseKeys);
        foreach (var key in englishKeys)
        {
            var englishText = english.GetString(key)!;
            var japaneseText = japanese.GetString(key)!;
            Assert.False(string.IsNullOrWhiteSpace(japaneseText));
            var englishArguments = System.Text.RegularExpressions.Regex.Matches(englishText, @"\{\d+\}")
                .Select(static match => match.Value).OrderBy(static value => value).ToArray();
            var japaneseArguments = System.Text.RegularExpressions.Regex.Matches(japaneseText, @"\{\d+\}")
                .Select(static match => match.Value).OrderBy(static value => value).ToArray();
            Assert.Equal(englishArguments, japaneseArguments);
            Assert.DoesNotContain("{", string.Format(CultureInfo.GetCultureInfo("ja"), japaneseText, "first", "second", "third"));
        }
    }

    [Theory]
    [InlineData("ja")]
    [InlineData("ja-JP")]
    public void JapaneseDiagnosticFormatsArguments(string cultureName)
    {
        var descriptor = (DiagnosticDescriptor)typeof(ViewModelGenerator).Assembly.GetType("Bindings.DiagnosticDescriptors")!
            .GetField("Bnd007", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        var diagnostic = Diagnostic.Create(descriptor, Location.None, "_count", "Count");
        Assert.Equal(
            "フィールド '_count' には [Schema] が付与されています。フィールドではなく、生成されたプロパティ 'Count' にアクセスしてください。",
            diagnostic.GetMessage(CultureInfo.GetCultureInfo(cultureName)));
    }

    [Theory]
    [InlineData("Bnd001", "ViewModel type name must end with \"ViewModel\"", "Type '{0}' is annotated with [ViewModel] but its name does not end with \"ViewModel\". No source will be generated for this ViewModel.")]
    [InlineData("Bnd002", "Invalid [Schema] id value", "[Schema] id value {0} is invalid. Use id >= 0 for explicit grouping, or omit id (defaults to -1) for auto-numbering.")]
    [InlineData("Bnd003", "Conflicting tooltip values for the same View field", "View field '{0}' has conflicting tooltip values from multiple [Schema] entries with the same id. Only the first tooltip will be used.")]
    [InlineData("Bnd004", "Unsupported type accessibility", "Type '{0}' has unsupported accessibility '{1}'. No source will be generated for ViewModel '{2}'.")]
    [InlineData("Bnd005", "Unity deserialization may leave ViewModel state uninitialized", "Field '{0}' uses [{2}] with ViewModel type '{1}'. Unity deserialization may leave generated runtime state uninitialized, even when the type is marked [Serializable]. Construct or assign the ViewModel at runtime instead.")]
    [InlineData("Bnd006", "View implementations must not derive from MonoBehaviour", "Type '{0}' implements Bindings.IView and derives from UnityEngine.MonoBehaviour. View implementations must not derive from MonoBehaviour.")]
    [InlineData("Bnd007", "Schema fields must be accessed through generated properties", "Field '{0}' is annotated with [Schema]. Access the generated property '{1}' instead of the field.")]
    [InlineData("Bndp001", "Use the generated self-rebind helper", "Use PublishRebindMessage() instead of directly publishing a rebind message for '{0}'")]
    [InlineData("Bndp002", "Views must not construct ViewModels", "View '{0}' constructs ViewModel '{1}'. Construct the ViewModel outside the View and pass it in instead.")]
    [InlineData("Bndp003", "ViewModels must not merely aggregate other ViewModels", "ViewModel '{0}' only stores or exposes child ViewModel member '{1}'. If it is not used in the owning ViewModel's logic, move ownership outside this ViewModel.")]
    [InlineData("Bndp004", "Do not wrap generated Schema property assignments", "Method '{0}' only assigns its parameters directly to publicly writable Schema properties. Assign the properties directly instead.")]
    public void ResourceMigrationPreservesDiagnosticText(string fieldName, string title, string message)
    {
        var descriptor = (DiagnosticDescriptor)typeof(ViewModelGenerator).Assembly.GetType("Bindings.DiagnosticDescriptors")!
            .GetField(fieldName, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        Assert.Equal(title, descriptor.Title.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(message, descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("en-US", "ViewModel 'Parent' only stores or exposes child ViewModel member 'Child'. If it is not used in the owning ViewModel's logic, move ownership outside this ViewModel.")]
    [InlineData("ja", "ViewModel 'Parent' は子 ViewModel メンバー 'Child' を保持または公開するだけです。所有する ViewModel 自身の処理で利用しないのであれば、所有をこの ViewModel の外へ移してください。")]
    [InlineData("ja-JP", "ViewModel 'Parent' は子 ViewModel メンバー 'Child' を保持または公開するだけです。所有する ViewModel 自身の処理で利用しないのであれば、所有をこの ViewModel の外へ移してください。")]
    public void AggregationDiagnosticUsesConditionalOwnershipGuidance(string cultureName, string expected)
    {
        var descriptor = (DiagnosticDescriptor)typeof(ViewModelGenerator).Assembly.GetType("Bindings.DiagnosticDescriptors")!
            .GetField("Bndp003", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        var diagnostic = Diagnostic.Create(descriptor, Location.None, "Parent", "Child");
        Assert.Equal(expected, diagnostic.GetMessage(CultureInfo.GetCultureInfo(cultureName)));
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bindings.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not find the Bindings repository root.");
    }

    private static string ToJapaneseSeverity(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Hidden => "非表示",
        DiagnosticSeverity.Info => "情報",
        DiagnosticSeverity.Warning => "警告",
        DiagnosticSeverity.Error => "エラー",
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null),
    };
}