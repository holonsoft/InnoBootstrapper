using holonsoft.Utils;

namespace holonsoft.InnoBootstrapper.Abstractions.Enums;
public record HolonSetupStage : ExtensibleEnum<HolonSetupStage>
{
  public static readonly HolonSetupStage InitBootstrapper = new(0, nameof(InitBootstrapper)); //empty from scratch - mostly internals
  public static readonly HolonSetupStage BasicServices = new(1000, nameof(BasicServices)); //like logging and configuration
  public static readonly HolonSetupStage CommonServices = new(2000, nameof(CommonServices)); //like database access or connections
  public static readonly HolonSetupStage BusinessServices = new(3000, nameof(BusinessServices)); //core logics of application
  public static readonly HolonSetupStage ApplicationRun = new(int.MaxValue, nameof(ApplicationRun)); // last one - application starts to run fully

  public HolonSetupStage(int value, string name) : base(value, name)
  {
  }
}
