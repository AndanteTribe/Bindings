#nullable enable

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bindings.Tests
{
    public sealed class TextMeshProExtensionsTests
    {
        private TextMeshProUGUI _text = null!;
        private CultureInfo _culture = null!;

        [SetUp]
        public void SetUp()
        {
            _culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            _text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_text.gameObject);
            CultureInfo.CurrentCulture = _culture;
        }

        private static IEnumerable FormattedValues()
        {
            yield return new TestCaseData((byte)255, "D3");
            yield return new TestCaseData((sbyte)-128, "D3");
            yield return new TestCaseData(short.MinValue, "D");
            yield return new TestCaseData(ushort.MaxValue, "D");
            yield return new TestCaseData(int.MinValue, "D");
            yield return new TestCaseData(uint.MaxValue, "D");
            yield return new TestCaseData(long.MinValue, "D");
            yield return new TestCaseData(ulong.MaxValue, "D");
            yield return new TestCaseData(1.25f, "F2");
            yield return new TestCaseData(-1.25, "F2");
            yield return new TestCaseData(decimal.MaxValue, "F2");
            yield return new TestCaseData(new DateTime(2026, 11, 16, 12, 34, 56), "O");
            yield return new TestCaseData(new DateTimeOffset(2026, 11, 16, 12, 34, 56, TimeSpan.FromHours(9)), "O");
            yield return new TestCaseData(Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"), "B");
            yield return new TestCaseData(TimeSpan.FromDays(123.5), "c");
        }

        [TestCaseSource(nameof(FormattedValues))]
        public void TypedAndNullableOverloadsFormatAndClear(object value, string format)
        {
            var type = value.GetType();
            var expected = ((IFormattable)value).ToString(format, CultureInfo.InvariantCulture);
            Invoke(type, value, format);
            Assert.That(_text.text, Is.EqualTo(expected));
            var nullable = typeof(Nullable<>).MakeGenericType(type);
            Invoke(nullable, value, format);
            Assert.That(_text.text, Is.EqualTo(expected));
            Invoke(nullable, null, format);
            Assert.That(_text.text, Is.Empty);
        }

        [TestCaseSource(nameof(FormattedValues))]
        public void LongFormatsGrowBuffersAndInvalidFormatsDoNotPoisonLaterCalls(object value, string format)
        {
            if (value is Guid)
            {
                // Even the longest supported Guid format fits the stack buffer.
                _ = Assert.Throws<TargetInvocationException>(() => Invoke(value.GetType(), value, "invalid"));
            }
            else
            {
                var longFormat = value is DateTime or DateTimeOffset or TimeSpan
                    ? "'" + new string('x', 160) + "'" : new string('0', 160);
                Invoke(value.GetType(), value, longFormat);
                Assert.That(_text.text, Is.EqualTo(((IFormattable)value).ToString(longFormat, CultureInfo.InvariantCulture)));
            }
            Invoke(value.GetType(), value, format);
            Assert.That(_text.text, Is.EqualTo(((IFormattable)value).ToString(format, CultureInfo.InvariantCulture)));
        }

        [Test]
        public void CharactersStringsAndObjectsReplaceExistingText()
        {
            _text.SetValue("hello".AsSpan());
            Assert.That(_text.text, Is.EqualTo("hello"));
            _text.SetValue(ReadOnlySpan<char>.Empty);
            Assert.That(_text.text, Is.Empty);
            _text.SetValue('猫');
            Assert.That(_text.text, Is.EqualTo("猫"));
            _text.SetValue((char?)'x');
            Assert.That(_text.text, Is.EqualTo("x"));
            _text.SetValue((char?)null);
            Assert.That(_text.text, Is.Empty);
            _text.SetValue(true);
            Assert.That(_text.text, Is.EqualTo("True"));
            _text.SetValue((bool?)false);
            Assert.That(_text.text, Is.EqualTo("False"));
            _text.SetValue((bool?)null);
            Assert.That(_text.text, Is.Empty);
            _text.SetValue("value");
            Assert.That(_text.text, Is.EqualTo("value"));
            _text.SetValue((string?)null);
            Assert.That(_text.text, Is.Null.Or.Empty);
            _text.SetValue((object)12, "D4");
            Assert.That(_text.text, Is.EqualTo("0012"));
            _text.SetValue((object)"object");
            Assert.That(_text.text, Is.EqualTo("object"));
            _text.SetValue((object?)null);
            Assert.That(_text.text, Is.Empty);
            _text.SetValue(new Vector2(1, 2), "F1");
            Assert.That(_text.text, Is.EqualTo("(1.0, 2.0)"));
            _text.SetValue<IFormattable>(null);
            Assert.That(_text.text, Is.Empty);
        }

        private void Invoke(Type type, object? value, string format)
        {
            var method = typeof(TextMeshProExtensions).GetMethods()
                .Single(m => !m.IsGenericMethod && m.Name == "SetValue" &&
                    m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == type);
            _ = method.Invoke(null, new[] { _text, value, format });
        }
    }
}