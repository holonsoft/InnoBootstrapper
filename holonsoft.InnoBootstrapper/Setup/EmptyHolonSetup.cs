using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;

namespace holonsoft.InnoBootstrapper.Setup;
internal class EmptyHolonSetup : IHolonSetup
{
  public Task InitializeAsync()
    => Task.CompletedTask;
}
