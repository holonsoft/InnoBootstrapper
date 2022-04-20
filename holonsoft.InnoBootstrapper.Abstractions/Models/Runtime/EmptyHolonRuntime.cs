using holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;

namespace holonsoft.InnoBootstrapper.Abstractions.Models.Runtime;
public class EmptyHolonRuntime : IHolonRuntime
{
  public Task InitializeAsync()
    => Task.CompletedTask;
  public Task RunAsync(CancellationToken stoppingToken)
    => Task.CompletedTask;
}
