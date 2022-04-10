using holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;

namespace holonsoft.InnoBootstrapper.Runtime;
internal class EmptyHolonRuntime : IHolonRuntime
{
  public Task InitializeAsync()
    => Task.CompletedTask;
  public Task RunAsync(CancellationToken stoppingToken)
    => Task.CompletedTask;
}
