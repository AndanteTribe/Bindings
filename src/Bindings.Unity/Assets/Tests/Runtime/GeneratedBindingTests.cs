#nullable enable

using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Bindings.Tests
{
    public sealed class GeneratedBindingTests
    {
        [UnityTest]
        public IEnumerator GeneratedViewConnectsRealUnityComponentsAndEvents() => new ToCoroutineEnumerator(async cancellationToken =>
        {
            var view = new IntegrationView();
            var binder = TestObjects.CreateBinder(view);
            var ui = new GameObject("UI", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                var image = ui.AddComponent<Image>();
                var button = ui.AddComponent<Button>();
                var fields = typeof(IntegrationView).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
                fields.Single(f => f.FieldType == typeof(Image)).SetValue(view, image);
                fields.Single(f => f.FieldType == typeof(Button)).SetValue(view, button);
                var model = new IntegrationViewModel(binder);
                binder.Initialize(model);
                model.Amount = 0.25f;
                binder.Run();
                Assert.That(image.fillAmount, Is.EqualTo(0.25f));
                model.Amount = 0.75f;
                Canvas.ForceUpdateCanvases();
                await Awaitable.NextFrameAsync(cancellationToken);
                Assert.That(image.fillAmount, Is.EqualTo(0.75f));
                binder.Run();
                button.onClick.Invoke();
                Assert.That(model.Clicks, Is.EqualTo(1), "Rebinding must not duplicate UnityEvent listeners.");
            }
            finally
            {
                Object.DestroyImmediate(binder.gameObject);
                Object.DestroyImmediate(ui);
            }
        });
    }

    [ViewModel]
    internal partial class IntegrationViewModel
    {
        [Schema(PathResolver.UnityEngine.UI.Image.fillAmount)]
        private float _amount;

        public int Clicks { get; private set; }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        private void Click() => Clicks++;
    }
}