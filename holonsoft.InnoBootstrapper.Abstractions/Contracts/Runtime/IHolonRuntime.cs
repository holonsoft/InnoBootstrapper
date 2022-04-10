namespace holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;
public interface IHolonRuntime
{
  public Task InitializeAsync();
  public Task RunAsync(CancellationToken stoppingToken);
}
