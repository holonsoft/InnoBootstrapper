using Autofac;
using Autofac.Features.ResolveAnything;
using holonsoft.FluentConditions;
using holonsoft.InnoBootstrapper.Abstractions.Attributes;
using holonsoft.InnoBootstrapper.Abstractions.Contracts;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;
using holonsoft.InnoBootstrapper.Abstractions.Enums;
using holonsoft.InnoBootstrapper.Runtime;
using holonsoft.InnoBootstrapper.Setup;
using holonsoft.Utils;
using System.Reflection;

namespace holonsoft.InnoBootstrapper;
public abstract class HolonBootstrapperBase<TSelf> : IHolonBootstrapper where TSelf : HolonBootstrapperBase<TSelf>
{
  private readonly Dictionary<HolonSetupStage, List<HolonRegistration>> _registrations = new()
  {
    { HolonSetupStage.InitBootstrapper, new List<HolonRegistration>() },
    { HolonSetupStage.BasicServices, new List<HolonRegistration>() },
    { HolonSetupStage.CommonServices, new List<HolonRegistration>() },
    { HolonSetupStage.BusinessServices, new List<HolonRegistration>() },
    { HolonSetupStage.ApplicationRun, new List<HolonRegistration>() },
  };

  private CancellationTokenSource? _cancellationTokenSource;
  private CancellationTokenSource GetCancellationTokenSource()
    => _cancellationTokenSource ?? throw new InvalidOperationException("Cancellation token source was not created!" +
                                                                       " Did you run the bootstrapper before trying to stop it?");

  private readonly TimeSpan _gracefulTeardownTimeSpan;
  private const int _defaultGracefulTeardownTimeSpanSeconds = 30;

  protected HolonBootstrapperBase(TimeSpan? gracefulTeardownTimeSpan = default)
    => _gracefulTeardownTimeSpan = gracefulTeardownTimeSpan ?? TimeSpan.FromSeconds(_defaultGracefulTeardownTimeSpanSeconds);

  private Task AddHolonInternalAsync(Type setupType, Type runtimeType, Func<IHolonSetup, Task> externalConfiguration)
  {
    setupType.Requires(nameof(setupType)).IsNotNull().IsOfType<IHolonSetup>().IsNotAbstract();
    runtimeType.Requires(nameof(runtimeType)).IsNotNull().IsOfType<IHolonRuntime>().IsNotAbstract();
    externalConfiguration = externalConfiguration ?? (x => Task.CompletedTask);

    static HolonSetupStage? DetermineSetupStage(Type type)
      => type.GetCustomAttributes<HolonSetupStageAttribute>(true).FirstOrDefault()?.SetupStage;

    var setupStage = DetermineSetupStage(setupType) ?? DetermineSetupStage(runtimeType) ?? HolonSetupStage.ApplicationRun;

    _registrations[setupStage].Add(new HolonRegistration(setupStage, setupType, runtimeType));

    return Task.CompletedTask;
  }

  Task IHolonBootstrapper.AddHolonAsync<THolonSetup, THolonRuntime>(Func<THolonSetup, Task> externalConfiguration)
    => AddHolonInternalAsync(typeof(THolonSetup), typeof(THolonRuntime), x => externalConfiguration((THolonSetup) x));

  Task IHolonBootstrapper.AddHolonRuntimeAsync<THolonRuntime>()
    => AddHolonInternalAsync(typeof(EmptyHolonSetup), typeof(THolonRuntime), x => Task.CompletedTask);

  Task IHolonBootstrapper.AddHolonSetupAsync<THolonSetup>(Func<THolonSetup, Task> externalConfiguration)
    => AddHolonInternalAsync(typeof(THolonSetup), typeof(EmptyHolonRuntime), x => externalConfiguration((THolonSetup) x));

