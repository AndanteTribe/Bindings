using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using Microsoft.CodeAnalysis;
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
            .Where(field => field.FieldType == typeof(DiagnosticDescriptor))
            .Select(field => Assert.IsType<DiagnosticDescriptor>(field.GetValue(null)))
            .OrderBy(descriptor => descriptor.Id)
            .ToArray();
        Assert.NotEmpty(descriptors);

        var repositoryRoot = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(repositoryRoot, "README.md"));
        var readmeJa = File.ReadAllText(Path.Combine(repositoryRoot, "README_JA.md"));

        foreach (var descriptor in descriptors)
        {
            Assert.Contains($"| `{descriptor.Id}` | {descriptor.DefaultSeverity} |", readme);
            Assert.Contains($"| `{descriptor.Id}` | {ToJapaneseSeverity(descriptor.DefaultSeverity)} |", readmeJa);
        }
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("ja-JP")]
    [InlineData("tr-TR")]
    public void DiagnosticResourcesAndMetadataAreComplete(string cultureName)
    {
        var assembly = typeof(ViewModelGenerator).Assembly;
        var descriptorType = assembly.GetType("Bindings.DiagnosticDescriptors")!;
        var resources = new ResourceManager("Bindings.DiagnosticResources", assembly);
        var culture = CultureInfo.GetCultureInfo(cultureName);
        foreach (var field in descriptorType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.FieldType == typeof(DiagnosticDescriptor)))
        {
            var descriptor = (DiagnosticDescriptor)field.GetValue(null)!;
            var title = resources.GetString(field.Name + "Title", CultureInfo.InvariantCulture);
            var message = resources.GetString(field.Name + "Message", CultureInfo.InvariantCulture);
            Assert.False(string.IsNullOrWhiteSpace(title));
            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.IsType<LocalizableResourceString>(descriptor.Title);
            Assert.IsType<LocalizableResourceString>(descriptor.MessageFormat);
            Assert.Equal(title, descriptor.Title.ToString(culture));
            Assert.Equal(message, descriptor.MessageFormat.ToString(culture));
            Assert.Contains(descriptor.Category, descriptor.CustomTags);
            Assert.Equal(
                "https://github.com/AndanteTribe/Bindings/blob/main/README.md#" +
                (descriptor.Category == "Bindings.Practices" ? "best-practice-diagnostics" : "analyzer-diagnostics"),
                descriptor.HelpLinkUri);
        }
    }

    [Theory]
    [InlineData("Bnd001", "ViewModel type name must contain \"ViewModel\"", "Type '{0}' is annotated with [ViewModel] but its name does not contain \"ViewModel\". No source will be generated for this ViewModel.")]
    [InlineData("Bnd002", "Invalid [Schema] id value", "[Schema] id value {0} is invalid. Use id >= 0 for explicit grouping, or omit id (defaults to -1) for auto-numbering.")]
    [InlineData("Bnd003", "Conflicting tooltip values for the same View field", "View field '{0}' has conflicting tooltip values from multiple [Schema] entries with the same id. Only the first tooltip will be used.")]
    [InlineData("Bnd004", "Unsupported type accessibility", "Type '{0}' has unsupported accessibility '{1}'. No source will be generated for ViewModel '{2}'.")]
    [InlineData("Bnd005", "Unity deserialization may leave ViewModel state uninitialized", "Field '{0}' uses [{2}] with ViewModel type '{1}'. Unity deserialization may leave generated runtime state uninitialized, even when the type is marked [Serializable]. Construct or assign the ViewModel at runtime instead.")]
    [InlineData("Bnd006", "View implementations must not derive from MonoBehaviour", "Type '{0}' implements Bindings.IView and derives from UnityEngine.MonoBehaviour. View implementations must not derive from MonoBehaviour.")]
    [InlineData("Bnd007", "Schema fields must be accessed through generated properties", "Field '{0}' is annotated with [Schema]. Access the generated property '{1}' instead of the field.")]
    [InlineData("Bndp001", "Use the generated self-rebind helper", "Use PublishRebindMessage() instead of directly publishing a rebind message for '{0}'")]
    [InlineData("Bndp002", "Views must not construct ViewModels", "View '{0}' constructs ViewModel '{1}'. Construct the ViewModel outside the View and pass it in instead.")]
    public void ResourceMigrationPreservesDiagnosticText(string fieldName, string title, string message)
    {
        var descriptor = (DiagnosticDescriptor)typeof(ViewModelGenerator).Assembly.GetType("Bindings.DiagnosticDescriptors")!
            .GetField(fieldName, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        Assert.Equal(title, descriptor.Title.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(message, descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture));
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