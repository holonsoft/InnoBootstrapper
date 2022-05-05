using holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;
using holonsoft.InnoBootstrapper.Abstractions.Enums;

namespace holonsoft.InnoBootstrapper.Abstractions.Contracts;
public interface IHolonBootstrapper
{
  public IHolonBootstrapper AddHolon(HolonSetupStage? setupStage, Type? setupType, Type? runtimeType, Func<IHolonSetup, Task>? externalConfiguration);

  public Task<IHolonBootstrapperLifetime> StartAsync(CancellationToken stoppingToken = default);

  public IHolonBootstrapper AddHolon(HolonSetupStage? setupStage, Type? setupType, Type? runtimeType, Action<IHolonSetup> externalConfiguration)
    => AddHolon(setupStage, setupType, runtimeType, x =>
    {
      externalConfiguration(x);
      return Task.CompletedTask;
    });
  public IHolonBootstrapper AddHolon(Type setupType, Type runtimeType, Func<IHolonSetup, Task> externalConfiguration)
    => AddHolon(null, setupType, runtimeType, externalConfiguration);
  public IHolonBootstrapper AddHolon(Type setupType, Type runtimeType, Action<IHolonSetup> externalConfiguration)
    => AddHolon(null, setupType, runtimeType, externalConfiguration);
  public IHolonBootstrapper AddHolon(Type setupType, Type runtimeType)
    => AddHolon(null, setupType, runtimeType, null);
  public IHolonBootstrapper AddHolon(HolonSetupStage setupStage, Type setupType, Type runtimeType)
    => AddHolon(setupStage, setupType, runtimeType, null);

  public IHolonBootstrapper AddHolon<THolonSetup, THolonRuntime>()
    where THolonSetup : IHolonSetup
    where THolonRuntime : IHolonRuntime
    => AddHolon(null, typeof(THolonSetup), typeof(THolonRuntime), null);
  public IHolonBootstrapper AddHolon<THolonSetup, THolonRuntime>(Func<THolonSetup, Task> externalConfiguration)
    where THolonSetup : IHolonSetup
    where THolonRuntime : IHolonRuntime
    => AddHolon(null, typeof(THolonSetup), typeof(THolonRuntime), x => externalConfiguration((THolonSetup) x));
  public IHolonBootstrapper AddHolon<THolonSetup, THolonRuntime>(Action<THolonSetup> externalConfiguration)
    where THolonSetup : IHolonSetup
    where THolonRuntime : IHolonRuntime
    => AddHolon(null, typeof(THolonSetup), typeof(THolonRuntime), x => externalConfiguration((THolonSetup) x));
  public IHolonBootstrapper AddHolon<THolonSetup, THolonRuntime>(HolonSetupStage setupStage)
    where THolonSetup : IHolonSetup
    where THolonRuntime : IHolonRuntime
    => AddHolon(setupStage, typeof(THolonSetup), typeof(THolonRuntime), null);
  public IHolonBootstrapper AddHolon<THolonSetup, THolonRuntime>(HolonSetupStage setupStage, Action<THolonSetup> externalConfiguration)
    where THolonSetup : IHolonSetup
    where THolonRuntime : IHolonRuntime
    => AddHolon(setupStage, typeof(THolonSetup), typeof(THolonRuntime), x => externalConfiguration((THolonSetup) x));
  public IHolonBootstrapper AddHolon<THolonSetup, THolonRuntime>(HolonSetupStage setupStage, Func<THolonSetup, Task> externalConfiguration)
    where THolonSetup : IHolonSetup
    where THolonRuntime : IHolonRuntime
    => AddHolon(setupStage, typeof(THolonSetup), typeof(THolonRuntime), x => externalConfiguration((THolonSetup) x));

  public IHolonBootstrapper AddHolonSetup<THolonSetup>() where THolonSetup : IHolonSetup
    => AddHolon(null, typeof(THolonSetup), null, null);
  public IHolonBootstrapper AddHolonSetup<THolonSetup>(Func<THolonSetup, Task> externalConfiguration) where THolonSetup : IHolonSetup
    => AddHolon(null, typeof(THolonSetup), null, x => externalConfiguration((THolonSetup) x));
  public IHolonBootstrapper AddHolonSetup<THolonSetup>(Action<THolonSetup> externalConfiguration) where THolonSetup : IHolonSetup
    => AddHolon(null, typeof(THolonSetup), null, x => externalConfiguration((THolonSetup) x));
  public IHolonBootstrapper AddHolonSetup<THolonSetup>(HolonSetupStage setupStage) where THolonSetup : IHolonSetup
    => AddHolon(setupStage, typeof(THolonSetup), null, null);
  public IHolonBootstrapper AddHolonSetup<THolonSetup>(HolonSetupStage setupStage, Action<THolonSetup> externalConfiguration) where THolonSetup : IHolonSetup
    => AddHolon(setupStage, typeof(THolonSetup), null, x => externalConfiguration((THolonSetup) x));
  public IHolonBootstrapper AddHolonSetup<THolonSetup>(HolonSetupStage setupStage, Func<THolonSetup, Task> externalConfiguration) where THolonSetup : IHolonSetup
    => AddHolon(setupStage, typeof(THolonSetup), null, x => externalConfiguration((THolonSetup) x));
  public IHolonBootstrapper AddHolonSetup(Type setupType)
    => AddHolon(null, setupType, null, null);
  public IHolonBootstrapper AddHolonSetup(Type setupType, Func<IHolonSetup, Task> externalConfiguration)
    => AddHolon(null, setupType, null, externalConfiguration);
  public IHolonBootstrapper AddHolonSetup(Type setupType, Action<IHolonSetup> externalConfiguration)
    => AddHolon(null, setupType, null, externalConfiguration);
  public IHolonBootstrapper AddHolonSetup(HolonSetupStage setupStage, Type setupType)
    => AddHolon(setupStage, setupType, null, null);
  public IHolonBootstrapper AddHolonSetup(HolonSetupStage setupStage, Type setupType, Func<IHolonSetup, Task> externalConfiguration)
    => AddHolon(setupStage, setupType, null, externalConfiguration);
  public IHolonBootstrapper AddHolonSetup(HolonSetupStage setupStage, Type setupType, Action<IHolonSetup> externalConfiguration)
    => AddHolon(setupStage, setupType, null, externalConfiguration);

  public IHolonBootstrapper AddHolonRuntime<THolonRuntime>() where THolonRuntime : IHolonRuntime
    => AddHolon(null, null, typeof(THolonRuntime), null);
  public IHolonBootstrapper AddHolonRuntime<THolonRuntime>(HolonSetupStage setupStage) where THolonRuntime : IHolonRuntime
    => AddHolon(setupStage, null, typeof(THolonRuntime), null);
  public IHolonBootstrapper AddHolonRuntime(Type runtimeType)
    => AddHolon(null, null, runtimeType, null);
  public IHolonBootstrapper AddHolonRuntime(HolonSetupStage setupStage, Type runtimeType)
    => AddHolon(setupStage, null, runtimeType, null);
}