  async Task IHolonBootstrapper.AddHolonsByScan(Func<(Type SetupType, Type RuntimeType), bool> predicate)
  {
    var setupTypes = ReflectionUtils.AllTypes.Values.Where(x => x.IsAssignableTo<IHolonSetup>()).ToHashSet();
    var runtimeTypes = ReflectionUtils.AllTypes.Values.Where(x => x.IsAssignableTo<IHolonRuntime>()).ToHashSet();

    var assignedTypes =
      setupTypes.SelectMany(x => x.GetInterfaces().Where(y => y.IsGenericType && y.GetGenericTypeDefinition() == typeof(IHolonSetup<>)).Select(y => (SetupType: x, Interface: y)))
                .Select(x => (SetupType: x.SetupType, RuntimeType: x.Interface.GetGenericArguments().Single()))
                .ToArray();

    setupTypes.ExceptWith(assignedTypes.Select(x => x.SetupType));
    runtimeTypes.ExceptWith(assignedTypes.Select(x => x.RuntimeType));

    async Task MayAdd(Type setupType, Type runtimeType)
    {
      if (predicate((setupType, runtimeType)))
      {
        await AddHolonInternalAsync(setupType, runtimeType, x => Task.CompletedTask);
      }
    }

    foreach ((var setupType, var runtimeType) in assignedTypes)
    {
      await MayAdd(setupType, runtimeType);
    }

    foreach (var runtimeType in runtimeTypes)
    {
      await MayAdd(typeof(EmptyHolonSetup), runtimeType);
    }

    foreach (var setupType in setupTypes)
    {
      await MayAdd(setupType, typeof(EmptyHolonRuntime));
    }
  }

  protected virtual Task ConfigureRootLifetimeScopeAsync(ContainerBuilder containerBuilder)
  {
    containerBuilder.RegisterSource<AnyConcreteTypeNotAlreadyRegisteredSource>();
    return Task.CompletedTask;
  }

  private async Task RunAsyncInternal(CancellationToken stoppingToken)
  {
    async Task RunWithoutSynchronizationAndExecutionFlow()
    {
      _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

      var containerBuilder = new ContainerBuilder();
      await ConfigureRootLifetimeScopeAsync(containerBuilder).ConfigureAwait(false);

      ILifetimeScope sharedLifetimeScope = containerBuilder.Build();
      foreach (var holonSetupRegistrationsPerStage in _registrations.OrderBy(x => x.Key).Select(x => x.Value))
      {
        var setupStageInstance = new HolonSetupStageInstance(holonSetupRegistrationsPerStage, sharedLifetimeScope);
        sharedLifetimeScope = await setupStageInstance.SetupAndRunAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
      }
    }

    //stop synchronization and Execution flow - e.g. WPF dispatcher or sth.....
    await Task.Run(RunWithoutSynchronizationAndExecutionFlow);
  }

  async Task IHolonBootstrapper.RunAsync(CancellationToken stoppingToken)
    => await RunAsyncInternal(stoppingToken).ConfigureAwait(false);

  private async Task WaitForShutdownInternalAsync()
  {
    var cancellationTokenSource = GetCancellationTokenSource();

    Task[] GetAllTasks()
      => _registrations
          .SelectMany(x => x.Value)
          .Select(x => x.RuntimeTask ?? Task.CompletedTask)
          .Where(x => !x.IsCompleted)
          .ToArray();

    Task[] allTasks;
    while (!cancellationTokenSource.IsCancellationRequested && (allTasks = GetAllTasks()).Length > 0)
    {
      try
      {
        await Task.WhenAll(allTasks).WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
      }
      catch (TaskCanceledException)
      {
      }
    }

    while ((allTasks = GetAllTasks()).Length > 0)
    {
      try
      {
        await Task.WhenAll(allTasks).WaitAsync(_gracefulTeardownTimeSpan).ConfigureAwait(false);
      }
      catch (TaskCanceledException)
      {
      }
    }
  }

  async Task IHolonBootstrapper.RunAndWaitAsync(CancellationToken stoppingToken)
  {
    await RunAsyncInternal(stoppingToken).ConfigureAwait(false);
    await WaitForShutdownInternalAsync().ConfigureAwait(false);
  }

  async Task IHolonBootstrapper.StopAsync()
  {
    GetCancellationTokenSource().Cancel();
    await WaitForShutdownInternalAsync().ConfigureAwait(false);
  }
}

public sealed class HolonBootstrapper : HolonBootstrapperBase<HolonBootstrapper>
{
  private HolonBootstrapper(TimeSpan? gracefulTeardownTimeSpan = default) : base(gracefulTeardownTimeSpan)
  {

  }

  public static IHolonBootstrapper Create(TimeSpan? gracefulTeardownTimeSpan = default)
    => new HolonBootstrapper(gracefulTeardownTimeSpan);
}
