using System;
using System.Collections.Generic;
using System.Globalization;
using Bindings.GeneratorCore;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Bindings.Tests;

/// <summary>
/// Tests for T4 template base class infrastructure methods, template helper utilities,
/// template partial helpers, and the CompilerError/CompilerErrorCollection polyfill.
/// These tests exist to bring line and branch coverage of the generated template
/// infrastructure above the 90% threshold.
/// </summary>
public class TemplateBaseTests
{
    // -------------------------------------------------------------------------
    // Helper: build a minimal GenerationContext for direct template instantiation
    // -------------------------------------------------------------------------

    private static GenerationContext MinimalContext(
        string className = "CountViewModel1",
        string ns = "Test.Space",
        bool isStruct = false,
        bool isReadOnly = false) =>
        new GenerationContext(
            className: className,
            @namespace: ns,
            containingTypes: Array.Empty<(string, string)>(),
            isStruct: isStruct,
            isReadOnly: isReadOnly,
            requireBindImplementation: false,
            alreadySerializable: false,
            requiredFields: Array.Empty<(string, string)>(),
            schemaFields: Array.Empty<(string, string, string, int, string, string)>(),
            schemaMethods: Array.Empty<(string, string, int, string)>(),
            diagnostics: Array.Empty<(DiagnosticDescriptor, Location, string[])>());

    private static ViewTemplate MinimalViewTemplate() =>
        new ViewTemplate(
            MinimalContext(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<(string, string, string)>());

    // -------------------------------------------------------------------------
    // ViewModelTemplateBase: Session property get/set
    // -------------------------------------------------------------------------

    [Fact]
    public void ViewModelTemplate_Session_GetReturnsNullInitially()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        Assert.Null(tmpl.Session);
    }

