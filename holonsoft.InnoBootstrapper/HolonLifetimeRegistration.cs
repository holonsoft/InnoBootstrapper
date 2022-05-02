namespace holonsoft.InnoBootstrapper;
internal class HolonLifetimeRegistration
{
  internal HolonRegistration Registration { get; init; }
  internal Task RuntimeTask { get; init; }

  public HolonLifetimeRegistration(HolonRegistration registration, Task runtimeTask)
  {
    RuntimeTask = runtimeTask;
    Registration = registration;
  }
}
