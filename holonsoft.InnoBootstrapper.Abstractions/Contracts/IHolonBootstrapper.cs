using holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;

namespace holonsoft.InnoBootstrapper.Abstractions.Contracts;
public interface IHolonBootstrapper
{
  public IHolonBootstrapper AddHolonSetup<THolonSetup>(Func<THolonSetup, Task> externalConfiguration) where THolonSetup : IHolonSetup;
  public IHolonBootstrapper AddHolonSetup<THolonSetup>() where THolonSetup : IHolonSetup
    => AddHolonSetup<THolonSetup>(x => Task.CompletedTask);
  public IHolonBootstrapper AddHolonSetup<THolonSetup>(Action<THolonSetup> externalConfiguration) where THolonSetup : IHolonSetup
    => AddHolonSetup<THolonSetup>(x =>
    {
      externalConfiguration(x);
      return Task.CompletedTask;
    });

  public IHolonBootstrapper AddHolonRuntime<THolonRuntime>() where THolonRuntime : IHolonRuntime;

  public IHolonBootstrapper AddHolon<THolonSetup, THolonRuntime>(Func<THolonSetup, Task> externalConfiguration)
    where THolonSetup : IHolonSetup
    where THolonRuntime : IHolonRuntime;

  public IHolonBootstrapper AddHolon<THolonSetup, THolonRuntime>()
    where THolonSetup : IHolonSetup
    where THolonRuntime : IHolonRuntime
    => AddHolon<THolonSetup, THolonRuntime>(x => Task.CompletedTask);

  public IHolonBootstrapper AddHolon<THolonSetup, THolonRuntime>(Action<THolonSetup> externalConfiguration)
    where THolonSetup : IHolonSetup
    where THolonRuntime : IHolonRuntime
    => AddHolon<THolonSetup, THolonRuntime>(x =>
    {
      externalConfiguration(x);
      return Task.CompletedTask;
    });

  public IHolonBootstrapper AddHolonsByScan(Func<(Type SetupType, Type RuntimeType), bool> predicate);

  public IHolonBootstrapper AddHolonsByScan()
    => AddHolonsByScan(x => true);

  public Task RunAsync(CancellationToken stoppingToken = default);

  public Task RunAndWaitAsync(CancellationToken stoppingToken = default);

  public Task StopAsync();
}
