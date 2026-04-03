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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TextMeshProUGUI text, object? value)
        {
            text.text = value?.ToString() ?? "";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue<T>(this TextMeshPro text, T? value, string format = "") where T : IFormattable
        {
            text.text = value?.ToString(format, null) ?? "";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, string value)
        {
            text.text = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, bool value)
        {
            text.text = value ? "True" : "False";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, char value)
        {
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, byte value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(16);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, sbyte value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(16);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, short value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(16);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, ushort value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(16);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, int value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(32);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, uint value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(32);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, long value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(32);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, ulong value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(32);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, float value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(32);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, double value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(32);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, decimal value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(64);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, DateTime value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(64);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, DateTimeOffset value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(64);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, Guid value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(64);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, TimeSpan value, string format = "")
        {
            var array = ArrayPool<char>.Shared.Rent(64);
            try
            {
                if (value.TryFormat(array, out var charsWritten, format))
                {
                    text.SetCharArray(array, 0, charsWritten);
                }
                else
                {
                    text.text = value.ToString(format);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }
    }
}