using System;

namespace TMPro;

// Deliberately omit the span API in legacy builds to catch accidental usage.
public class TMP_Text
{
    public string? text { get; set; }
    public int SpanCalls { get; private set; }
    public int ArrayCalls { get; private set; }
    public bool CaptureText { get; set; } = true;

#if UGUI2_6_0_OR_GREATER
    public void SetText(ReadOnlySpan<char> value)
    {
        SpanCalls++;
        if (CaptureText)
        {
            text = new string(value);
        }
    }
#endif

    public void SetCharArray(char[] value, int start, int length)
    {
        ArrayCalls++;
        if (CaptureText)
        {
            text = new string(value, start, length);
        }
    }
}

public class TextMeshProUGUI : TMP_Text;
public class TextMeshPro : TMP_Text;