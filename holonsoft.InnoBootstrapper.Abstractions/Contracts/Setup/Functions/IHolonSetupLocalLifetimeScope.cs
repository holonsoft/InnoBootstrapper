using Autofac;

namespace holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup.Functions;
public interface IHolonSetupLocalLifetimeScope : IHolonSetupFunctions
{
  public void SetupLocalLifetime(ContainerBuilder containerBuilder);
}
