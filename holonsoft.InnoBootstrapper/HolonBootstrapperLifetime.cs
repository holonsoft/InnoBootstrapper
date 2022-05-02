using Autofac;
using holonsoft.InnoBootstrapper.Abstractions.Contracts;
using System.Collections.Concurrent;

namespace holonsoft.InnoBootstrapper;
internal class HolonBootstrapperLifetime : IHolonBootstrapperLifetime
{
  private const int _defaultGracefulTeardownPeriodSeconds = 30;

  private readonly ILifetimeScope _rootLifetimeScope;

  internal ConcurrentBag<HolonLifetimeRegistration> Registrations { get; private set; }
  internal CancellationTokenSource CancellationTokenSource { get; private set; }

  public ILifetimeScope SharedLifetimeScope { get; internal set; }

  public HolonBootstrapperLifetime(
    ILifetimeScope rootLifetimeScope,
    CancellationTokenSource cancellationTokenSource)
  {
    _rootLifetimeScope = rootLifetimeScope;
    CancellationTokenSource = cancellationTokenSource;

    Registrations = new();

    SharedLifetimeScope = rootLifetimeScope.BeginLifetimeScope(
      x => x.RegisterInstance(this)
            .As<IHolonBootstrapperLifetime>()
            .SingleInstance());
  }

  private async Task WaitForShutdownInternalAsync(TimeSpan? gracefulTeardownPeriod)
  {
    try
    {
      Task[] GetAllTasks()
        => Registrations
            .Select(x => x.RuntimeTask)
            .Where(x => !x.IsCompleted)
            .ToArray();

      Task[] allTasks;
      while (!CancellationTokenSource.IsCancellationRequested && (allTasks = GetAllTasks()).Length > 0)
      {
        try
        {
          await Task.WhenAll(allTasks).WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
        }
      }

      gracefulTeardownPeriod ??= TimeSpan.FromSeconds(_defaultGracefulTeardownPeriodSeconds);

      while ((allTasks = GetAllTasks()).Length > 0)
      {
        try
        {
          await Task.WhenAll(allTasks).WaitAsync(gracefulTeardownPeriod.Value).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
        }
      }
    }
    finally
    {
      if (_rootLifetimeScope != null)
      {
        await _rootLifetimeScope.DisposeAsync().ConfigureAwait(false);
      }
    }
  }

  public async Task WaitAsync(TimeSpan? gracefulTeardownPeriod = default)
    => await WaitForShutdownInternalAsync(gracefulTeardownPeriod).ConfigureAwait(false);

  public async Task StopAsync(TimeSpan? gracefulTeardownPeriod = default)
  {
    CancellationTokenSource.Cancel();
    await WaitForShutdownInternalAsync(gracefulTeardownPeriod).ConfigureAwait(false);
  }
}
