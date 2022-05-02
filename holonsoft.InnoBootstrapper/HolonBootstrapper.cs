using Autofac;
using Autofac.Features.ResolveAnything;
using holonsoft.FluentConditions;
using holonsoft.InnoBootstrapper.Abstractions.Attributes;
using holonsoft.InnoBootstrapper.Abstractions.Contracts;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup.Functions;
using holonsoft.InnoBootstrapper.Abstractions.Enums;
using holonsoft.InnoBootstrapper.Abstractions.Models.Runtime;
using holonsoft.InnoBootstrapper.Abstractions.Models.Setup;
using System.Collections.Concurrent;
using System.Reflection;

namespace holonsoft.InnoBootstrapper;
public class HolonBootstrapperBase : IHolonBootstrapper
{
  private readonly ConcurrentDictionary<HolonSetupStage, ConcurrentBag<HolonRegistration>> _registrations;

  private HolonSetupStage? _currentStage;

  protected HolonBootstrapperBase() => _registrations = new(HolonSetupStage.GetValues().Select(x => KeyValuePair.Create(x, new ConcurrentBag<HolonRegistration>())));

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

    setupStage.Value
      .Requires(nameof(setupStage))
      .IsGreaterThan(_currentStage?.Value ?? int.MinValue, "New holons can only be added to coming stages!");

    _registrations[setupStage].Add(new HolonRegistration(setupStage, setupType, runtimeType, externalConfiguration));

    return this;
  }

  async Task<IHolonBootstrapperLifetime> IHolonBootstrapper.StartAsync(CancellationToken stoppingToken)
  {
    _currentStage.Requires(nameof(_currentStage)).IsNull($"Bootstrapper was already started!");
    _currentStage = HolonSetupStage.GetValues().Min();

    async Task<IHolonBootstrapperLifetime> RunWithoutSynchronizationAndExecutionFlow()
    {
      var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

      var containerBuilder = new ContainerBuilder();

      containerBuilder.RegisterSource<AnyConcreteTypeNotAlreadyRegisteredSource>();

      containerBuilder
        .RegisterInstance(this)
        .As<IHolonBootstrapper>()
        .SingleInstance();

      var rootLifetimeScope = containerBuilder.Build();

      var lifetime = new HolonBootstrapperLifetime(rootLifetimeScope, cancellationTokenSource);
      try
      {
        foreach (var stage in _registrations.OrderBy(x => x.Key))
        {
          _currentStage = stage.Key;
          await SetupAndRunStageAsync(lifetime, stage.Value).ConfigureAwait(false);
        }
      }
      catch (Exception) //in case of an error while setting up - try to teardown everything
      {
        await lifetime.StopAsync();
        throw;
      }
      return lifetime;
    }

    //stop synchronization and Execution flow - e.g. WPF dispatcher or sth.....
    return await Task.Run(RunWithoutSynchronizationAndExecutionFlow).ConfigureAwait(false);
  }

  private async Task SetupAndRunStageAsync(
    HolonBootstrapperLifetime lifetime, IEnumerable<HolonRegistration> holonRegistrations)
  {
    ConcurrentBag<Action<ContainerBuilder>> sharedLifetimeSetupActions = new();

    ConcurrentDictionary<HolonRegistration, IHolonSetup> holonSetups = new();
    await Parallel.ForEachAsync(holonRegistrations, async (holonRegistration, _) =>
    {
      var setup = (IHolonSetup) lifetime.SharedLifetimeScope.Resolve(holonRegistration.SetupType);

      await holonRegistration.ExternalConfiguration(setup).ConfigureAwait(false);

      await setup.InitializeAsync().ConfigureAwait(false);

      if (setup is IHolonSetupSharedLifetimeScope setupSharedLifetimeScope)
      {
        sharedLifetimeSetupActions.Add(setupSharedLifetimeScope.SetupSharedLifetime);
      }

      holonSetups[holonRegistration] = setup;
    });

    lifetime.SharedLifetimeScope = lifetime.SharedLifetimeScope.BeginLifetimeScope(x =>
    {
      foreach (var sharedLifetimeSetupAction in sharedLifetimeSetupActions)
      {
        sharedLifetimeSetupAction(x);
      }
    });

    ConcurrentDictionary<HolonRegistration, IHolonRuntime> runtimes = new();
    await Parallel.ForEachAsync(holonRegistrations, async (holonRegistration, _) =>
    {
      ILifetimeScope localLifetimeScope;

      var setup = holonSetups[holonRegistration];

      if (setup is IHolonSetupLocalLifetimeScope setupLocalLifetimeScope)
      {
        localLifetimeScope = lifetime.SharedLifetimeScope.BeginLifetimeScope(setupLocalLifetimeScope.SetupLocalLifetime);
      }
      else
      {
        localLifetimeScope = lifetime.SharedLifetimeScope.BeginLifetimeScope();
      }

      if (setup is IHolonSetupCustom setupCustom)
      {
        await setupCustom.SetupCustomAsync().ConfigureAwait(false);
      }

      if (setup is IHolonSetupCompletion setupCompletion)
      {
        await setupCompletion.OnSetupCompletedAsync(localLifetimeScope).ConfigureAwait(false);
      }

      var runtime = (IHolonRuntime) localLifetimeScope.Resolve(holonRegistration.RuntimeType);
      await runtime.InitializeAsync().ConfigureAwait(false);
      runtimes[holonRegistration] = runtime;
    });

    Parallel.ForEach(runtimes, runtime =>
    {
      lifetime.Registrations.Add(new HolonLifetimeRegistration(runtime.Key, Task.Run(() => runtime.Value.RunAsync(lifetime.CancellationTokenSource.Token))));
    });
  }
}

public sealed class HolonBootstrapper : HolonBootstrapperBase
{
  private HolonBootstrapper() : base()
  {

  }
  public static IHolonBootstrapper Create()
    => new HolonBootstrapper();

}
