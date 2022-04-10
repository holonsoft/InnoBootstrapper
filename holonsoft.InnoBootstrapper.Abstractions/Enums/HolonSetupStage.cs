namespace holonsoft.InnoBootstrapper.Abstractions.Enums;
public enum HolonSetupStage
{
  InitBootstrapper = 0, //empty from scratch - mostly internals
  BasicServices = 1, //like logging and configuration
  CommonServices = 2, //like database access or connections
  BusinessServices = 3, //core logics of application
  ApplicationRun = 4, // last one - application starts to run fully
}
