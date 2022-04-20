using holonsoft.InnoBootstrapper.Abstractions.Enums;

namespace holonsoft.InnoBootstrapper.Abstractions.Attributes;
public class HolonSetupStageAttribute : Attribute
{
  public HolonSetupStage SetupStage { get; init; }
  public HolonSetupStageAttribute(string setupStageName)
    => SetupStage = HolonSetupStage.Parse(setupStageName);
}
