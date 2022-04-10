using Autofac;

namespace holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup.Functions;
public interface IHolonSetupSharedLifetimeScope
{
  public void SetupSharedLifetime(ContainerBuilder containerBuilder);
}
