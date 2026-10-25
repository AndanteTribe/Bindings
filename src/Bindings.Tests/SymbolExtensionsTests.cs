using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Bindings.Tests;

public class SymbolExtensionsTests
{
    private static bool InvokePredicate(string name, params object?[] arguments)
    {
        var extensions = typeof(ViewModelGenerator).Assembly.GetType("Bindings.SymbolExtensions")!;
        return Assert.IsType<bool>(extensions.GetMethod(name, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, arguments));
    }

    [Theory]
    [InlineData("interface IContract { void M(int value); } class Sample : IContract { public void M(int value) { } }", true)]
    [InlineData("interface IContract { void M(int value); } class Sample : IContract { void IContract.M(int value) { } }", true)]
    [InlineData("interface IContract<T> { void M(T value); } class Sample : IContract<int> { public void M(int value) { } }", true)]
    [InlineData("interface IContract<T> { void M(T value); } class Sample : IContract<int> { void IContract<int>.M(int value) { } }", true)]
    [InlineData("interface IRoot { void M(int value); } interface IContract : IRoot { } class Sample : IContract { public void M(int value) { } }", true)]
    [InlineData("interface IContract { void M<T>(T value); } class Sample : IContract { public void M<T>(T value) { } }", true)]
    [InlineData("interface IContract { void M(int value); } abstract class Sample : IContract { public abstract void M(int value); }", true)]
    [InlineData("class Sample { public void M(int value) { } }", false)]
    [InlineData("class Sample { public static void M(int value) { } }", false)]
    public void InterfaceImplementationChecksDoNotDependOnBindings(string source, bool expected)
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation(source, stubs: "");
        Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var type = compilation.GetTypeByMetadataName("Sample")!;
        var method = type.GetMembers().OfType<IMethodSymbol>().Single(static method =>
            method.Name == "M" || method.Name.EndsWith(".M", StringComparison.Ordinal));
        Assert.Equal(expected, InvokePredicate("IsInterfaceImplementation", method));
    }

    [Fact]
    public void UnrelatedOverloadsAreNotInterfaceImplementations()
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation("""
            interface IContract { void M(string value); }
            class Sample : IContract {
                public void M(string value) { }
                public void M(int value) { }
            }
            """, stubs: "");
        var methods = compilation.GetTypeByMetadataName("Sample")!.GetMembers("M").OfType<IMethodSymbol>();
        foreach (var method in methods)
        {
            Assert.Equal(method.Parameters[0].Type.SpecialType == SpecialType.System_String, InvokePredicate("IsInterfaceImplementation", method));
        }
    }

    [Fact]
    public void PartialInterfaceMethodsAreRecognizedFromEitherPart()
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation("""
            interface IContract { void M(int value); }
            partial class Sample : IContract {
                public partial void M(int value);
                public partial void M(int value) { }
            }
            """, stubs: "");
        Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var definition = Assert.IsAssignableFrom<IMethodSymbol>(Assert.Single(compilation.GetTypeByMetadataName("Sample")!.GetMembers("M")));
        Assert.True(InvokePredicate("IsInterfaceImplementation", definition));
        Assert.NotNull(definition.PartialImplementationPart);
        Assert.True(InvokePredicate("IsInterfaceImplementation", definition.PartialImplementationPart));
    }

    [Theory]
    [InlineData("[Bindings.Schema(\"Text.text\")] private int _count; public int Count { get; set; }", true)]
    [InlineData("[Bindings.Schema(\"Text.text\")] private int m_count; public int Count { get; set; }", true)]
    [InlineData("[Bindings.Schema(\"Text.text\")] private int _count; public int Count { get; private set; }", true)]
    [InlineData("[Bindings.Schema(\"Text.text\")] private int _count; public string Count { get; set; }", false)]
    [InlineData("[Bindings.Schema(\"Text.text\")] private int _other; public int Count { get; set; }", false)]
    [InlineData("private int _count; public int Count { get; set; }", false)]
    [InlineData("public int Count { get; set; }", false)]
    public void SchemaPropertyMappingChecksOnlyFieldNameTypeAndAttribute(string members, bool expected)
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation("class Sample { " + members + " }");
        var property = Assert.IsAssignableFrom<IPropertySymbol>(Assert.Single(compilation.GetTypeByMetadataName("Sample")!.GetMembers("Count")));
        var schemaAttribute = compilation.GetTypeByMetadataName("Bindings.SchemaAttribute");
        Assert.Equal(expected, InvokePredicate("IsSchemaProperty", property, schemaAttribute));
        Assert.False(InvokePredicate("IsSchemaProperty", property, null));
    }
}