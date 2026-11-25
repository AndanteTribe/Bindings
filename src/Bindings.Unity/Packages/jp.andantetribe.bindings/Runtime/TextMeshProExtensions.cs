#nullable enable

using System;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Collections;

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this TMP_Text text, ReadOnlySpan<char> value)
        {
            text.SetText(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            var buffer = (Span<char>)stackalloc char[1];
            buffer[0] = value;
            text.SetText(buffer);
        }

        public static void SetValue(this TMP_Text text, byte value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, sbyte value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, short value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, ushort value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, int value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, uint value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, long value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, ulong value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, float value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, double value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, decimal value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, DateTime value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, DateTimeOffset value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, Guid value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }

        public static void SetValue(this TMP_Text text, TimeSpan value, string format = "")
        {
            var buffer = (Span<char>)stackalloc char[StackBufferSize];
            if (value.TryFormat(buffer, out var written, format))
            {
                text.SetText(buffer[..written]);
                return;
            }
            for (var size = StackBufferSize * 2; ; size = checked(size * 2))
            {
                using var native = new NativeArray<char>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var span = native.AsSpan();
                if (value.TryFormat(span, out written, format))
                {
                    text.SetText(span[..written]);
                    return;
                }
            }
        }
    }
}