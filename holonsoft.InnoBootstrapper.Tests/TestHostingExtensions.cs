using holonsoft.InnoBootstrapper.Abstractions.Contracts.Runtime;
using holonsoft.InnoBootstrapper.Abstractions.Contracts.Setup;
using holonsoft.InnoBootstrapper.HostingExtensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace holonsoft.InnoBootstrapper.Tests;
public class TestHostingExtensions
{
  private class TestHolonRuntime : IHolonRuntime
  {
    public Task InitializeAsync()
      => Task.CompletedTask;
    public Task RunAsync(CancellationToken stoppingToken)
      => Task.Delay(Timeout.Infinite, stoppingToken);
  }

  private class TestHolonSetup : IHolonSetup<TestHolonRuntime>
  {
    public Task InitializeAsync()
      => Task.CompletedTask;
  }

  [Fact]
  public async Task TestBasicHostingWithPreBuildBootstrapper()
  {
    var bootstrapper
      = HolonBootstrapper.Create()
      .AddHolon<TestHolonSetup, TestHolonRuntime>();

    var builder = new HostBuilder()
      .ConfigureServices(x => x.AddHolonBootstrapper(bootstrapper))
      .Build();

    await builder.StartAsync();
    await builder.StopAsync();
  }

  [Fact]
  public async Task TestBasicHosting()
  {
    var builder = new HostBuilder()
      .ConfigureServices(x => x.AddHolonBootstrapper(x => HolonBootstrapper.Create().AddHolon<TestHolonSetup, TestHolonRuntime>()))
      .Build();

    await builder.StartAsync();
    await builder.StopAsync();
  }


}
