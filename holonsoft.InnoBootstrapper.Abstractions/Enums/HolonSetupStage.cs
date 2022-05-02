using holonsoft.Utils;

namespace holonsoft.InnoBootstrapper.Abstractions.Enums;
public record HolonSetupStage : ExtensibleEnum<HolonSetupStage>
{
  /// <summary>
  ///   Absolute first level. The bootstrapper injects itself into the root container.
  ///   You can initialize ILogger for Bootstrapping phase - no access to configuration or anything.
  ///   Holon scanner work from here and can load plugins into later levels.
  ///   If an outer application environment exists at this level (e.g. we run inside a blazor application)
  ///   we can inject services provided by the outer platform at this point.
  /// </summary>
  public static readonly HolonSetupStage InitializeBootstrapper = new(0, nameof(InitializeBootstrapper));

  /// <summary>
  ///   Configure needed environment or determine the environment.
  ///   Security related setup.
  /// </summary>
  public static readonly HolonSetupStage InitializeEnvironment = new(1000, nameof(InitializeEnvironment));

  /// <summary>
  ///   Initialize configuration access e.g. IConfiguration on json files.
  /// </summary>
  public static readonly HolonSetupStage InitializeConfiguration = new(2000, nameof(InitializeConfiguration));

  /// <summary>
  ///   Initialize the real logger on the framework and/or sink you like.
  /// </summary>
  public static readonly HolonSetupStage InitializeLogging = new(3000, nameof(InitializeLogging));

  /// <summary>
  ///   Initialize the communication to host or inter process communication like the NoQBus
  /// </summary>
  public static readonly HolonSetupStage InitializeCommunication = new(4000, nameof(InitializeCommunication));

  /// <summary>
  ///   Initialize basic user interface like STA WPF
  /// </summary>
  public static readonly HolonSetupStage InitializeUserInterface = new(5000, nameof(InitializeUserInterface));

  /// <summary>
  ///   Checks for updates and handles them
  /// </summary>
  public static readonly HolonSetupStage InitializeSelfUpdate = new(6000, nameof(InitializeSelfUpdate));

  /// <summary>
  ///   Configurationstores and services that depend on the previous levels.
  /// </summary>
  public static readonly HolonSetupStage BasicServices = new(10000, nameof(BasicServices));

  /// <summary>
  ///   Connections and access to services like databases.
  /// </summary>
  public static readonly HolonSetupStage CommonServices = new(20000, nameof(CommonServices));

  /// <summary>
  ///   The core logic services of your application.
  /// </summary>
  public static readonly HolonSetupStage BusinessServices = new(100000, nameof(BusinessServices));

  /// <summary>
  ///   Ultimate last level - application starts to run fully.
  /// </summary>
  public static readonly HolonSetupStage ApplicationRun = new(int.MaxValue, nameof(ApplicationRun));

  public HolonSetupStage(int value, string name) : base(value, name)
  {
  }
}
