using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;
using holonsoft.InnoBootstrapper.Abstractions.Enums;

namespace holonsoft.InnoBootstrapper;
internal class HolonRegistration
{
  internal HolonSetupStage SetupStage { get; init; }
  internal Type SetupType { get; init; }
  internal Type RuntimeType { get; init; }
  internal Func<IHolonSetup, Task> ExternalConfiguration { get; init; }

  public HolonRegistration(HolonSetupStage setupStage, Type setupType, Type runtimeType, Func<IHolonSetup, Task> externalConfiguration)
  {
    SetupStage = setupStage;
    SetupType = setupType;
    RuntimeType = runtimeType;
    ExternalConfiguration = externalConfiguration;
  }
}
