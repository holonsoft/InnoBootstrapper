using Autofac;
using Autofac.Core;
using FluentAssertions;
using holonsoft.InnoBootstrapper.Abstractions.Attributes;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup.Functions;
using holonsoft.InnoBootstrapper.Abstractions.Enums;
using Xunit;

namespace holonsoft.InnoBootstrapper.Tests;
public class TestBasicFunctionality
{
  [Fact]
  public async Task TestEmpty()
  {
    var bootstrapper = HolonBootstrapper.Create();
    await bootstrapper.RunAndWaitAsync();
  }

  private class TestHolonRuntime : IHolonRuntime
  {
    public Task InitializeAsync()
      => Task.CompletedTask;
    public Task RunAsync(CancellationToken stoppingToken)
      => Task.Delay(Timeout.Infinite, stoppingToken);
  }

  [Fact]
  public async Task TestStopByCall()
  {
    var bootstrapper
      = HolonBootstrapper
      .Create()
      .AddHolonRuntime<TestHolonRuntime>();
    await bootstrapper.RunAsync();
    await bootstrapper.StopAsync();
  }

  private class TestHolonRuntimeUngraceful : IHolonRuntime
  {
    public Task InitializeAsync()
      => Task.CompletedTask;
    public Task RunAsync(CancellationToken stoppingToken)
      => Task.Delay(Timeout.Infinite);
  }

  [Fact]
  public async Task TestUngracefulStopByCall()
  {
    var bootstrapper
      = HolonBootstrapper
      .Create(TimeSpan.FromSeconds(2))
      .AddHolonRuntime<TestHolonRuntimeUngraceful>();

    await bootstrapper.RunAsync();
    Exception? exception = null;
    try
    {
      await bootstrapper.StopAsync();
    }
    catch (Exception ex)
    {
      exception = ex;
    }
    exception.Should().NotBeNull();
    exception.Should().BeAssignableTo<TimeoutException>();
  }

  private interface IFakeDependency
  {
  }

  private class FakeDependency : IFakeDependency
  {
  }


  private class TestHolonRuntimeWithDependency : IHolonRuntime
  {
    private readonly IFakeDependency _fakeDependency;

    public TestHolonRuntimeWithDependency(IFakeDependency fakeDependency)
      => _fakeDependency = fakeDependency;

    public Task InitializeAsync()
      => Task.CompletedTask;
    public Task RunAsync(CancellationToken stoppingToken)
      => Task.CompletedTask;
  }

  private class TestHolonSetupLocalDependency : IHolonSetup, IHolonSetupLocalLifetimeScope
  {
    public Task InitializeAsync()
      => Task.CompletedTask;

    public void SetupLocalLifetime(ContainerBuilder containerBuilder)
      => containerBuilder.RegisterType<FakeDependency>()
                         .As<IFakeDependency>();
  }

  [Fact]
  public async Task TestLocalDependenciesFail()
  {
    var bootstrapper
      = HolonBootstrapper
      .Create()
      .AddHolonRuntime<TestHolonRuntimeWithDependency>();

    Exception? exception = null;
    try
    {
      await bootstrapper.RunAsync();
    }
    catch (Exception ex)
    {
      exception = ex;
    }
    exception.Should().NotBeNull();
    exception.Should().BeAssignableTo<DependencyResolutionException>();
    await bootstrapper.StopAsync();
  }

  [Fact]
  public async Task TestLocalDependenciesWork()
  {
    var bootstrapper
      = HolonBootstrapper
      .Create()
      .AddHolon<TestHolonSetupLocalDependency, TestHolonRuntimeWithDependency>();

    await bootstrapper.RunAsync();
    await bootstrapper.StopAsync();
  }

  [Fact]
  public async Task TestLocalDependenciesInvisibleToOthers()
  {
    var bootstrapper
      = HolonBootstrapper
      .Create()
      .AddHolon<TestHolonSetupLocalDependency, TestHolonRuntimeWithDependency>()
      .AddHolonRuntime<TestHolonRuntimeWithDependency>();

    Exception? exception = null;
    try
    {
      await bootstrapper.RunAsync();
    }
    catch (Exception ex)
    {
      exception = ex;
    }
    exception.Should().NotBeNull();
    exception.Should().BeAssignableTo<DependencyResolutionException>();
    await bootstrapper.StopAsync();
  }

