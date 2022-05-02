using Autofac;

namespace holonsoft.InnoBootstrapper.Abstractions.Contracts;
public interface IHolonBootstrapperLifetime
{
  public ILifetimeScope SharedLifetimeScope { get; }

  public Task WaitAsync(TimeSpan? gracefulTeardownPeriod = default);
  public Task StopAsync(TimeSpan? gracefulTeardownPeriod = default);
}
