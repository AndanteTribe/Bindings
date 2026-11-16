#nullable enable

using System;
using System.Linq;
using NUnit.Framework;
using VContainer;
using Object = UnityEngine.Object;

namespace Bindings.Tests
{
    public sealed class VContainerTests
    {
        private Binder _binder = null!;

        [SetUp]
        public void SetUp() => _binder = TestObjects.CreateBinder(new ContainerView());

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_binder.gameObject);

        [TestCase(false)]
        [TestCase(true)]
        public void RegistrationInjectsPublisherAndParametersAndDisposesOwnedModels(bool keyed)
        {
            // Repeat to exercise delegate reuse with a fresh container and fresh parameters.
            for (var i = 0; i < 2; i++)
            {
                var builder = new ContainerBuilder();
                using (var registration = builder.RegisterBinder(_binder, keyed))
                {
                    _ = registration.WithViewModel<ContainerModel>().AsSelf().WithParameter(i + 10);
                }
                ContainerModel model;
                using (var resolver = builder.Build())
                {
                    model = resolver.Resolve<ContainerModel>();
                    Assert.That(model.Publisher, Is.SameAs(_binder));
                    Assert.That(model.Value, Is.EqualTo(i + 10));
                    Assert.That(resolver.Resolve<Binder>(keyed ? _binder : null), Is.SameAs(_binder));
                    Assert.That(model.Disposed, Is.False);
                }
                Assert.That(model.Disposed, Is.True);
            }
        }

        [Test]
        public void ScrollRegistrationCreatesIndependentItemScopesAndDisposesThem()
        {
            TestObjects.SetField(_binder, "_views", new IView[] { new ScrollContainerView() });
            for (var iteration = 0; iteration < 2; iteration++)
            {
                var builder = new ContainerBuilder();
                using (var registration = builder.RegisterBinder(_binder))
                {
                    _ = registration.WithScrollViewModel(_ => new[] { 3, 7, 11 },
                        static (item, value) => item.WithViewModel<ContainerModel>().AsSelf().WithParameter(value)).AsSelf();
                }
                ContainerModel[] models;
                using (var resolver = builder.Build())
                {
                    var scroll = resolver.Resolve<SingleAxisVirtualScrollViewModel>();
                    var publishers = scroll._publishers.ToArray();
                    models = publishers.Select(p => (ContainerModel)p._viewModels.Single()).ToArray();
                    Assert.That(models.Select(m => m.Value), Is.EqualTo(new[] { 3, 7, 11 }));
                    for (var i = 0; i < models.Length; i++)
                    {
                        Assert.That(models[i].Publisher, Is.SameAs(publishers[i]));
                        Assert.That(models[i].Disposed, Is.False);
                    }
                    Assert.That(models.Distinct().Count(), Is.EqualTo(3));
                }
                Assert.That(models.All(m => m.Disposed), Is.True);
            }
        }

        [Test]
        public void NestedInjectorIsPreferredOverReflectionInjection()
        {
            var builder = new ContainerBuilder();
            using var resolver = builder.Build();
            var instance = ViewModelInjectorCache<NestedModel>.Injector.CreateInstance(resolver, Array.Empty<IInjectParameter>());
            Assert.That(instance, Is.TypeOf<NestedModel>());
            Assert.That(((NestedModel)instance).CreatedByInjector, Is.True);
        }

        private sealed class ContainerModel : IViewModel, IDisposable
        {
            public readonly IMvvmPublisher Publisher;
            public readonly int Value;
            public bool Disposed;

            public ContainerModel(IMvvmPublisher publisher, int value)
            {
                Publisher = publisher;
                Value = value;
            }

            public void Dispose() => Disposed = true;
        }

        private sealed class ContainerView : IView<ContainerModel>
        {
            public void Initialize(ContainerModel viewModel) { }
            public System.Threading.Tasks.ValueTask BindAsync(System.Threading.CancellationToken cancellationToken) => default;
        }

        private sealed class ScrollContainerView : IView<SingleAxisVirtualScrollViewModel>
        {
            public void Initialize(SingleAxisVirtualScrollViewModel viewModel) { }
            public System.Threading.Tasks.ValueTask BindAsync(System.Threading.CancellationToken cancellationToken) => default;
        }

        private sealed class NestedModel : IViewModel
        {
            public bool CreatedByInjector;

            private sealed class __BindingsInjector : IInjector
            {
                public object CreateInstance(IObjectResolver resolver, System.Collections.Generic.IReadOnlyList<IInjectParameter> parameters)
                    => new NestedModel { CreatedByInjector = true };

                public void Inject(object instance, IObjectResolver resolver, System.Collections.Generic.IReadOnlyList<IInjectParameter> parameters) { }
            }
        }
    }
}