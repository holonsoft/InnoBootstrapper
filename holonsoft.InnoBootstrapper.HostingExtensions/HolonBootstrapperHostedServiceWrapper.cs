using holonsoft.InnoBootstrapper.Abstractions.Contracts;
using Microsoft.Extensions.Hosting;

namespace holonsoft.InnoBootstrapper.HostingExtensions;
internal class HolonBootstrapperHostedServiceWrapper : IHostedService
{
  private readonly IHolonBootstrapper _bootstrapper;
  private IHolonBootstrapperLifetime? _bootstrapperLifetime;

  internal HolonBootstrapperHostedServiceWrapper(IHolonBootstrapper bootstrapper)
    => _bootstrapper = bootstrapper;

  async Task IHostedService.StartAsync(CancellationToken cancellationToken)
   => _bootstrapperLifetime = await _bootstrapper.StartAsync();

  async Task IHostedService.StopAsync(CancellationToken cancellationToken)
  {
    if (_bootstrapperLifetime != null)
    {
      await _bootstrapperLifetime.StopAsync();
    }
  }

}
