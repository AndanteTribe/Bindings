#nullable enable

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using TMPro;

namespace Bindings
{
    /// <summary>
    /// Extensions for TextMeshPro components to set values with formatting.
    /// </summary>
    public static class TextMeshProExtensions
    {
        private const int StackBufferSize = 64;

        /// <summary>
        /// Copies characters to the text component without creating an intermediate string.
        /// </summary>
        public static void SetValue(this TMP_Text text, ReadOnlySpan<char> value)
        {
#if UGUI2_6_0_OR_GREATER
            text.SetText(value);
#else
            var array = ArrayPool<char>.Shared.Rent(value.Length);
            try
            {
                value.CopyTo(array);
                text.SetCharArray(array, 0, value.Length);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
#endif
        }

        public static void SetValue(this TMP_Text text, byte? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, sbyte? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, short? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, ushort? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, int? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, uint? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, long? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, ulong? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, float? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, double? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, decimal? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, DateTime? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, DateTimeOffset? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, Guid? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, TimeSpan? value, string format = "")
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value, format);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, bool? value)
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value);
            }
            else
            {
                text.text = "";
            }
        }

        public static void SetValue(this TMP_Text text, char? value)
        {
            if (value.HasValue)
            {
                SetValue(text, value.Value);
            }
            else
            {
                text.text = "";
            }
        }

        /// <summary>
        /// Formats values such as Unity vectors without boxing value-type implementations.
        /// The formatted string itself may allocate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue<T>(this TMP_Text text, T? value, string format = "") where T : IFormattable
        {
            text.text = value?.ToString(format, null) ?? "";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, object? value, string format = "")
        {
            text.text = value is IFormattable formattable
                ? formattable.ToString(format, null)
                : value?.ToString() ?? "";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, string? value)
        {
            text.text = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, bool value)
        {
            text.text = value ? "True" : "False";
        }

        public static void SetValue(this TMP_Text text, char value)
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[1];
            buffer[0] = value;
            text.SetText(buffer);
#else
            var array = ArrayPool<char>.Shared.Rent(1);
            try
            {
                array[0] = value;
                text.SetCharArray(array, 0, 1);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
#endif
        }

        public static void SetValue(this TMP_Text text, byte value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 8;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, sbyte value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 8;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, short value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 8;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, ushort value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 8;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, int value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 16;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, uint value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 16;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, long value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 24;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, ulong value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 24;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, float value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 16;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, double value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 24;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, decimal value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 32;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, DateTime value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 32;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, DateTimeOffset value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 32;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, Guid value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 36;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        public static void SetValue(this TMP_Text text, TimeSpan value, string format = "")
        {
#if UGUI2_6_0_OR_GREATER
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            var size = StackBufferSize * 2;
#else
            var size = 32;
#endif
            var array = ArrayPool<char>.Shared.Rent(size);
            try
            {
                int charsWritten;
                while (!value.TryFormat(array, out charsWritten, format))
                {
                    var toReturn = array;
                    size = array.Length * 2;
                    array = ArrayPool<char>.Shared.Rent(size);
                    ArrayPool<char>.Shared.Return(toReturn);
                }
                text.SetCharArray(array, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }
    }
}