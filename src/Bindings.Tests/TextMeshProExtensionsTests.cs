using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using Xunit;

namespace Bindings.Tests;

public class TextMeshProExtensionsTests
{
    public static IEnumerable<object[]> FormattedValues()
    {
        yield return new object[] { byte.MaxValue, "D3", "D200" };
        yield return new object[] { sbyte.MinValue, "D3", "D200" };
        yield return new object[] { short.MinValue, "D3", "D200" };
        yield return new object[] { ushort.MaxValue, "D3", "D200" };
        yield return new object[] { int.MinValue, "D3", "D200" };
        yield return new object[] { uint.MaxValue, "D3", "D200" };
        yield return new object[] { long.MinValue, "D3", "D200" };
        yield return new object[] { ulong.MaxValue, "D3", "D200" };
        yield return new object[] { 1.25f, "F2", "F200" };
        yield return new object[] { 1.25d, "F2", "F200" };
        yield return new object[] { decimal.MaxValue, "F2", "F200" };
        yield return new object[] { new DateTime(2026, 10, 18, 12, 34, 56), "O", "'" + new string('x', 200) + "'" };
        yield return new object[] { new DateTimeOffset(2026, 10, 18, 12, 34, 56, TimeSpan.FromHours(9)), "O", "'" + new string('x', 200) + "'" };
        yield return new object[] { Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"), "D", "B" };
        yield return new object[] { TimeSpan.FromHours(-123.5), "c", "'" + new string('x', 200) + "'" };
    }

    [Theory]
    [MemberData(nameof(FormattedValues))]
    public void TypedAndNullableValuesPreserveFormatting(object value, string format, string longFormat)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "en-US", "fr-FR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                foreach (var currentFormat in new[] { "", format, longFormat })
                {
                    var expected = ((IFormattable)value).ToString(currentFormat, null);
                    foreach (var nullable in new[] { false, true })
                    {
                        var type = nullable ? typeof(Nullable<>).MakeGenericType(value.GetType()) : value.GetType();
                        var method = typeof(TextMeshProExtensions).GetMethod("SetValue", new[] { typeof(TMP_Text), type, typeof(string) });
                        Assert.NotNull(method);
                        var text = new TMP_Text();
                        method.Invoke(null, new[] { text, value, currentFormat });
                        Assert.Equal(expected, text.text);
#if UGUI2_6_0_OR_GREATER
                        Assert.Equal(expected.Length <= 64 ? 1 : 0, text.SpanCalls);
                        Assert.Equal(expected.Length <= 64 ? 0 : 1, text.ArrayCalls);
#else
                        Assert.Equal(1, text.ArrayCalls);
#endif
                        if (nullable)
                        {
                            method.Invoke(null, new object?[] { text, null, currentFormat });
                            Assert.Equal("", text.text);
                        }
                    }
                }
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void ConcreteReceiversUseTheTypedOverload()
    {
        var ui = new TextMeshProUGUI();
        var world = new TextMeshPro();
        TMP_Text baseText = new TextMeshProUGUI();
        ui.SetValue(123);
        world.SetValue(123);
        baseText.SetValue(123);
        foreach (var text in new TMP_Text[] { ui, world, baseText })
        {
            Assert.Equal("123", text.text);
            Assert.Equal(1, text.SpanCalls + text.ArrayCalls);
        }
    }

    [Fact]
    public void StringsBooleansCharactersAndSpansAreSupported()
    {
        var text = new TMP_Text();
        text.SetValue("hello");
        Assert.Equal("hello", text.text);
        text.SetValue((string?)null);
        Assert.Null(text.text);
        text.SetValue(true);
        Assert.Equal("True", text.text);
        text.SetValue(false);
        Assert.Equal("False", text.text);
        text.SetValue((bool?)true);
        Assert.Equal("True", text.text);
        text.SetValue((bool?)null);
        Assert.Equal("", text.text);
        text.SetValue('あ');
        Assert.Equal("あ", text.text);
        text.SetValue((char?)'X');
        Assert.Equal("X", text.text);
        text.SetValue((char?)null);
        Assert.Equal("", text.text);
        text.SetValue("prefix 😀<b>text</b> suffix".AsSpan(7, 14));
        Assert.Equal("😀<b>text</b> ", text.text);
        text.SetValue(ReadOnlySpan<char>.Empty);
        Assert.Equal("", text.text);
    }

    [Fact]
    public void ObjectFallbackPreservesFormattingAndNullHandling()
    {
        var text = new TMP_Text();
        text.SetValue((object)123, "D5");
        Assert.Equal(123.ToString("D5"), text.text);
        text.SetValue((object)new NamedValue());
        Assert.Equal("custom", text.text);
        text.SetValue((object?)null);
        Assert.Equal("", text.text);
        text.SetValue((object)DayOfWeek.Monday);
        Assert.Equal("Monday", text.text);
    }

    [Fact]
    public void FormattableStructsUseTheGenericOverloadWithoutBoxing()
    {
        var value = new FormattableValue();
        var ui = new TextMeshProUGUI();
        var world = new TextMeshPro();
        TMP_Text baseText = new TextMeshProUGUI();
        ui.SetValue(value, "vector");
        world.SetValue(value, "vector");
        baseText.SetValue(value, "vector");
        Assert.Equal("vector", ui.text);
        Assert.Equal("vector", world.text);
        Assert.Equal("vector", baseText.text);
        ui.SetValue(value);
        Assert.Equal("", ui.text);

        // The formatter returns an existing string, isolating boxing from string creation.
        for (var i = 0; i < 1000; i++)
        {
            ui.SetValue(value, "vector");
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            ui.SetValue(value, "vector");
        }
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    [Fact]
    public void GenericFormattableReferenceValuesSupportNullAndExplicitTypeArguments()
    {
        var text = new TMP_Text();
        text.SetValue<FormattableReference>(new FormattableReference(), "formatted");
        Assert.Equal("formatted", text.text);
        text.SetValue<FormattableReference>(null);
        Assert.Equal("", text.text);
    }

    [Fact]
    public void InvalidFormatsStillThrowAndSubsequentCallsWork()
    {
        var text = new TMP_Text();
        Assert.Throws<FormatException>(() => text.SetValue(123, "Q"));
        Assert.Throws<FormatException>(() => text.SetValue(Guid.Empty, "Q"));
        Assert.Throws<FormatException>(() => text.SetValue(TimeSpan.Zero, "Q"));
        text.SetValue(456);
        Assert.Equal("456", text.text);
    }

    [Fact]
    public void StackBufferBoundariesAndPooledGrowthPreserveOutput()
    {
        var text = new TMP_Text();
        foreach (var length in new[] { 64, 65, 128, 129, 1024, 1 })
        {
            var format = "'" + new string('x', length) + "'";
            text.SetValue(123, format);
            Assert.Equal(new string('x', length), text.text);
        }
    }

    [Fact]
    public void RepeatedNumericUpdatesDoNotAllocateInTheAdapter()
    {
        var text = new TextMeshProUGUI { CaptureText = false };
        for (var i = 0; i < 1000; i++)
        {
            text.SetValue(i);
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            text.SetValue(i);
        }
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    private readonly struct FormattableValue : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider) => format ?? "";

        public override string ToString() => throw new InvalidOperationException("The generic formatting overload must be used.");
    }

    private sealed class FormattableReference : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider) => format ?? "";
    }

    private sealed class NamedValue
    {
        public override string ToString() => "custom";
    }
}