  [Fact]
  public async Task TestSharedDependenciesFail()
  {
    var bootstrapper
      = HolonBootstrapper
      .Create()
      .AddHolonRuntime<TestHolonRuntimeWithDependency>();

    Exception? exception = null;
    try
    {
      await bootstrapper.RunAsync();
    }
    catch (Exception ex)
    {
      exception = ex;
    }
    exception.Should().NotBeNull();
    exception.Should().BeAssignableTo<DependencyResolutionException>();
    await bootstrapper.StopAsync();
  }

  private class TestHolonSetupSharedDependency : IHolonSetup, IHolonSetupSharedLifetimeScope
  {
    public Task InitializeAsync()
      => Task.CompletedTask;

    public void SetupSharedLifetime(ContainerBuilder containerBuilder)
      => containerBuilder.RegisterType<FakeDependency>()
                         .As<IFakeDependency>();
  }

  [Fact]
  public async Task TestSharedDependenciesWork()
  {
    var bootstrapper = HolonBootstrapper.Create()
      .AddHolon<TestHolonSetupSharedDependency, TestHolonRuntimeWithDependency>()
      .AddHolonRuntime<TestHolonRuntimeWithDependency>();

    await bootstrapper.RunAsync();
    await bootstrapper.StopAsync();
  }

  [HolonSetupStage(nameof(HolonSetupStage.InitBootstrapper))]
  private class TestHolonSetupSharedDependencyOnFirstStage : IHolonSetup, IHolonSetupSharedLifetimeScope
  {
    public Task InitializeAsync()
      => Task.CompletedTask;

    public void SetupSharedLifetime(ContainerBuilder containerBuilder)
      => containerBuilder.RegisterType<FakeDependency>()
                         .As<IFakeDependency>();
  }

  [Fact]
  public async Task TestSharedDependenciesWorkAcrossStages()
  {
    var bootstrapper
      = HolonBootstrapper
      .Create()
      .AddHolonSetup<TestHolonSetupSharedDependencyOnFirstStage>()
      .AddHolonRuntime<TestHolonRuntimeWithDependency>();

    await bootstrapper.RunAsync();
    await bootstrapper.StopAsync();
  }

  [HolonSetupStage(nameof(HolonSetupStage.InitBootstrapper))]
  private class TestHolonRuntimeWithDependencyOnFirstStage : IHolonRuntime
  {
    private readonly IFakeDependency _fakeDependency;

    public TestHolonRuntimeWithDependencyOnFirstStage(IFakeDependency fakeDependency)
      => _fakeDependency = fakeDependency;

    public Task InitializeAsync()
      => Task.CompletedTask;
    public Task RunAsync(CancellationToken stoppingToken)
      => Task.CompletedTask;
  }


  [Fact]
  public async Task TestSharedDependenciesRespectStages()
  {
    var bootstrapper = HolonBootstrapper.Create()
      .AddHolonSetup<TestHolonSetupSharedDependency>()
      .AddHolonRuntime<TestHolonRuntimeWithDependencyOnFirstStage>();

    Exception? exception = null;
    try
    {
      await bootstrapper.RunAsync();
    }
    catch (Exception ex)
    {
      exception = ex;
    }
    exception.Should().NotBeNull();
    exception.Should().BeAssignableTo<DependencyResolutionException>();
    await bootstrapper.StopAsync();
  }

  private class TestHolonSetupForScanning : IHolonSetup<TestHolonRuntime>
  {
    public Task InitializeAsync()
      => Task.CompletedTask;
  }

  [Fact]
  public async Task TestScanning()
  {
    var scanned = new List<(Type SetupType, Type RuntimeType)>();

    var bootstrapper
      = HolonBootstrapper
      .Create()
      .AddHolonsByScan(x =>
    {
      scanned.Add(x);
      return x.SetupType == typeof(TestHolonSetupForScanning);
    });

    await bootstrapper.RunAsync();
    await bootstrapper.StopAsync();

    scanned.Where(x => x.SetupType == typeof(TestHolonSetupForScanning)).Count().Should().Be(1);
    scanned.Where(x => x.RuntimeType == typeof(TestHolonRuntime)).Count().Should().Be(1);
    scanned.Where(x => x.SetupType == typeof(TestHolonSetupForScanning) && x.RuntimeType == typeof(TestHolonRuntime)).Count().Should().Be(1);
    scanned.Count().Should().BeGreaterThan(1);
  }
}