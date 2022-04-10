namespace holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;
public interface IHolonInformation
{
  public string TechnicalName { get; }
  public string UserFriendlyName { get; }
  public string Description { get; }
  public string Version { get; }
  public string ConfigurationInformation { get; }
  public Dictionary<string, object> CustomInformation { get; }
}
