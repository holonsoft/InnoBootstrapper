using holonsoft.InnoBootstrapper.Abstractions.Contracts;
using Microsoft.Extensions.Hosting;

namespace holonsoft.InnoBootstrapper.HostingExtensions;
internal class HolonBootstrapperHostedServiceWrapper : IHostedService
{
  private readonly IHolonBootstrapper _bootstrapper;

  internal HolonBootstrapperHostedServiceWrapper(IHolonBootstrapper bootstrapper)
    => _bootstrapper = bootstrapper;

  Task IHostedService.StartAsync(CancellationToken cancellationToken)
   => _bootstrapper.RunAsync();
  Task IHostedService.StopAsync(CancellationToken cancellationToken)
    => _bootstrapper.StopAsync();
}
