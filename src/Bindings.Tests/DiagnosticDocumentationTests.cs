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

        var repositoryRoot = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(repositoryRoot, "README.md"));
        var readmeJa = File.ReadAllText(Path.Combine(repositoryRoot, "README_JA.md"));

        foreach (var descriptor in descriptors)
        {
            Assert.Contains($"| `{descriptor.Id}` | {descriptor.DefaultSeverity} |", readme);
            Assert.Contains($"| `{descriptor.Id}` | {ToJapaneseSeverity(descriptor.DefaultSeverity)} |", readmeJa);
        }
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