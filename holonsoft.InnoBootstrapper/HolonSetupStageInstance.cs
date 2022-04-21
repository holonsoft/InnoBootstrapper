using Autofac;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup.Functions;

namespace holonsoft.InnoBootstrapper;
internal class HolonSetupStageInstance
{
  private readonly IEnumerable<HolonRegistration> _holonRegistrations;
  private readonly ILifetimeScope _sharedLifeTimeScope;

  internal HolonSetupStageInstance(IEnumerable<HolonRegistration> holonRegistrations, ILifetimeScope sharedLifeTimeScope)
  {
    _holonRegistrations = holonRegistrations;
    _sharedLifeTimeScope = sharedLifeTimeScope;
  }

  private async Task<ILifetimeScope> SetupAsync()
  {
    List<Action<ContainerBuilder>> sharedLifetimeSetupActions = new();

    foreach (var holonRegistration in _holonRegistrations)
    {
      holonRegistration.Setup = (IHolonSetup) _sharedLifeTimeScope.Resolve(holonRegistration.SetupType);

      await holonRegistration.ExternalConfiguration(holonRegistration.Setup).ConfigureAwait(false);

      await holonRegistration.Setup.InitializeAsync().ConfigureAwait(false);

      if (holonRegistration.Setup is IHolonSetupSharedLifetimeScope setupSharedLifetimeScope)
      {
        sharedLifetimeSetupActions.Add(setupSharedLifetimeScope.SetupSharedLifetime);
      }
    }

    var newSharedLifetimeScope = _sharedLifeTimeScope.BeginLifetimeScope(x =>
    {
      foreach (var sharedLifetimeSetupAction in sharedLifetimeSetupActions)
      {
        sharedLifetimeSetupAction(x);
      }
    });

    foreach (var holonRegistration in _holonRegistrations)
    {
      if (holonRegistration.Setup is IHolonSetupLocalLifetimeScope setupLocalLifetimeScope)
      {
        holonRegistration.LocalLifetimeScope = newSharedLifetimeScope.BeginLifetimeScope(setupLocalLifetimeScope.SetupLocalLifetime);
      }
      else
      {
        holonRegistration.LocalLifetimeScope = newSharedLifetimeScope.BeginLifetimeScope();
      }

      if (holonRegistration.Setup is IHolonSetupCustom setupCustom)
      {
        await setupCustom.SetupCustomAsync().ConfigureAwait(false);
      }

      if (holonRegistration.Setup is IHolonSetupCompletion setupCompletion)
      {
        await setupCompletion.OnSetupCompletedAsync(holonRegistration.LocalLifetimeScope).ConfigureAwait(false);
      }
    }

    return newSharedLifetimeScope;
  }

  private async Task RunAsync(CancellationToken stoppingToken)
  {
    foreach (var holonRegistration in _holonRegistrations)
    {
      holonRegistration.Runtime = (IHolonRuntime) holonRegistration.LocalLifetimeScope!.Resolve(holonRegistration.RuntimeType);
      await holonRegistration.Runtime.InitializeAsync().ConfigureAwait(false);
    }

    foreach (var holonRegistration in _holonRegistrations)
    {
      holonRegistration.RuntimeTask = Task.Run(() => holonRegistration.Runtime!.RunAsync(stoppingToken));
    }
  }

  internal async Task<ILifetimeScope> SetupAndRunAsync(CancellationToken stoppingToken)
  {
    var newSharedLifetimeScope = await SetupAsync().ConfigureAwait(false);

    await RunAsync(stoppingToken).ConfigureAwait(false);

    return newSharedLifetimeScope;
  }

}
