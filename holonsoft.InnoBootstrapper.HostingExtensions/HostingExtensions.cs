using holonsoft.InnoBootstrapper.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace holonsoft.InnoBootstrapper.HostingExtensions;
public static class HostingExtensions
{
  public static IServiceCollection AddHolonBootstrapper(this IServiceCollection services, IHolonBootstrapper holonBootstrapper)
    => services.AddHostedService(x => new HolonBootstrapperHostedServiceWrapper(holonBootstrapper));

  public static IServiceCollection AddHolonBootstrapper(this IServiceCollection services, Func<IServiceProvider, IHolonBootstrapper> implementationFactory)
    => services.AddHostedService(x => new HolonBootstrapperHostedServiceWrapper(implementationFactory(x)));
}