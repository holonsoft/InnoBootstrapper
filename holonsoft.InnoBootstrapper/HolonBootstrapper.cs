using Autofac;
using Autofac.Features.ResolveAnything;
using holonsoft.FluentConditions;
using holonsoft.InnoBootstrapper.Abstractions.Attributes;
using holonsoft.InnoBootstrapper.Abstractions.Contracts;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;
using holonsoft.InnoBootstrapper.Abstractions.Enums;
using holonsoft.InnoBootstrapper.Abstractions.Models.Runtime;
using holonsoft.InnoBootstrapper.Abstractions.Models.Setup;
using System.Reflection;

namespace holonsoft.InnoBootstrapper;
public class HolonBootstrapperBase : IHolonBootstrapper
{
  private readonly Dictionary<HolonSetupStage, List<HolonRegistration>> _registrations;
  private readonly TimeSpan _gracefulTeardownTimeSpan;
  private const int _defaultGracefulTeardownTimeSpanSeconds = 30;

  private CancellationTokenSource? _cancellationTokenSource;
  private ILifetimeScope? _rootLifetimeScope;
  private HolonSetupStage? _currentStage;

  protected HolonBootstrapperBase(TimeSpan? gracefulTeardownTimeSpan = default)
  {
    _registrations = HolonSetupStage.GetValues().ToDictionary(x => x, x => new List<HolonRegistration>());
    _gracefulTeardownTimeSpan = gracefulTeardownTimeSpan ?? TimeSpan.FromSeconds(_defaultGracefulTeardownTimeSpanSeconds);
  }

  IHolonBootstrapper IHolonBootstrapper.AddHolon(HolonSetupStage? setupStage, Type? setupType, Type? runtimeType, Func<IHolonSetup, Task>? externalConfiguration)
  {
    setupType = setupType ?? typeof(EmptyHolonSetup);
    runtimeType = runtimeType ?? typeof(EmptyHolonRuntime);

    setupType.Requires(nameof(setupType)).IsOfType<IHolonSetup>().IsNotAbstract();
    runtimeType.Requires(nameof(runtimeType)).IsOfType<IHolonRuntime>().IsNotAbstract();

    externalConfiguration = externalConfiguration ?? (x => Task.CompletedTask);

    static HolonSetupStage? DetermineSetupStage(Type type)
      => type.GetCustomAttributes<HolonSetupStageAttribute>(true).FirstOrDefault()?.SetupStage;

    setupStage
      = setupStage
        ?? DetermineSetupStage(setupType)
        ?? DetermineSetupStage(runtimeType)
        ?? HolonSetupStage.ApplicationRun;

    setupStage
      .Value
      .Requires(nameof(setupStage))
      .IsGreaterThan(_currentStage?.Value ?? int.MinValue, "New holons can only be added to coming stages!");

    _registrations[setupStage].Add(new HolonRegistration(setupStage, setupType, runtimeType, externalConfiguration));

    return this;
  }

  protected virtual Task ConfigureRootLifetimeScopeAsync(ContainerBuilder containerBuilder)
  {
    containerBuilder.RegisterSource<AnyConcreteTypeNotAlreadyRegisteredSource>();

    containerBuilder
      .RegisterInstance(this)
      .As<IHolonBootstrapper>()
      .SingleInstance();

    return Task.CompletedTask;
  }

  private async Task RunAsyncInternal(CancellationToken stoppingToken)
  {
    async Task RunWithoutSynchronizationAndExecutionFlow()
    {
      _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

      var containerBuilder = new ContainerBuilder();
      await ConfigureRootLifetimeScopeAsync(containerBuilder).ConfigureAwait(false);

      _rootLifetimeScope = containerBuilder.Build();

      var sharedLifetimeScope = _rootLifetimeScope;
      var stages = _registrations.Keys.OrderBy(x => x).ToArray();
      foreach (var stage in stages)
      {
        _currentStage = stage;
        var holonSetupRegistrationsPerStage = _registrations[_currentStage];
        var setupStageInstance = new HolonSetupStageInstance(holonSetupRegistrationsPerStage, sharedLifetimeScope);
        sharedLifetimeScope = await setupStageInstance.SetupAndRunAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
      }
    }

    //stop synchronization and Execution flow - e.g. WPF dispatcher or sth.....
    await Task.Run(RunWithoutSynchronizationAndExecutionFlow);
  }

  async Task IHolonBootstrapper.RunAsync(CancellationToken stoppingToken)
    => await RunAsyncInternal(stoppingToken).ConfigureAwait(false);

  private CancellationTokenSource GetCancellationTokenSource()
  => _cancellationTokenSource ?? throw new InvalidOperationException("Cancellation token source was not created!" +
                                                                     " Did you run the bootstrapper before trying to stop it?");

  private async Task WaitForShutdownInternalAsync()
  {
    try
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
    finally
    {
      if (_rootLifetimeScope != null)
      {
        await _rootLifetimeScope.DisposeAsync();
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

public sealed class HolonBootstrapper : HolonBootstrapperBase
{
  private HolonBootstrapper(TimeSpan? gracefulTeardownTimeSpan = default) : base(gracefulTeardownTimeSpan)
  {

  }
  public static IHolonBootstrapper Create(TimeSpan? gracefulTeardownTimeSpan = default)
    => new HolonBootstrapper(gracefulTeardownTimeSpan);

}
