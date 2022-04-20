using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;

namespace holonsoft.InnoBootstrapper.Abstractions.Models.Setup;
public class EmptyHolonSetup : IHolonSetup
{
  public Task InitializeAsync()
    => Task.CompletedTask;
}
