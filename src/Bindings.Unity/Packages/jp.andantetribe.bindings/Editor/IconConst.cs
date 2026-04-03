#nullable enable

using UnityEditor;
using UnityEngine;

namespace Bindings.Editor
{
    internal static class IconConst
    {
        public static readonly Texture2D ScriptIcon = (Texture2D)EditorGUIUtility.Load("Cs Script Icon");
        public static readonly Texture2D RemoveIcon = (Texture2D)EditorGUIUtility.Load("Toolbar Minus");
    }
}