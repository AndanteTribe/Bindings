#nullable enable
#pragma warning disable IDE1006

using System.Diagnostics.CodeAnalysis;

namespace Bindings
{
    using G_UnityEngine = global::UnityEngine;
    using G_TMPro = global::TMPro;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class PathResolver
    {
        public static partial class UnityEngine
        {
            public static class Object
            {
                public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
            }
        }

        public static partial class UnityEngine
        {
            public static class Component
            {
                public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
            }
        }

        public static partial class UnityEngine
        {
            public static class Behaviour
            {
                public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
            }
        }

        public static partial class UnityEngine
        {
            public static class MonoBehaviour
            {
                public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class UIBehaviour
                {
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static class Canvas
            {
                public const string additionalShaderChannels = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.additionalShaderChannels);
                public const string normalizedSortingGridSize = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.normalizedSortingGridSize);
                public const string overridePixelPerfect = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.overridePixelPerfect);
                public const string overrideSorting = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.overrideSorting);
                public const string pixelPerfect = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.pixelPerfect);
                public const string planeDistance = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.planeDistance);
                public const string referencePixelsPerUnit = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.referencePixelsPerUnit);
                public const string renderMode = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.renderMode);
                public const string scaleFactor = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.scaleFactor);
                public const string sortingLayerID = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.sortingLayerID);
                public const string sortingLayerName = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.sortingLayerName);
                public const string sortingOrder = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.sortingOrder);
                public const string targetDisplay = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.targetDisplay);
                public const string updateRectTransformForStandalone = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.updateRectTransformForStandalone);
                public const string vertexColorAlwaysGammaSpace = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.vertexColorAlwaysGammaSpace);
                public const string worldCamera = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Canvas) + "." + nameof(G_UnityEngine.Canvas.worldCamera);
                public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
            }
        }

        public static partial class UnityEngine
        {
            public static class CanvasGroup
            {
                public const string alpha = nameof(UnityEngine) + "." + nameof(G_UnityEngine.CanvasGroup) + "." + nameof(G_UnityEngine.CanvasGroup.alpha);
                public const string blocksRaycasts = nameof(UnityEngine) + "." + nameof(G_UnityEngine.CanvasGroup) + "." + nameof(G_UnityEngine.CanvasGroup.blocksRaycasts);
                public const string ignoreParentGroups = nameof(UnityEngine) + "." + nameof(G_UnityEngine.CanvasGroup) + "." + nameof(G_UnityEngine.CanvasGroup.ignoreParentGroups);
                public const string interactable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.CanvasGroup) + "." + nameof(G_UnityEngine.CanvasGroup.interactable);
                public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
            }
        }

        public static partial class UnityEngine
        {
            public static class CanvasRenderer
            {
                public const string clippingSoftness = nameof(UnityEngine) + "." + nameof(G_UnityEngine.CanvasRenderer) + "." + nameof(G_UnityEngine.CanvasRenderer.clippingSoftness);
                public const string cull = nameof(UnityEngine) + "." + nameof(G_UnityEngine.CanvasRenderer) + "." + nameof(G_UnityEngine.CanvasRenderer.cull);
                public const string cullTransparentMesh = nameof(UnityEngine) + "." + nameof(G_UnityEngine.CanvasRenderer) + "." + nameof(G_UnityEngine.CanvasRenderer.cullTransparentMesh);
                public const string hasPopInstruction = nameof(UnityEngine) + "." + nameof(G_UnityEngine.CanvasRenderer) + "." + nameof(G_UnityEngine.CanvasRenderer.hasPopInstruction);
                public const string materialCount = nameof(UnityEngine) + "." + nameof(G_UnityEngine.CanvasRenderer) + "." + nameof(G_UnityEngine.CanvasRenderer.materialCount);
                public const string popMaterialCount = nameof(UnityEngine) + "." + nameof(G_UnityEngine.CanvasRenderer) + "." + nameof(G_UnityEngine.CanvasRenderer.popMaterialCount);
                public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
            }
        }

        public static partial class UnityEngine
        {
            public static class Transform
            {
                public const string eulerAngles = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.eulerAngles);
                public const string forward = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.forward);
                public const string hasChanged = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.hasChanged);
                public const string hierarchyCapacity = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.hierarchyCapacity);
                public const string localEulerAngles = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.localEulerAngles);
                public const string localPosition = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.localPosition);
                public const string localRotation = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.localRotation);
                public const string localScale = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.localScale);
                public const string parent = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.parent);
                public const string position = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.position);
                public const string right = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.right);
                public const string rotation = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.rotation);
                public const string up = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.up);
                public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
            }
        }

        public static partial class UnityEngine
        {
            public static class RectTransform
            {
                public const string anchoredPosition = nameof(UnityEngine) + "." + nameof(G_UnityEngine.RectTransform) + "." + nameof(G_UnityEngine.RectTransform.anchoredPosition);
                public const string anchoredPosition3D = nameof(UnityEngine) + "." + nameof(G_UnityEngine.RectTransform) + "." + nameof(G_UnityEngine.RectTransform.anchoredPosition3D);
                public const string anchorMax = nameof(UnityEngine) + "." + nameof(G_UnityEngine.RectTransform) + "." + nameof(G_UnityEngine.RectTransform.anchorMax);
                public const string anchorMin = nameof(UnityEngine) + "." + nameof(G_UnityEngine.RectTransform) + "." + nameof(G_UnityEngine.RectTransform.anchorMin);
                public const string offsetMax = nameof(UnityEngine) + "." + nameof(G_UnityEngine.RectTransform) + "." + nameof(G_UnityEngine.RectTransform.offsetMax);
                public const string offsetMin = nameof(UnityEngine) + "." + nameof(G_UnityEngine.RectTransform) + "." + nameof(G_UnityEngine.RectTransform.offsetMin);
                public const string pivot = nameof(UnityEngine) + "." + nameof(G_UnityEngine.RectTransform) + "." + nameof(G_UnityEngine.RectTransform.pivot);
                public const string sizeDelta = nameof(UnityEngine) + "." + nameof(G_UnityEngine.RectTransform) + "." + nameof(G_UnityEngine.RectTransform.sizeDelta);
                public const string eulerAngles = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.eulerAngles);
                public const string forward = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.forward);
                public const string hasChanged = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.hasChanged);
                public const string hierarchyCapacity = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.hierarchyCapacity);
                public const string localEulerAngles = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.localEulerAngles);
                public const string localPosition = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.localPosition);
                public const string localRotation = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.localRotation);
                public const string localScale = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.localScale);
                public const string parent = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.parent);
                public const string position = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.position);
                public const string right = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.right);
                public const string rotation = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.rotation);
                public const string up = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Transform) + "." + nameof(G_UnityEngine.Transform.up);
                public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class AspectRatioFitter
                {
                    public const string aspectMode = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.AspectRatioFitter) + "." + nameof(G_UnityEngine.UI.AspectRatioFitter.aspectMode);
                    public const string aspectRatio = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.AspectRatioFitter) + "." + nameof(G_UnityEngine.UI.AspectRatioFitter.aspectRatio);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class Button
                {
                    public const string onClick = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Button) + "." + nameof(G_UnityEngine.UI.Button.onClick);
                    public const string colors = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.colors);
                    public const string interactable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.interactable);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class CanvasScaler
                {
                    public const string defaultSpriteDPI = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.CanvasScaler) + "." + nameof(G_UnityEngine.UI.CanvasScaler.defaultSpriteDPI);
                    public const string dynamicPixelsPerUnit = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.CanvasScaler) + "." + nameof(G_UnityEngine.UI.CanvasScaler.dynamicPixelsPerUnit);
                    public const string fallbackScreenDPI = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.CanvasScaler) + "." + nameof(G_UnityEngine.UI.CanvasScaler.fallbackScreenDPI);
                    public const string matchWidthOrHeight = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.CanvasScaler) + "." + nameof(G_UnityEngine.UI.CanvasScaler.matchWidthOrHeight);
                    public const string physicalUnit = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.CanvasScaler) + "." + nameof(G_UnityEngine.UI.CanvasScaler.physicalUnit);
                    public const string referencePixelsPerUnit = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.CanvasScaler) + "." + nameof(G_UnityEngine.UI.CanvasScaler.referencePixelsPerUnit);
                    public const string referenceResolution = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.CanvasScaler) + "." + nameof(G_UnityEngine.UI.CanvasScaler.referenceResolution);
                    public const string scaleFactor = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.CanvasScaler) + "." + nameof(G_UnityEngine.UI.CanvasScaler.scaleFactor);
                    public const string screenMatchMode = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.CanvasScaler) + "." + nameof(G_UnityEngine.UI.CanvasScaler.screenMatchMode);
                    public const string uiScaleMode = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.CanvasScaler) + "." + nameof(G_UnityEngine.UI.CanvasScaler.uiScaleMode);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class ContentSizeFitter
                {
                    public const string horizontalFit = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ContentSizeFitter) + "." + nameof(G_UnityEngine.UI.ContentSizeFitter.horizontalFit);
                    public const string verticalFit = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ContentSizeFitter) + "." + nameof(G_UnityEngine.UI.ContentSizeFitter.verticalFit);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class Dropdown
                {
                    public const string alphaFadeSpeed = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Dropdown) + "." + nameof(G_UnityEngine.UI.Dropdown.alphaFadeSpeed);
                    public const string captionImage = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Dropdown) + "." + nameof(G_UnityEngine.UI.Dropdown.captionImage);
                    public const string captionText = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Dropdown) + "." + nameof(G_UnityEngine.UI.Dropdown.captionText);
                    public const string itemImage = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Dropdown) + "." + nameof(G_UnityEngine.UI.Dropdown.itemImage);
                    public const string itemText = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Dropdown) + "." + nameof(G_UnityEngine.UI.Dropdown.itemText);
                    public const string onValueChanged = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Dropdown) + "." + nameof(G_UnityEngine.UI.Dropdown.onValueChanged);
                    public const string template = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Dropdown) + "." + nameof(G_UnityEngine.UI.Dropdown.template);
                    public const string value = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Dropdown) + "." + nameof(G_UnityEngine.UI.Dropdown.value);
                    public const string colors = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.colors);
                    public const string interactable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.interactable);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class Graphic
                {
                    public const string color = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.color);
                    public const string material = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.material);
                    public const string raycastPadding = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.raycastPadding);
                    public const string raycastTarget = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.raycastTarget);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class GraphicRaycaster
                {
                    public const string blockingMask = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.GraphicRaycaster) + "." + nameof(G_UnityEngine.UI.GraphicRaycaster.blockingMask);
                    public const string blockingObjects = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.GraphicRaycaster) + "." + nameof(G_UnityEngine.UI.GraphicRaycaster.blockingObjects);
                    public const string ignoreReversedGraphics = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.GraphicRaycaster) + "." + nameof(G_UnityEngine.UI.GraphicRaycaster.ignoreReversedGraphics);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class GridLayoutGroup
                {
                    public const string cellSize = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.GridLayoutGroup) + "." + nameof(G_UnityEngine.UI.GridLayoutGroup.cellSize);
                    public const string constraint = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.GridLayoutGroup) + "." + nameof(G_UnityEngine.UI.GridLayoutGroup.constraint);
                    public const string constraintCount = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.GridLayoutGroup) + "." + nameof(G_UnityEngine.UI.GridLayoutGroup.constraintCount);
                    public const string spacing = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.GridLayoutGroup) + "." + nameof(G_UnityEngine.UI.GridLayoutGroup.spacing);
                    public const string startAxis = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.GridLayoutGroup) + "." + nameof(G_UnityEngine.UI.GridLayoutGroup.startAxis);
                    public const string startCorner = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.GridLayoutGroup) + "." + nameof(G_UnityEngine.UI.GridLayoutGroup.startCorner);
                    public const string childAlignment = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutGroup) + "." + nameof(G_UnityEngine.UI.LayoutGroup.childAlignment);
                    public const string padding = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutGroup) + "." + nameof(G_UnityEngine.UI.LayoutGroup.padding);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class HorizontalLayoutGroup
                {
                    public const string childControlHeight = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childControlHeight);
                    public const string childControlWidth = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childControlWidth);
                    public const string childForceExpandHeight = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childForceExpandHeight);
                    public const string childForceExpandWidth = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childForceExpandWidth);
                    public const string childScaleHeight = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childScaleHeight);
                    public const string childScaleWidth = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childScaleWidth);
                    public const string reverseArrangement = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.reverseArrangement);
                    public const string spacing = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.spacing);
                    public const string childAlignment = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutGroup) + "." + nameof(G_UnityEngine.UI.LayoutGroup.childAlignment);
                    public const string padding = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutGroup) + "." + nameof(G_UnityEngine.UI.LayoutGroup.padding);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class HorizontalOrVerticalLayoutGroup
                {
                    public const string childControlHeight = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childControlHeight);
                    public const string childControlWidth = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childControlWidth);
                    public const string childForceExpandHeight = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childForceExpandHeight);
                    public const string childForceExpandWidth = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childForceExpandWidth);
                    public const string childScaleHeight = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childScaleHeight);
                    public const string childScaleWidth = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childScaleWidth);
                    public const string reverseArrangement = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.reverseArrangement);
                    public const string spacing = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.spacing);
                    public const string childAlignment = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutGroup) + "." + nameof(G_UnityEngine.UI.LayoutGroup.childAlignment);
                    public const string padding = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutGroup) + "." + nameof(G_UnityEngine.UI.LayoutGroup.padding);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class Image
                {
                    public const string alphaHitTestMinimumThreshold = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Image) + "." + nameof(G_UnityEngine.UI.Image.alphaHitTestMinimumThreshold);
                    public const string fillAmount = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Image) + "." + nameof(G_UnityEngine.UI.Image.fillAmount);
                    public const string fillCenter = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Image) + "." + nameof(G_UnityEngine.UI.Image.fillCenter);
                    public const string fillClockwise = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Image) + "." + nameof(G_UnityEngine.UI.Image.fillClockwise);
                    public const string fillMethod = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Image) + "." + nameof(G_UnityEngine.UI.Image.fillMethod);
                    public const string fillOrigin = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Image) + "." + nameof(G_UnityEngine.UI.Image.fillOrigin);
                    public const string overrideSprite = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Image) + "." + nameof(G_UnityEngine.UI.Image.overrideSprite);
                    public const string pixelsPerUnitMultiplier = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Image) + "." + nameof(G_UnityEngine.UI.Image.pixelsPerUnitMultiplier);
                    public const string preserveAspect = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Image) + "." + nameof(G_UnityEngine.UI.Image.preserveAspect);
                    public const string sprite = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Image) + "." + nameof(G_UnityEngine.UI.Image.sprite);
                    public const string type = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Image) + "." + nameof(G_UnityEngine.UI.Image.type);
                    public const string useSpriteMesh = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Image) + "." + nameof(G_UnityEngine.UI.Image.useSpriteMesh);
                    [System.Obsolete("eventAlphaThreshold has been deprecated. Use eventMinimumAlphaThreshold instead (UnityUpgradable) -> alphaHitTestMinimumThreshold")]
                    public const string eventAlphaThreshold = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Image) + "." + nameof(G_UnityEngine.UI.Image.eventAlphaThreshold);
                    public const string isMaskingGraphic = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.isMaskingGraphic);
                    public const string maskable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.maskable);
                    public const string onCullStateChanged = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.onCullStateChanged);
                    public const string color = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.color);
                    public const string material = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.material);
                    public const string raycastPadding = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.raycastPadding);
                    public const string raycastTarget = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.raycastTarget);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class InputField
                {
                    public const string asteriskChar = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.asteriskChar);
                    public const string caretBlinkRate = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.caretBlinkRate);
                    public const string caretColor = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.caretColor);
                    public const string caretPosition = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.caretPosition);
                    public const string caretWidth = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.caretWidth);
                    public const string characterLimit = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.characterLimit);
                    public const string characterValidation = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.characterValidation);
                    public const string contentType = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.contentType);
                    public const string customCaretColor = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.customCaretColor);
                    public const string inputType = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.inputType);
                    public const string keyboardType = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.keyboardType);
                    public const string lineType = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.lineType);
                    public const string onEndEdit = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.onEndEdit);
                    public const string onSubmit = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.onSubmit);
                    public const string onValidateInput = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.onValidateInput);
                    public const string placeholder = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.placeholder);
                    public const string readOnly = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.readOnly);
                    public const string selectionAnchorPosition = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.selectionAnchorPosition);
                    public const string selectionColor = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.selectionColor);
                    public const string selectionFocusPosition = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.selectionFocusPosition);
                    public const string shouldActivateOnSelect = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.shouldActivateOnSelect);
                    public const string shouldHideMobileInput = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.shouldHideMobileInput);
                    public const string text = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.text);
                    public const string textComponent = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.textComponent);
                    [System.Obsolete("onValueChange has been renamed to onValueChanged")]
                    public const string onValueChange = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.InputField) + "." + nameof(G_UnityEngine.UI.InputField.onValueChange);
                    public const string colors = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.colors);
                    public const string interactable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.interactable);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class LayoutElement
                {
                    public const string flexibleHeight = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutElement) + "." + nameof(G_UnityEngine.UI.LayoutElement.flexibleHeight);
                    public const string flexibleWidth = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutElement) + "." + nameof(G_UnityEngine.UI.LayoutElement.flexibleWidth);
                    public const string ignoreLayout = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutElement) + "." + nameof(G_UnityEngine.UI.LayoutElement.ignoreLayout);
                    public const string layoutPriority = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutElement) + "." + nameof(G_UnityEngine.UI.LayoutElement.layoutPriority);
                    public const string minHeight = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutElement) + "." + nameof(G_UnityEngine.UI.LayoutElement.minHeight);
                    public const string minWidth = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutElement) + "." + nameof(G_UnityEngine.UI.LayoutElement.minWidth);
                    public const string preferredHeight = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutElement) + "." + nameof(G_UnityEngine.UI.LayoutElement.preferredHeight);
                    public const string preferredWidth = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutElement) + "." + nameof(G_UnityEngine.UI.LayoutElement.preferredWidth);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class LayoutGroup
                {
                    public const string childAlignment = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutGroup) + "." + nameof(G_UnityEngine.UI.LayoutGroup.childAlignment);
                    public const string padding = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutGroup) + "." + nameof(G_UnityEngine.UI.LayoutGroup.padding);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class Mask
                {
                    public const string showMaskGraphic = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Mask) + "." + nameof(G_UnityEngine.UI.Mask.showMaskGraphic);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class Outline
                {
                    public const string effectColor = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Shadow) + "." + nameof(G_UnityEngine.UI.Shadow.effectColor);
                    public const string effectDistance = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Shadow) + "." + nameof(G_UnityEngine.UI.Shadow.effectDistance);
                    public const string useGraphicAlpha = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Shadow) + "." + nameof(G_UnityEngine.UI.Shadow.useGraphicAlpha);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class MaskableGraphic
                {
                    public const string isMaskingGraphic = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.isMaskingGraphic);
                    public const string maskable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.maskable);
                    public const string onCullStateChanged = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.onCullStateChanged);
                    public const string color = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.color);
                    public const string material = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.material);
                    public const string raycastPadding = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.raycastPadding);
                    public const string raycastTarget = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.raycastTarget);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class RawImage
                {
                    public const string texture = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.RawImage) + "." + nameof(G_UnityEngine.UI.RawImage.texture);
                    public const string uvRect = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.RawImage) + "." + nameof(G_UnityEngine.UI.RawImage.uvRect);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class RectMask2D
                {
                    public const string padding = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.RectMask2D) + "." + nameof(G_UnityEngine.UI.RectMask2D.padding);
                    public const string softness = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.RectMask2D) + "." + nameof(G_UnityEngine.UI.RectMask2D.softness);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class ScrollRect
                {
                    public const string content = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.content);
                    public const string decelerationRate = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.decelerationRate);
                    public const string elasticity = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.elasticity);
                    public const string horizontal = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.horizontal);
                    public const string horizontalNormalizedPosition = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.horizontalNormalizedPosition);
                    public const string horizontalScrollbar = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.horizontalScrollbar);
                    public const string horizontalScrollbarSpacing = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.horizontalScrollbarSpacing);
                    public const string horizontalScrollbarVisibility = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.horizontalScrollbarVisibility);
                    public const string inertia = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.inertia);
                    public const string movementType = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.movementType);
                    public const string normalizedPosition = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.normalizedPosition);
                    public const string onValueChanged = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.onValueChanged);
                    public const string scrollSensitivity = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.scrollSensitivity);
                    public const string velocity = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.velocity);
                    public const string vertical = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.vertical);
                    public const string verticalNormalizedPosition = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.verticalNormalizedPosition);
                    public const string verticalScrollbar = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.verticalScrollbar);
                    public const string verticalScrollbarSpacing = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.verticalScrollbarSpacing);
                    public const string verticalScrollbarVisibility = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.verticalScrollbarVisibility);
                    public const string viewport = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ScrollRect) + "." + nameof(G_UnityEngine.UI.ScrollRect.viewport);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class Scrollbar
                {
                    public const string direction = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Scrollbar) + "." + nameof(G_UnityEngine.UI.Scrollbar.direction);
                    public const string handleRect = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Scrollbar) + "." + nameof(G_UnityEngine.UI.Scrollbar.handleRect);
                    public const string numberOfSteps = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Scrollbar) + "." + nameof(G_UnityEngine.UI.Scrollbar.numberOfSteps);
                    public const string onValueChanged = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Scrollbar) + "." + nameof(G_UnityEngine.UI.Scrollbar.onValueChanged);
                    public const string size = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Scrollbar) + "." + nameof(G_UnityEngine.UI.Scrollbar.size);
                    public const string value = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Scrollbar) + "." + nameof(G_UnityEngine.UI.Scrollbar.value);
                    public const string colors = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.colors);
                    public const string interactable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.interactable);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class Selectable
                {
                    public const string colors = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.colors);
                    public const string interactable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.interactable);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class Shadow
                {
                    public const string effectColor = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Shadow) + "." + nameof(G_UnityEngine.UI.Shadow.effectColor);
                    public const string effectDistance = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Shadow) + "." + nameof(G_UnityEngine.UI.Shadow.effectDistance);
                    public const string useGraphicAlpha = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Shadow) + "." + nameof(G_UnityEngine.UI.Shadow.useGraphicAlpha);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class Slider
                {
                    public const string direction = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Slider) + "." + nameof(G_UnityEngine.UI.Slider.direction);
                    public const string fillRect = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Slider) + "." + nameof(G_UnityEngine.UI.Slider.fillRect);
                    public const string handleRect = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Slider) + "." + nameof(G_UnityEngine.UI.Slider.handleRect);
                    public const string maxValue = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Slider) + "." + nameof(G_UnityEngine.UI.Slider.maxValue);
                    public const string minValue = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Slider) + "." + nameof(G_UnityEngine.UI.Slider.minValue);
                    public const string normalizedValue = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Slider) + "." + nameof(G_UnityEngine.UI.Slider.normalizedValue);
                    public const string onValueChanged = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Slider) + "." + nameof(G_UnityEngine.UI.Slider.onValueChanged);
                    public const string value = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Slider) + "." + nameof(G_UnityEngine.UI.Slider.value);
                    public const string wholeNumbers = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Slider) + "." + nameof(G_UnityEngine.UI.Slider.wholeNumbers);
                    public const string colors = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.colors);
                    public const string interactable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.interactable);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class Text
                {
                    public const string alignByGeometry = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Text) + "." + nameof(G_UnityEngine.UI.Text.alignByGeometry);
                    public const string alignment = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Text) + "." + nameof(G_UnityEngine.UI.Text.alignment);
                    public const string font = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Text) + "." + nameof(G_UnityEngine.UI.Text.font);
                    public const string fontSize = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Text) + "." + nameof(G_UnityEngine.UI.Text.fontSize);
                    public const string fontStyle = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Text) + "." + nameof(G_UnityEngine.UI.Text.fontStyle);
                    public const string horizontalOverflow = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Text) + "." + nameof(G_UnityEngine.UI.Text.horizontalOverflow);
                    public const string lineSpacing = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Text) + "." + nameof(G_UnityEngine.UI.Text.lineSpacing);
                    public const string resizeTextForBestFit = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Text) + "." + nameof(G_UnityEngine.UI.Text.resizeTextForBestFit);
                    public const string resizeTextMaxSize = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Text) + "." + nameof(G_UnityEngine.UI.Text.resizeTextMaxSize);
                    public const string resizeTextMinSize = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Text) + "." + nameof(G_UnityEngine.UI.Text.resizeTextMinSize);
                    public const string supportRichText = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Text) + "." + nameof(G_UnityEngine.UI.Text.supportRichText);
                    public const string text = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Text) + "." + nameof(G_UnityEngine.UI.Text.text);
                    public const string verticalOverflow = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Text) + "." + nameof(G_UnityEngine.UI.Text.verticalOverflow);
                    public const string isMaskingGraphic = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.isMaskingGraphic);
                    public const string maskable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.maskable);
                    public const string onCullStateChanged = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.onCullStateChanged);
                    public const string color = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.color);
                    public const string material = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.material);
                    public const string raycastPadding = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.raycastPadding);
                    public const string raycastTarget = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.raycastTarget);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class Toggle
                {
                    public const string group = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Toggle) + "." + nameof(G_UnityEngine.UI.Toggle.group);
                    public const string isOn = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Toggle) + "." + nameof(G_UnityEngine.UI.Toggle.isOn);
                    public const string colors = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.colors);
                    public const string interactable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.interactable);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class ToggleGroup
                {
                    public const string allowSwitchOff = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.ToggleGroup) + "." + nameof(G_UnityEngine.UI.ToggleGroup.allowSwitchOff);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class UnityEngine
        {
            public static partial class UI
            {
                public static class VerticalLayoutGroup
                {
                    public const string childControlHeight = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childControlHeight);
                    public const string childControlWidth = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childControlWidth);
                    public const string childForceExpandHeight = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childForceExpandHeight);
                    public const string childForceExpandWidth = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childForceExpandWidth);
                    public const string childScaleHeight = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childScaleHeight);
                    public const string childScaleWidth = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.childScaleWidth);
                    public const string reverseArrangement = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.reverseArrangement);
                    public const string spacing = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup) + "." + nameof(G_UnityEngine.UI.HorizontalOrVerticalLayoutGroup.spacing);
                    public const string childAlignment = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutGroup) + "." + nameof(G_UnityEngine.UI.LayoutGroup.childAlignment);
                    public const string padding = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.LayoutGroup) + "." + nameof(G_UnityEngine.UI.LayoutGroup.padding);
                    public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                    public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                    public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                    public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
                }
            }
        }

        public static partial class TMPro
        {
            public static class TMP_Dropdown
            {
                public const string MultiSelect = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Dropdown) + "." + nameof(G_TMPro.TMP_Dropdown.MultiSelect);
                public const string alphaFadeSpeed = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Dropdown) + "." + nameof(G_TMPro.TMP_Dropdown.alphaFadeSpeed);
                public const string captionImage = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Dropdown) + "." + nameof(G_TMPro.TMP_Dropdown.captionImage);
                public const string captionText = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Dropdown) + "." + nameof(G_TMPro.TMP_Dropdown.captionText);
                public const string itemImage = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Dropdown) + "." + nameof(G_TMPro.TMP_Dropdown.itemImage);
                public const string itemText = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Dropdown) + "." + nameof(G_TMPro.TMP_Dropdown.itemText);
                public const string onValueChanged = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Dropdown) + "." + nameof(G_TMPro.TMP_Dropdown.onValueChanged);
                public const string options = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Dropdown) + "." + nameof(G_TMPro.TMP_Dropdown.options);
                public const string placeholder = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Dropdown) + "." + nameof(G_TMPro.TMP_Dropdown.placeholder);
                public const string template = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Dropdown) + "." + nameof(G_TMPro.TMP_Dropdown.template);
                public const string value = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Dropdown) + "." + nameof(G_TMPro.TMP_Dropdown.value);
                public const string colors = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.colors);
                public const string interactable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.interactable);
                public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
            }
        }

        public static partial class TMPro
        {
            public static class TMP_InputField
            {
                public const string asteriskChar = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.asteriskChar);
                public const string caretBlinkRate = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.caretBlinkRate);
                public const string caretColor = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.caretColor);
                public const string caretPosition = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.caretPosition);
                public const string caretWidth = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.caretWidth);
                public const string characterLimit = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.characterLimit);
                public const string characterValidation = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.characterValidation);
                public const string contentType = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.contentType);
                public const string customCaretColor = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.customCaretColor);
                public const string fontAsset = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.fontAsset);
                public const string inputType = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.inputType);
                public const string inputValidator = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.inputValidator);
                public const string isRichTextEditingAllowed = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.isRichTextEditingAllowed);
                public const string keepTextSelectionVisible = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.keepTextSelectionVisible);
                public const string keyboardType = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.keyboardType);
                public const string lineLimit = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.lineLimit);
                public const string lineType = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.lineType);
                public const string onDeselect = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.onDeselect);
                public const string onEndEdit = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.onEndEdit);
                public const string onEndTextSelection = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.onEndTextSelection);
                public const string onFocusSelectAll = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.onFocusSelectAll);
                public const string onSelect = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.onSelect);
                public const string onSubmit = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.onSubmit);
                public const string onTextSelection = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.onTextSelection);
                public const string onTouchScreenKeyboardStatusChanged = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.onTouchScreenKeyboardStatusChanged);
                public const string onValidateInput = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.onValidateInput);
                public const string onValueChanged = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.onValueChanged);
                public const string placeholder = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.placeholder);
                public const string pointSize = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.pointSize);
                public const string readOnly = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.readOnly);
                public const string resetOnDeActivation = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.resetOnDeActivation);
                public const string restoreOriginalTextOnEscape = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.restoreOriginalTextOnEscape);
                public const string richText = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.richText);
                public const string scrollSensitivity = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.scrollSensitivity);
                public const string selectionAnchorPosition = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.selectionAnchorPosition);
                public const string selectionColor = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.selectionColor);
                public const string selectionFocusPosition = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.selectionFocusPosition);
                public const string selectionStringAnchorPosition = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.selectionStringAnchorPosition);
                public const string selectionStringFocusPosition = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.selectionStringFocusPosition);
                public const string shouldActivateOnSelect = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.shouldActivateOnSelect);
                public const string shouldHideMobileInput = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.shouldHideMobileInput);
                public const string shouldHideSoftKeyboard = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.shouldHideSoftKeyboard);
                public const string stringPosition = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.stringPosition);
                public const string text = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.text);
                public const string textComponent = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.textComponent);
                public const string textViewport = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.textViewport);
                public const string verticalScrollbar = nameof(TMPro) + "." + nameof(G_TMPro.TMP_InputField) + "." + nameof(G_TMPro.TMP_InputField.verticalScrollbar);
                public const string colors = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.colors);
                public const string interactable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Selectable) + "." + nameof(G_UnityEngine.UI.Selectable.interactable);
                public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
            }
        }

        public static partial class TMPro
        {
            public static class TMP_Text
            {
                public const string alignment = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.alignment);
                public const string alpha = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.alpha);
                public const string autoSizeTextContainer = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.autoSizeTextContainer);
                public const string characterSpacing = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.characterSpacing);
                public const string characterWidthAdjustment = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.characterWidthAdjustment);
                public const string colorGradient = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.colorGradient);
                public const string colorGradientPreset = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.colorGradientPreset);
                public const string emojiFallbackSupport = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.emojiFallbackSupport);
                public const string enableAutoSizing = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.enableAutoSizing);
                public const string enableCulling = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.enableCulling);
                public const string enableVertexGradient = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.enableVertexGradient);
                public const string extraPadding = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.extraPadding);
                public const string faceColor = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.faceColor);
                public const string firstVisibleCharacter = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.firstVisibleCharacter);
                public const string font = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.font);
                public const string fontFeatures = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontFeatures);
                public const string fontMaterial = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontMaterial);
                public const string fontMaterials = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontMaterials);
                public const string fontSharedMaterial = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontSharedMaterial);
                public const string fontSharedMaterials = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontSharedMaterials);
                public const string fontSize = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontSize);
                public const string fontSizeMax = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontSizeMax);
                public const string fontSizeMin = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontSizeMin);
                public const string fontStyle = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontStyle);
                public const string fontWeight = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontWeight);
                public const string geometrySortingOrder = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.geometrySortingOrder);
                public const string havePropertiesChanged = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.havePropertiesChanged);
                public const string horizontalAlignment = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.horizontalAlignment);
                public const string horizontalMapping = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.horizontalMapping);
                public const string ignoreVisibility = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.ignoreVisibility);
                public const string isOrthographic = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.isOrthographic);
                public const string isOverlay = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.isOverlay);
                public const string isRightToLeftText = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.isRightToLeftText);
                public const string isTextObjectScaleStatic = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.isTextObjectScaleStatic);
                public const string isUsingLegacyAnimationComponent = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.isUsingLegacyAnimationComponent);
                public const string isVolumetricText = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.isVolumetricText);
                public const string lineSpacing = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.lineSpacing);
                public const string lineSpacingAdjustment = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.lineSpacingAdjustment);
                public const string linkedTextComponent = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.linkedTextComponent);
                public const string mappingUvLineOffset = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.mappingUvLineOffset);
                public const string margin = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.margin);
                public const string maxHeight = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.maxHeight);
                public const string maxVisibleCharacters = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.maxVisibleCharacters);
                public const string maxVisibleLines = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.maxVisibleLines);
                public const string maxVisibleWords = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.maxVisibleWords);
                public const string outlineColor = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.outlineColor);
                public const string outlineWidth = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.outlineWidth);
                public const string overflowMode = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.overflowMode);
                public const string overrideColorTags = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.overrideColorTags);
                public const string pageToDisplay = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.pageToDisplay);
                public const string paragraphSpacing = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.paragraphSpacing);
                public const string parseCtrlCharacters = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.parseCtrlCharacters);
                public const string renderMode = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.renderMode);
                public const string richText = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.richText);
                public const string spriteAsset = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.spriteAsset);
                public const string styleSheet = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.styleSheet);
                public const string text = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.text);
                public const string textPreprocessor = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.textPreprocessor);
                public const string textStyle = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.textStyle);
                public const string textWrappingMode = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.textWrappingMode);
                public const string tintAllSprites = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.tintAllSprites);
                public const string useMaxVisibleDescender = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.useMaxVisibleDescender);
                public const string vertexBufferAutoSizeReduction = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.vertexBufferAutoSizeReduction);
                public const string verticalAlignment = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.verticalAlignment);
                public const string verticalMapping = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.verticalMapping);
                public const string wordSpacing = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.wordSpacing);
                public const string wordWrappingRatios = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.wordWrappingRatios);
                [System.Obsolete("The \"enableKerning\" property has been deprecated. Use the \"fontFeatures\" property to control what features are enabled on the text component.")]
                public const string enableKerning = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.enableKerning);
                [System.Obsolete("The enabledWordWrapping property is now obsolete. Please use the textWrappingMode property instead.")]
                public const string enableWordWrapping = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.enableWordWrapping);
                public const string isMaskingGraphic = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.isMaskingGraphic);
                public const string maskable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.maskable);
                public const string onCullStateChanged = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.onCullStateChanged);
                public const string color = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.color);
                public const string material = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.material);
                public const string raycastPadding = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.raycastPadding);
                public const string raycastTarget = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.raycastTarget);
                public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
            }
        }

        public static partial class TMPro
        {
            public static class TextMeshProUGUI
            {
                public const string maskOffset = nameof(TMPro) + "." + nameof(G_TMPro.TextMeshProUGUI) + "." + nameof(G_TMPro.TextMeshProUGUI.maskOffset);
                public const string alignment = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.alignment);
                public const string alpha = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.alpha);
                public const string autoSizeTextContainer = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.autoSizeTextContainer);
                public const string characterSpacing = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.characterSpacing);
                public const string characterWidthAdjustment = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.characterWidthAdjustment);
                public const string colorGradient = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.colorGradient);
                public const string colorGradientPreset = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.colorGradientPreset);
                public const string emojiFallbackSupport = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.emojiFallbackSupport);
                public const string enableAutoSizing = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.enableAutoSizing);
                public const string enableCulling = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.enableCulling);
                public const string enableVertexGradient = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.enableVertexGradient);
                public const string extraPadding = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.extraPadding);
                public const string faceColor = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.faceColor);
                public const string firstVisibleCharacter = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.firstVisibleCharacter);
                public const string font = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.font);
                public const string fontFeatures = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontFeatures);
                public const string fontMaterial = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontMaterial);
                public const string fontMaterials = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontMaterials);
                public const string fontSharedMaterial = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontSharedMaterial);
                public const string fontSharedMaterials = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontSharedMaterials);
                public const string fontSize = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontSize);
                public const string fontSizeMax = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontSizeMax);
                public const string fontSizeMin = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontSizeMin);
                public const string fontStyle = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontStyle);
                public const string fontWeight = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.fontWeight);
                public const string geometrySortingOrder = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.geometrySortingOrder);
                public const string havePropertiesChanged = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.havePropertiesChanged);
                public const string horizontalAlignment = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.horizontalAlignment);
                public const string horizontalMapping = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.horizontalMapping);
                public const string ignoreVisibility = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.ignoreVisibility);
                public const string isOrthographic = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.isOrthographic);
                public const string isOverlay = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.isOverlay);
                public const string isRightToLeftText = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.isRightToLeftText);
                public const string isTextObjectScaleStatic = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.isTextObjectScaleStatic);
                public const string isUsingLegacyAnimationComponent = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.isUsingLegacyAnimationComponent);
                public const string isVolumetricText = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.isVolumetricText);
                public const string lineSpacing = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.lineSpacing);
                public const string lineSpacingAdjustment = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.lineSpacingAdjustment);
                public const string linkedTextComponent = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.linkedTextComponent);
                public const string mappingUvLineOffset = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.mappingUvLineOffset);
                public const string margin = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.margin);
                public const string maxHeight = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.maxHeight);
                public const string maxVisibleCharacters = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.maxVisibleCharacters);
                public const string maxVisibleLines = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.maxVisibleLines);
                public const string maxVisibleWords = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.maxVisibleWords);
                public const string outlineColor = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.outlineColor);
                public const string outlineWidth = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.outlineWidth);
                public const string overflowMode = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.overflowMode);
                public const string overrideColorTags = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.overrideColorTags);
                public const string pageToDisplay = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.pageToDisplay);
                public const string paragraphSpacing = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.paragraphSpacing);
                public const string parseCtrlCharacters = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.parseCtrlCharacters);
                public const string renderMode = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.renderMode);
                public const string richText = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.richText);
                public const string spriteAsset = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.spriteAsset);
                public const string styleSheet = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.styleSheet);
                public const string text = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.text);
                public const string textPreprocessor = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.textPreprocessor);
                public const string textStyle = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.textStyle);
                public const string textWrappingMode = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.textWrappingMode);
                public const string tintAllSprites = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.tintAllSprites);
                public const string useMaxVisibleDescender = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.useMaxVisibleDescender);
                public const string vertexBufferAutoSizeReduction = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.vertexBufferAutoSizeReduction);
                public const string verticalAlignment = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.verticalAlignment);
                public const string verticalMapping = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.verticalMapping);
                public const string wordSpacing = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.wordSpacing);
                public const string wordWrappingRatios = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.wordWrappingRatios);
                [System.Obsolete("The \"enableKerning\" property has been deprecated. Use the \"fontFeatures\" property to control what features are enabled on the text component.")]
                public const string enableKerning = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.enableKerning);
                [System.Obsolete("The enabledWordWrapping property is now obsolete. Please use the textWrappingMode property instead.")]
                public const string enableWordWrapping = nameof(TMPro) + "." + nameof(G_TMPro.TMP_Text) + "." + nameof(G_TMPro.TMP_Text.enableWordWrapping);
                public const string isMaskingGraphic = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.isMaskingGraphic);
                public const string maskable = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.maskable);
                public const string onCullStateChanged = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.MaskableGraphic) + "." + nameof(G_UnityEngine.UI.MaskableGraphic.onCullStateChanged);
                public const string color = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.color);
                public const string material = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.material);
                public const string raycastPadding = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.raycastPadding);
                public const string raycastTarget = nameof(UnityEngine) + "." + nameof(G_UnityEngine.UI) + "." + nameof(G_UnityEngine.UI.Graphic) + "." + nameof(G_UnityEngine.UI.Graphic.raycastTarget);
                public const string enabled = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Behaviour) + "." + nameof(G_UnityEngine.Behaviour.enabled);
                public const string tag = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Component) + "." + nameof(G_UnityEngine.Component.tag);
                public const string hideFlags = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.hideFlags);
                public const string name = nameof(UnityEngine) + "." + nameof(G_UnityEngine.Object) + "." + nameof(G_UnityEngine.Object.name);
            }
        }

    }
}