    [Fact]
    public void ViewModelTemplate_Session_SetAndGet()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        var dict = new Dictionary<string, object> { ["key"] = "value" };
        tmpl.Session = dict;
        Assert.Equal(dict, tmpl.Session);
    }

    // -------------------------------------------------------------------------
    // ViewModelTemplateBase: Error / Warning methods
    // -------------------------------------------------------------------------

    [Fact]
    public void ViewModelTemplate_Error_DoesNotThrow()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        tmpl.Error("test error message");
    }

    [Fact]
    public void ViewModelTemplate_Warning_DoesNotThrow()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        tmpl.Warning("test warning message");
    }

    // -------------------------------------------------------------------------
    // ViewModelTemplateBase: GenerationEnvironment lazy-init (null setter branch)
    // -------------------------------------------------------------------------

    [Fact]
    public void ViewModelTemplate_GenerationEnvironment_NullSetThenGetTriggersLazyInit()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        tmpl.GenerationEnvironment = null!;
        var env = tmpl.GenerationEnvironment;
        Assert.NotNull(env);
    }

    // -------------------------------------------------------------------------
    // ViewModelTemplateBase: PushIndent / PopIndent / ClearIndent
    // -------------------------------------------------------------------------

    [Fact]
    public void ViewModelTemplate_PopIndent_WhenEmpty_ReturnsEmptyString()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        Assert.Equal(string.Empty, tmpl.PopIndent());
    }

    [Fact]
    public void ViewModelTemplate_PushAndPop_RoundTrip()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        tmpl.PushIndent("    ");
        Assert.Equal("    ", tmpl.CurrentIndent);
        var popped = tmpl.PopIndent();
        Assert.Equal("    ", popped);
        Assert.Equal(string.Empty, tmpl.CurrentIndent);
    }

    [Fact]
    public void ViewModelTemplate_ClearIndent_ResetsToEmpty()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        tmpl.PushIndent("    ");
        tmpl.PushIndent("    ");
        tmpl.ClearIndent();
        Assert.Equal(string.Empty, tmpl.CurrentIndent);
    }

    // -------------------------------------------------------------------------
    // ViewModelTemplateBase: Write(format, params object[])
    // -------------------------------------------------------------------------

    [Fact]
    public void ViewModelTemplate_WriteFormatArgs_AppendsFormattedText()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        tmpl.GenerationEnvironment = null!;
        tmpl.Write("{0} {1}", "hello", "world");
        Assert.Equal("hello world", tmpl.GenerationEnvironment.ToString());
    }

    // -------------------------------------------------------------------------
    // ViewModelTemplateBase: WriteLine(string) / WriteLine(format, params)
    // -------------------------------------------------------------------------

    [Fact]
    public void ViewModelTemplate_WriteLineString_PrependsIndent()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        tmpl.GenerationEnvironment = null!;
        tmpl.PushIndent("  ");
        tmpl.WriteLine("content");
        Assert.StartsWith("  content", tmpl.GenerationEnvironment.ToString());
    }

    [Fact]
    public void ViewModelTemplate_WriteLineFormatArgs_PrependsIndentAndFormats()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        tmpl.GenerationEnvironment = null!;
        tmpl.PushIndent("  ");
        tmpl.WriteLine("{0}+{1}", "a", "b");
        Assert.StartsWith("  a+b", tmpl.GenerationEnvironment.ToString());
    }

    // -------------------------------------------------------------------------
    // ViewModelTemplateBase: Initialize()
    // -------------------------------------------------------------------------

    [Fact]
    public void ViewModelTemplate_Initialize_DoesNotThrow()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        tmpl.Initialize();
    }

    // -------------------------------------------------------------------------
    // ViewModelTemplateBase.ToStringInstanceHelper: FormatProvider get/set
    // -------------------------------------------------------------------------

    [Fact]
    public void ViewModelTemplate_ToStringHelper_FormatProvider_SetNull_IsIgnored()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        var original = tmpl.ToStringHelper.FormatProvider;
        tmpl.ToStringHelper.FormatProvider = null!;
        Assert.Equal(original, tmpl.ToStringHelper.FormatProvider);
    }

    [Fact]
    public void ViewModelTemplate_ToStringHelper_FormatProvider_SetNonNull_Applies()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        var newProvider = CultureInfo.GetCultureInfo("fr-FR");
        tmpl.ToStringHelper.FormatProvider = newProvider;
        Assert.Equal(newProvider, tmpl.ToStringHelper.FormatProvider);
    }

    // -------------------------------------------------------------------------
    // ViewModelTemplateBase.ToStringInstanceHelper: ToStringWithCulture edge cases
    // -------------------------------------------------------------------------

    [Fact]
    public void ViewModelTemplate_ToStringWithCulture_Null_ThrowsArgumentNullException()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        Assert.Throws<ArgumentNullException>(() => tmpl.ToStringHelper.ToStringWithCulture(null!));
    }

    private sealed class NonConvertible
    {
        public override string ToString() => "custom-str";
    }

    [Fact]
    public void ViewModelTemplate_ToStringWithCulture_NonIConvertibleObject_FallsBackToToString()
    {
        var tmpl = new ViewModelTemplate(MinimalContext());
        var result = tmpl.ToStringHelper.ToStringWithCulture(new NonConvertible());
        Assert.Equal("custom-str", result);
    }

    // =========================================================================
    // ViewTemplateBase: symmetric infrastructure tests
    // =========================================================================

    [Fact]
    public void ViewTemplate_Session_GetReturnsNullInitially()
    {
        var tmpl = MinimalViewTemplate();
        Assert.Null(tmpl.Session);
    }

    [Fact]
    public void ViewTemplate_Session_SetAndGet()
    {
        var tmpl = MinimalViewTemplate();
        var dict = new Dictionary<string, object> { ["k"] = 42 };
        tmpl.Session = dict;
        Assert.Equal(dict, tmpl.Session);
    }

    [Fact]
    public void ViewTemplate_Error_DoesNotThrow()
    {
        var tmpl = MinimalViewTemplate();
        tmpl.Error("view error");
    }

    [Fact]
    public void ViewTemplate_Warning_DoesNotThrow()
    {
        var tmpl = MinimalViewTemplate();
        tmpl.Warning("view warning");
    }

    [Fact]
    public void ViewTemplate_GenerationEnvironment_NullSetThenGetTriggersLazyInit()
    {
        var tmpl = MinimalViewTemplate();
        tmpl.GenerationEnvironment = null!;
        var env = tmpl.GenerationEnvironment;
        Assert.NotNull(env);
    }

    [Fact]
    public void ViewTemplate_PopIndent_WhenEmpty_ReturnsEmptyString()
    {
        var tmpl = MinimalViewTemplate();
        Assert.Equal(string.Empty, tmpl.PopIndent());
    }

    [Fact]
    public void ViewTemplate_PushAndPop_RoundTrip()
    {
        var tmpl = MinimalViewTemplate();
        tmpl.PushIndent("    ");
        Assert.Equal("    ", tmpl.CurrentIndent);
        var popped = tmpl.PopIndent();
        Assert.Equal("    ", popped);
        Assert.Equal(string.Empty, tmpl.CurrentIndent);
    }

    [Fact]
    public void ViewTemplate_ClearIndent_ResetsToEmpty()
    {
        var tmpl = MinimalViewTemplate();
        tmpl.PushIndent("    ");
        tmpl.ClearIndent();
        Assert.Equal(string.Empty, tmpl.CurrentIndent);
    }

    [Fact]
    public void ViewTemplate_WriteFormatArgs_AppendsFormattedText()
    {
        var tmpl = MinimalViewTemplate();
        tmpl.GenerationEnvironment = null!;
        tmpl.Write("{0}:{1}", "x", "y");
        Assert.Equal("x:y", tmpl.GenerationEnvironment.ToString());
    }

    [Fact]
    public void ViewTemplate_WriteLineString_PrependsIndent()
    {
        var tmpl = MinimalViewTemplate();
        tmpl.GenerationEnvironment = null!;
        tmpl.PushIndent("  ");
        tmpl.WriteLine("line");
        Assert.StartsWith("  line", tmpl.GenerationEnvironment.ToString());
    }

    [Fact]
    public void ViewTemplate_WriteLineFormatArgs_PrependsIndentAndFormats()
    {
        var tmpl = MinimalViewTemplate();
        tmpl.GenerationEnvironment = null!;
        tmpl.PushIndent("  ");
        tmpl.WriteLine("{0}={1}", "p", "q");
        Assert.StartsWith("  p=q", tmpl.GenerationEnvironment.ToString());
    }

    [Fact]
    public void ViewTemplate_Initialize_DoesNotThrow()
    {
        var tmpl = MinimalViewTemplate();
        tmpl.Initialize();
    }

    [Fact]
    public void ViewTemplate_ToStringHelper_FormatProvider_SetNull_IsIgnored()
    {
        var tmpl = MinimalViewTemplate();
        var original = tmpl.ToStringHelper.FormatProvider;
        tmpl.ToStringHelper.FormatProvider = null!;
        Assert.Equal(original, tmpl.ToStringHelper.FormatProvider);
    }

    [Fact]
    public void ViewTemplate_ToStringHelper_FormatProvider_SetNonNull_Applies()
    {
        var tmpl = MinimalViewTemplate();
        tmpl.ToStringHelper.FormatProvider = CultureInfo.GetCultureInfo("de-DE");
        Assert.Equal(CultureInfo.GetCultureInfo("de-DE"), tmpl.ToStringHelper.FormatProvider);
    }

    [Fact]
    public void ViewTemplate_ToStringWithCulture_Null_ThrowsArgumentNullException()
    {
        var tmpl = MinimalViewTemplate();
        Assert.Throws<ArgumentNullException>(() => tmpl.ToStringHelper.ToStringWithCulture(null!));
    }

    [Fact]
    public void ViewTemplate_ToStringWithCulture_NonIConvertibleObject_FallsBackToToString()
    {
        var tmpl = MinimalViewTemplate();
        var result = tmpl.ToStringHelper.ToStringWithCulture(new NonConvertible());
        Assert.Equal("custom-str", result);
    }

    // =========================================================================
    // CompilerError / CompilerErrorCollection polyfill classes
    // =========================================================================

    [Fact]
    public void CompilerError_DefaultConstructor_Works()
    {
        var err = new System.CodeDom.Compiler.CompilerError();
        Assert.Null(err.ErrorText);
        Assert.False(err.IsWarning);
    }

    [Fact]
    public void CompilerError_FullConstructor_SetsErrorText()
    {
        var err = new System.CodeDom.Compiler.CompilerError("file.cs", 1, 1, "CS0001", "some error");
        Assert.Equal("some error", err.ErrorText);
    }

    [Fact]
    public void CompilerError_IsWarning_CanBeSetToTrue()
    {
        var err = new System.CodeDom.Compiler.CompilerError();
        err.IsWarning = true;
        Assert.True(err.IsWarning);
    }

    [Fact]
    public void CompilerErrorCollection_Add_DoesNotThrow()
    {
        var col = new System.CodeDom.Compiler.CompilerErrorCollection();
        col.Add(new System.CodeDom.Compiler.CompilerError());
    }

    // =========================================================================
    // TemplateHelpers: m_ prefix stripping and empty-after-strip edge cases
    // =========================================================================

    [Fact]
    public void ViewModelTemplate_ToPropertyName_mPrefixField_IsStripped()
    {
        Assert.Equal("Count", ViewModelTemplate.ToPropertyName("m_Count"));
    }

    [Fact]
    public void ViewModelTemplate_ToPropertyName_UnderscoreAndmPrefixField_IsStripped()
    {
        Assert.Equal("Value", ViewModelTemplate.ToPropertyName("_m_Value"));
    }

    [Fact]
    public void ViewModelTemplate_ToParamName_mPrefixField_IsStripped()
    {
        Assert.Equal("count", ViewModelTemplate.ToParamName("m_Count"));
    }

    [Fact]
    public void ViewModelTemplate_ToPropertyName_OnlyUnderscore_ReturnsEmpty()
    {
        // "_" strips to "" → ToPropertyName returns ""
        Assert.Equal("", ViewModelTemplate.ToPropertyName("_"));
    }

    [Fact]
    public void ViewModelTemplate_ToParamName_OnlyUnderscore_ReturnsEmpty()
    {
        Assert.Equal("", ViewModelTemplate.ToParamName("_"));
    }

    // =========================================================================
    // ViewTemplate.SplitBindingPath: path with no dot
    // =========================================================================

    [Fact]
    public void ViewTemplate_SplitBindingPath_NoDot_ReturnsSelfAndEmptyMember()
    {
        var (typePart, memberName) = ViewTemplate.SplitBindingPath("nodot");
        Assert.Equal("nodot", typePart);
        Assert.Equal("", memberName);
    }
}
