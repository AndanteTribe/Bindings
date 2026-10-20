using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Bindings.Tests;

public class VContainerRegistrationTests
{
    private static readonly Lazy<Assembly> s_testAssembly = new(CompileAssertions);

    [Theory]
    [InlineData("DeferredRegistration")]
    [InlineData("SettingsAndPrivateInjector")]
    [InlineData("MultipleBindersAndDuplicateTypes")]
    [InlineData("FallbackAndEmpty")]
    [InlineData("ScopeLifecycle")]
    [InlineData("ScopeCommitsOnException")]
    [InlineData("ValueTypeAndDefaultScope")]
    [InlineData("ExplicitDisposeSurface")]
    public void BinderRegistrationPreservesConfigurationAndResolution(string scenario)
        => s_testAssembly.Value.GetType("Assertions")!.GetMethod(scenario)!.Invoke(null, null);

    [Fact]
    public void HelperCompilesWithoutVContainer()
    {
        var compilation = CSharpCompilation.Create("NoVContainer",
            new[] { CSharpSyntaxTree.ParseText(ReadHelper()) }, References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void DisposeCannotBeCalledDirectly()
    {
        var options = new CSharpParseOptions(preprocessorSymbols: new[] { "ENABLE_VCONTAINER" });
        var compilation = CSharpCompilation.Create("DirectDispose",
            new[]
            {
                CSharpSyntaxTree.ParseText(ReadHelper(), options),
                CSharpSyntaxTree.ParseText(StubsAndAssertions, options),
                CSharpSyntaxTree.ParseText("class DirectDispose { void Invoke(Bindings.BinderRegistration scope) { scope.Dispose(); } }", options),
            }, References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var error = Assert.Single(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Equal("CS1061", error.Id);
        Assert.Contains("Dispose", error.GetMessage());
    }

    private static string ReadHelper() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "VContainerExtensions.cs"));

    private static MetadataReference[] References() => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
        .Select(path => MetadataReference.CreateFromFile(path)).ToArray();

    private static Assembly CompileAssertions()
    {
        var options = new CSharpParseOptions(preprocessorSymbols: new[] { "ENABLE_VCONTAINER" });
        var compilation = CSharpCompilation.Create("RegistrationTest_" + Guid.NewGuid().ToString("N"),
            new[]
            {
                CSharpSyntaxTree.ParseText(ReadHelper(), options),
                CSharpSyntaxTree.ParseText(StubsAndAssertions, options),
            }, References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return Assembly.Load(stream.ToArray());
    }

    // API doubles test the actual package helper without requiring Unity in .NET tests.
    private const string StubsAndAssertions = """
        using System;
        using System.Collections.Generic;
        using Bindings;
        using VContainer;
        namespace VContainer
        {
            public enum Lifetime { Transient, Scoped, Singleton }
            public interface IObjectResolver { object Resolve(Registration registration); }
            public interface IInjectParameter { }
            public sealed class Parameter : IInjectParameter
            {
                public Type Type; public Func<IObjectResolver, object> Value;
                public Parameter(Type type, Func<IObjectResolver, object> value) { Type = type; Value = value; }
            }
            public interface IInjector
            {
                object CreateInstance(IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters);
                void Inject(object instance, IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters);
            }
            public interface IInstanceProvider { object SpawnInstance(IObjectResolver resolver); }
            public interface IContainerBuilder
            {
                T Register<T>(T registration) where T : RegistrationBuilder;
                void RegisterBuildCallback(Action<IObjectResolver> callback);
            }
            public sealed class Builder : IContainerBuilder
            {
                public readonly List<RegistrationBuilder> Builders = new();
                public readonly List<Registration> Registrations = new();
                private readonly List<Action<IObjectResolver>> _callbacks = new();
                public T Register<T>(T registration) where T : RegistrationBuilder { Builders.Add(registration); return registration; }
                public void RegisterBuildCallback(Action<IObjectResolver> callback) => _callbacks.Add(callback);
                public Resolver Build()
                {
                    foreach (var builder in Builders) Registrations.Add(builder.Build());
                    var resolver = new Resolver();
                    foreach (var callback in _callbacks) callback(resolver);
                    return resolver;
                }
            }
            public sealed class Resolver : IObjectResolver
            {
                public readonly List<Registration> Resolved = new();
                private readonly Dictionary<Registration, object> _instances = new();
                public object Resolve(Registration registration)
                {
                    Resolved.Add(registration);
                    if (registration.Lifetime == Lifetime.Transient) return registration.Provider.SpawnInstance(this);
                    if (!_instances.TryGetValue(registration, out var value))
                    { value = registration.Provider.SpawnInstance(this); _instances.Add(registration, value); }
                    return value;
                }
            }
            public sealed class Registration
            {
                public Type Type; public Lifetime Lifetime; public List<Type> Interfaces;
                public IInstanceProvider Provider; public object Key;
                public Registration(Type type, Lifetime lifetime, List<Type> interfaces, IInstanceProvider provider, object key)
                { Type = type; Lifetime = lifetime; Interfaces = interfaces; Provider = provider; Key = key; }
            }
            public class RegistrationBuilder
            {
                protected internal readonly Type ImplementationType;
                protected internal readonly Lifetime Lifetime;
                protected internal List<Type> InterfaceTypes;
                protected internal List<IInjectParameter> Parameters;
                protected internal object Key;
                public RegistrationBuilder(Type type, Lifetime lifetime) { ImplementationType = type; Lifetime = lifetime; }
                public virtual Registration Build() => new Registration(ImplementationType, Lifetime, InterfaceTypes, new PlainProvider(ImplementationType), Key);
                public RegistrationBuilder As<T>() { (InterfaceTypes ??= new List<Type>()).Add(typeof(T)); return this; }
                public RegistrationBuilder AsSelf() { (InterfaceTypes ??= new List<Type>()).Add(ImplementationType); return this; }
                public RegistrationBuilder WithParameter<T>(T value) => WithParameter<T>(_ => value);
                public RegistrationBuilder WithParameter<T>(Func<IObjectResolver, T> value)
                { (Parameters ??= new List<IInjectParameter>()).Add(new Parameter(typeof(T), r => value(r))); return this; }
                public RegistrationBuilder Keyed(object key) { Key = key; return this; }
            }
            public sealed class PlainProvider : IInstanceProvider
            {
                private readonly Type _type;
                public PlainProvider(Type type) { _type = type; }
                public object SpawnInstance(IObjectResolver resolver) => Activator.CreateInstance(_type);
            }
        }
        namespace VContainer.Internal
        {
            public static class InjectorCache
            {
                public static IInjector GetOrBuild(Type type) => new BinderInjector();
            }
            public sealed class BinderInjector : IInjector
            {
                public object CreateInstance(IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters) => throw new Exception("Binder must not be constructed.");
                public void Inject(object instance, IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters)
                {
                    var binder = (Binder)instance;
                    foreach (Parameter parameter in parameters)
                        if (parameter.Type == typeof(IReadOnlyList<IViewModel>)) binder.ViewModels = (IReadOnlyList<IViewModel>)parameter.Value(resolver);
                }
            }
        }
        namespace Bindings
        {
            public interface IViewModel { }
            public interface IMvvmPublisher { }
            public sealed class Binder : IMvvmPublisher { public IReadOnlyList<IViewModel> ViewModels; public int RunCalls; }
        }
        public sealed class OptimizedViewModel : IViewModel
        {
            public IReadOnlyList<IInjectParameter> Parameters;
            private sealed class __BindingsInjector : IInjector
            {
                public object CreateInstance(IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters)
                    => new OptimizedViewModel { Parameters = parameters };
                public void Inject(object instance, IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters) { }
            }
        }
        public sealed class PlainViewModel : IViewModel { }
        public sealed class IncompatibleViewModel : IViewModel { private sealed class __BindingsInjector { } }
        public static class Assertions
        {
            public static void DeferredRegistration()
            {
                var builder = new Builder();
                RegistrationBuilder first; RegistrationBuilder second;
                using (var scope = builder.RegisterBinder(new Binder()))
                {
                    first = scope.WithViewModel<OptimizedViewModel>();
                    second = scope.WithViewModel<OptimizedViewModel>();
                    if (builder.Builders.Count != 0) throw new Exception("Configuration registered prematurely.");
                }
                if (builder.Builders.Count != 3 || !ReferenceEquals(builder.Builders[0], first) || !ReferenceEquals(builder.Builders[1], second))
                    throw new Exception("Deferred registration order changed.");
            }
            public static void SettingsAndPrivateInjector()
            {
                var builder = new Builder(); var binder = new Binder();
                using var configuration = builder.RegisterBinder(binder);
                var vmBuilder = configuration.WithViewModel<OptimizedViewModel>(Lifetime.Singleton);
                vmBuilder.AsSelf().WithParameter(42).Keyed("test");
                var binderBuilder = configuration.Register();
                var resolver = builder.Build();
                var registration = builder.Registrations[0];
                if (registration.Type != typeof(OptimizedViewModel) || registration.Lifetime != Lifetime.Singleton || !Equals(registration.Key, "test"))
                    throw new Exception("Registration settings lost.");
                if (!registration.Interfaces.Contains(typeof(IViewModel)) || !registration.Interfaces.Contains(typeof(OptimizedViewModel)))
                    throw new Exception("Interfaces lost.");
                if (!ReferenceEquals(((BinderViewModelRegistrationBuilder)vmBuilder).Registration, registration))
                    throw new Exception("The finalized registration was not stored.");
                if (!ReferenceEquals(builder.Registrations[1].Key, binder) || !ReferenceEquals(builder.Builders[1], binderBuilder))
                    throw new Exception("Binder key or returned builder lost.");
                resolver.Resolve(builder.Registrations[1]);
                var vm = (OptimizedViewModel)binder.ViewModels[0];
                if (vm.Parameters.Count != 2 || !ReferenceEquals(((Parameter)vm.Parameters[0]).Value(resolver), binder) || !Equals(((Parameter)vm.Parameters[1]).Value(resolver), 42))
                    throw new Exception("Parameters lost.");
                if (!ReferenceEquals(vm, resolver.Resolve(registration)) || binder.RunCalls != 0)
                    throw new Exception("Lifetime lost or Binder.Run called automatically.");
            }
            public static void MultipleBindersAndDuplicateTypes()
            {
                var builder = new Builder(); var first = new Binder(); var second = new Binder();
                using (var group = builder.RegisterBinder(first))
                {
                    group.WithViewModel<OptimizedViewModel>().Keyed("same");
                    group.WithViewModel<OptimizedViewModel>().Keyed("same");
                }
                using (var group = builder.RegisterBinder(second))
                    group.WithViewModel<OptimizedViewModel>(Lifetime.Transient).Keyed("same");
                var resolver = builder.Build();
                resolver.Resolve(builder.Registrations[2]); resolver.Resolve(builder.Registrations[4]);
                if (first.ViewModels.Count != 2 || second.ViewModels.Count != 1 || ReferenceEquals(first.ViewModels[0], first.ViewModels[1]))
                    throw new Exception("Duplicate ViewModel types were merged or leaked between Binders.");
                if (!ReferenceEquals(first.ViewModels[0], resolver.Resolve(builder.Registrations[0])) || !ReferenceEquals(first.ViewModels[1], resolver.Resolve(builder.Registrations[1])))
                    throw new Exception("Resolution did not use exact finalized registrations in order.");
                var other = resolver.Resolve(builder.Registrations[3]);
                if (ReferenceEquals(second.ViewModels[0], other)) throw new Exception("Transient lifetime lost.");
                if (!ReferenceEquals(((Parameter)((OptimizedViewModel)first.ViewModels[0]).Parameters[0]).Value(resolver), first) ||
                    !ReferenceEquals(((Parameter)((OptimizedViewModel)second.ViewModels[0]).Parameters[0]).Value(resolver), second))
                    throw new Exception("Publisher leaked between Binders.");
            }
            public static void FallbackAndEmpty()
            {
                var builder = new Builder(); var binder = new Binder(); var empty = new Binder();
                RegistrationBuilder plain; RegistrationBuilder incompatible;
                using (var scope = builder.RegisterBinder(binder))
                {
                    plain = scope.WithViewModel<PlainViewModel>();
                    incompatible = scope.WithViewModel<IncompatibleViewModel>();
                }
                using (builder.RegisterBinder(empty)) { }
                var resolver = builder.Build();
                resolver.Resolve(builder.Registrations[2]); resolver.Resolve(builder.Registrations[3]);
                if (binder.ViewModels[0] is not PlainViewModel || binder.ViewModels[1] is not IncompatibleViewModel || empty.ViewModels.Count != 0)
                    throw new Exception("Fallback or empty chain failed.");
                if (!ReferenceEquals(((BinderViewModelRegistrationBuilder)plain).Registration, builder.Registrations[0]) ||
                    !ReferenceEquals(((BinderViewModelRegistrationBuilder)incompatible).Registration, builder.Registrations[1]))
                    throw new Exception("Fallback did not retain the finalized registration.");
            }

            public static void ScopeLifecycle()
            {
                var builder = new Builder();
                using (var scope = builder.RegisterBinder(new Binder()))
                {
                    scope.WithViewModel<OptimizedViewModel>();
                    var first = scope.Register();
                    if (!ReferenceEquals(first, scope.Register()) || builder.Builders.Count != 2)
                        throw new Exception("Explicit commit registered more than once.");
                    try { scope.WithViewModel<OptimizedViewModel>(); throw new Exception("Adding after commit succeeded."); }
                    catch (InvalidOperationException) { }
                }
                if (builder.Builders.Count != 2)
                    throw new Exception("Dispose registered an already committed scope twice.");
                using (builder.RegisterBinder(new Binder())) { }
                if (builder.Builders.Count != 3) throw new Exception("Empty scope was registered twice.");
            }

            public static void ScopeCommitsOnException()
            {
                var builder = new Builder();
                try
                {
                    using var scope = builder.RegisterBinder(new Binder());
                    scope.WithViewModel<PlainViewModel>();
                    throw new InvalidOperationException("Scope body failed.");
                }
                catch (InvalidOperationException) { }
                if (builder.Builders.Count != 2) throw new Exception("Scope did not commit during stack unwinding.");
            }

            public static void ValueTypeAndDefaultScope()
            {
                if (!typeof(BinderRegistration).IsValueType)
                    throw new Exception("The registration scope must be a struct.");
                using var scope = default(BinderRegistration);
                try { scope.WithViewModel<PlainViewModel>(); throw new Exception("Default scope accepted a ViewModel."); }
                catch (InvalidOperationException) { }
                try { scope.Register(); throw new Exception("Default scope committed."); }
                catch (InvalidOperationException) { }
            }

            public static void ExplicitDisposeSurface()
            {
                if (!typeof(IDisposable).IsAssignableFrom(typeof(BinderRegistration)) || typeof(BinderRegistration).GetMethod("Dispose") != null)
                    throw new Exception("Dispose must be implemented explicitly.");
                if (typeof(BinderRegistration).GetNestedType("State", System.Reflection.BindingFlags.NonPublic) != null)
                    throw new Exception("The struct must hold its own registration state.");
            }
        }
        """;
}