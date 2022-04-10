using holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;

namespace holonsoft.InnoBootstrapper.Abstractions.Contracts;
public interface IHolonBootstrapper
{
  public Task AddHolonSetupAsync<THolonSetup>(Func<THolonSetup, Task> externalConfiguration) where THolonSetup : IHolonSetup;
  public Task AddHolonSetupAsync<THolonSetup>() where THolonSetup : IHolonSetup
    => AddHolonSetupAsync<THolonSetup>(x => Task.CompletedTask);
  public Task AddHolonSetupAsync<THolonSetup>(Action<THolonSetup> externalConfiguration) where THolonSetup : IHolonSetup
    => AddHolonSetupAsync<THolonSetup>(x =>
    {
      externalConfiguration(x);
      return Task.CompletedTask;
    });

  public Task AddHolonRuntimeAsync<THolonRuntime>() where THolonRuntime : IHolonRuntime;

  public Task AddHolonAsync<THolonSetup, THolonRuntime>(Func<THolonSetup, Task> externalConfiguration)
    where THolonSetup : IHolonSetup
    where THolonRuntime : IHolonRuntime;

  public Task AddHolonAsync<THolonSetup, THolonRuntime>()
    where THolonSetup : IHolonSetup
    where THolonRuntime : IHolonRuntime
    => AddHolonAsync<THolonSetup, THolonRuntime>(x => Task.CompletedTask);

  public Task AddHolonAsync<THolonSetup, THolonRuntime>(Action<THolonSetup> externalConfiguration)
    where THolonSetup : IHolonSetup
    where THolonRuntime : IHolonRuntime
    => AddHolonAsync<THolonSetup, THolonRuntime>(x =>
    {
      externalConfiguration(x);
      return Task.CompletedTask;
    });

  public Task AddHolonsByScan(Func<(Type SetupType, Type RuntimeType), bool> predicate);

  public Task AddHolonsByScan()
    => AddHolonsByScan(x => true);

  public Task RunAsync(CancellationToken stoppingToken = default);

  public Task RunAndWaitAsync(CancellationToken stoppingToken = default);

  public Task StopAsync();
}
