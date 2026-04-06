using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Bindings.Tests;

public class ViewModelGeneratorTests
{
    // A class annotated with [ViewModel], [Model], and [Schema] for testing information gathering.
    private const string SampleViewModelText = @"
namespace TestNamespace;

[Bindings.ViewModel]
public partial class TestViewModel
{
    [Bindings.Model]
    private TestModel _model;

    [Bindings.Schema(""path/to/text"")]
    private int _value;

    [Bindings.Schema(""path/to/button"")]
    public void OnClick() { }
}

public class TestModel { }
";

    // A class annotated with [ViewModel(requireBindImplementation: true)] and [Serializable].
    private const string SerializableViewModelText = @"
namespace TestNamespace;

[System.Serializable]
[Bindings.ViewModel(true)]
public partial class SerializableViewModel
{
    [Bindings.Model]
    private object _model;
}
";

    private static CSharpGeneratorDriver CreateDriver() =>
        CSharpGeneratorDriver.Create(new ViewModelGenerator());

    private static Compilation CreateCompilation(string source) =>
        CSharpCompilation.Create(nameof(ViewModelGeneratorTests),
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

    [Fact]
    public void RegistersAttributeDefinitions()
    {
        var driver = CreateDriver();
        var compilation = CSharpCompilation.Create(nameof(ViewModelGeneratorTests));

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        var attributesFile = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("BindingsAttributes.g.cs"));

        Assert.NotNull(attributesFile);
        var text = attributesFile.GetText().ToString();
        Assert.Contains("ViewModelAttribute", text);
        Assert.Contains("ModelAttribute", text);
        Assert.Contains("SchemaAttribute", text);
    }

    [Fact]
    public void GeneratesPartialClassForViewModelAnnotatedClass()
    {
        var driver = CreateDriver();
        var compilation = CreateCompilation(SampleViewModelText);

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        var generatedFile = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("TestViewModel.g.cs"));

        Assert.NotNull(generatedFile);
    }

    [Fact]
    public void DoesNotGenerateForNonViewModelClass()
    {
        const string plainClass = @"
namespace TestNamespace;
public class PlainClass { }
";
        var driver = CreateDriver();
        var compilation = CreateCompilation(plainClass);

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        var viewModelFile = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("PlainClass.g.cs"));

        Assert.Null(viewModelFile);
    }

    [Fact]
    public void BuildGenerationContext_ExtractsModels()
    {
        var driver = CreateDriver();
        var compilation = CreateCompilation(SampleViewModelText);
        var runResult = driver.RunGenerators(compilation);

        // Get the updated compilation (includes generated attribute definitions).
        runResult.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out _);

        var tree = updatedCompilation.SyntaxTrees
            .First(t => t.GetText().ToString().Contains("TestViewModel"));
        var semanticModel = updatedCompilation.GetSemanticModel(tree);
        var classSymbol = updatedCompilation.GetTypeByMetadataName("TestNamespace.TestViewModel");

        Assert.NotNull(classSymbol);
        var ctx = ViewModelGenerator.BuildGenerationContext(classSymbol!);

        Assert.Single(ctx.Models);
        Assert.Equal("TestNamespace.TestModel", ctx.Models[0].typeFullName);
        Assert.Equal("_model", ctx.Models[0].name);
    }

    [Fact]
    public void BuildGenerationContext_ExtractsSchemas()
    {
        var driver = CreateDriver();
        var compilation = CreateCompilation(SampleViewModelText);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out _);

        var classSymbol = updatedCompilation.GetTypeByMetadataName("TestNamespace.TestViewModel");

        Assert.NotNull(classSymbol);
        var ctx = ViewModelGenerator.BuildGenerationContext(classSymbol!);

        Assert.Single(ctx.Schemas);
        Assert.Equal("path/to/text", ctx.Schemas[0].bindingPath);
    }

    [Fact]
    public void BuildGenerationContext_ExtractsSchemaMethods()
    {
        var driver = CreateDriver();
        var compilation = CreateCompilation(SampleViewModelText);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out _);

        var classSymbol = updatedCompilation.GetTypeByMetadataName("TestNamespace.TestViewModel");

        Assert.NotNull(classSymbol);
        var ctx = ViewModelGenerator.BuildGenerationContext(classSymbol!);

        Assert.Single(ctx.SchemaMethods);
        Assert.Equal("path/to/button", ctx.SchemaMethods[0].bindingPath);
    }

    [Fact]
    public void BuildGenerationContext_DetectsRequireBindImplementation()
    {
        var driver = CreateDriver();
        var compilation = CreateCompilation(SerializableViewModelText);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out _);

        var classSymbol = updatedCompilation.GetTypeByMetadataName("TestNamespace.SerializableViewModel");

        Assert.NotNull(classSymbol);
        var ctx = ViewModelGenerator.BuildGenerationContext(classSymbol!);

        Assert.True(ctx.RequireBindImplementation);
    }

    [Fact]
    public void BuildGenerationContext_DetectsAlreadySerializable()
    {
        var driver = CreateDriver();
        var compilation = CreateCompilation(SerializableViewModelText);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out _);

        var classSymbol = updatedCompilation.GetTypeByMetadataName("TestNamespace.SerializableViewModel");

        Assert.NotNull(classSymbol);
        var ctx = ViewModelGenerator.BuildGenerationContext(classSymbol!);

        Assert.True(ctx.AlreadySerializable);
    }
}
