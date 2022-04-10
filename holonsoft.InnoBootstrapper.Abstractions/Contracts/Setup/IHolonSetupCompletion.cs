using Autofac;

namespace holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;
public interface IHolonSetupCompletion
{
  public Task OnSetupCompletedAsync(ILifetimeScope localLifetimeScope);
}
