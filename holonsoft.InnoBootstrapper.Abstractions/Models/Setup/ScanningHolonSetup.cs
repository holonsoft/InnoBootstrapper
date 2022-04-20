using Autofac;
using holonsoft.InnoBootstrapper.Abstractions.Contracts;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;
using holonsoft.InnoBootstrapper.Abstractions.Models.Runtime;
using holonsoft.Utils;

namespace holonsoft.InnoBootstrapper.Abstractions.Models.Setup;
public class ScanningHolonSetup : IHolonSetup
{
  private readonly IHolonBootstrapper _bootstrapper;
  private Func<(Type SetupType, Type RuntimeType), bool> _predicate;
  private Action<IHolonBootstrapper, (Type SetupType, Type RuntimeType)> _addMethod;

  public ScanningHolonSetup(IHolonBootstrapper bootstrapper)
  {
    _bootstrapper = bootstrapper;
    _addMethod = (bootstrapper, types) => bootstrapper.AddHolon(types.SetupType, types.RuntimeType);
    _predicate = (types) => true;
  }

  public ScanningHolonSetup FilterByPredicate(Func<(Type SetupType, Type RuntimeType), bool> predicate)
  {
    _predicate = predicate;
    return this;
  }

  public ScanningHolonSetup UseAddMethod(Action<IHolonBootstrapper, (Type SetupType, Type RuntimeType)> addMethod)
  {
    _addMethod = addMethod;
    return this;
  }

  Task IHolonSetup.InitializeAsync()
  {
    void FoundHolon(Type setupType, Type runtimeType)
    {
      if (_predicate((setupType, runtimeType)))
      {
        _addMethod(_bootstrapper, (setupType, runtimeType));
      }
    }

    var setupTypes = ReflectionUtils.AllTypes.Values.Where(x => typeof(IHolonSetup).IsAssignableFrom(x)).ToHashSet();
    var runtimeTypes = ReflectionUtils.AllTypes.Values.Where(x => typeof(IHolonRuntime).IsAssignableFrom(x)).ToHashSet();

    var assignedTypes =
      setupTypes.SelectMany(x => x.GetInterfaces().Where(y => y.IsGenericType && y.GetGenericTypeDefinition() == typeof(IHolonSetup<>)).Select(y => (SetupType: x, Interface: y)))
                .Select(x => (SetupType: x.SetupType, RuntimeType: x.Interface.GetGenericArguments().Single()))
                .ToArray();

    setupTypes.ExceptWith(assignedTypes.Select(x => x.SetupType));
    runtimeTypes.ExceptWith(assignedTypes.Select(x => x.RuntimeType));

    foreach ((var setupType, var runtimeType) in assignedTypes)
    {
      FoundHolon(setupType, runtimeType);
    }

    foreach (var runtimeType in runtimeTypes)
    {
      FoundHolon(typeof(EmptyHolonSetup), runtimeType);
    }

    foreach (var setupType in setupTypes)
    {
      FoundHolon(setupType, typeof(EmptyHolonRuntime));
    }

    return Task.CompletedTask;
  }
}