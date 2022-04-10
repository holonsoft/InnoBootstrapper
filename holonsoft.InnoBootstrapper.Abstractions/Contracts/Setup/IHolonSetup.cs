using holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;

namespace holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;
public interface IHolonSetup
{
  public Task InitializeAsync();
}

public interface IHolonSetup<THolonRuntime> : IHolonSetup where THolonRuntime : IHolonRuntime
{

}