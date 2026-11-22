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
        [Test]
        public void StructListenerReadsValueSuppliedToView()
        {
            var view = new StructEventView();
            var binder = TestObjects.CreateBinder(view);
            var ui = new GameObject("UI", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                var image = ui.AddComponent<Image>();
                var button = ui.AddComponent<Button>();
                var fields = typeof(StructEventView).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
                fields.Single(f => f.FieldType == typeof(Image)).SetValue(view, image);
                fields.Single(f => f.FieldType == typeof(Button)).SetValue(view, button);
                float observed = -1;
                var model = new StructEventViewModel(value => observed = value, binder)
                {
                    Amount = 0.75f
                };
                binder.Initialize(model);
                binder.Run();
                button.onClick.Invoke();
                Assert.That(image.fillAmount, Is.EqualTo(0.75f));
                Assert.That(observed, Is.EqualTo(image.fillAmount));
            }
            finally
            {
                Object.DestroyImmediate(binder.gameObject);
                Object.DestroyImmediate(ui);
            }
        }

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
    internal partial struct StructEventViewModel
    {
        [Required]
        private readonly System.Action<float> _observe;

        [Schema(PathResolver.UnityEngine.UI.Image.fillAmount)]
        public partial float Amount { get; set; }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        private void Click() => _observe(Amount);
    }

    [ViewModel]
    internal partial class IntegrationViewModel
    {
        [Schema(PathResolver.UnityEngine.UI.Image.fillAmount)]
        public partial float Amount { get; set; }

        public int Clicks { get; private set; }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        private void Click() => Clicks++;
    }
}