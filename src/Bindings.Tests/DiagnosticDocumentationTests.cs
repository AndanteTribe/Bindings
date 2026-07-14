using System;
using System.IO;
using System.Linq;
using System.Reflection;
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

        var readme = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "README.md"));
        var readmeJa = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "README_JA.md"));

        foreach (var descriptor in descriptors)
        {
            Assert.Contains($"| `{descriptor.Id}` | {descriptor.DefaultSeverity} |", readme);
            Assert.Contains($"| `{descriptor.Id}` | {ToJapaneseSeverity(descriptor.DefaultSeverity)} |", readmeJa);
        }
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