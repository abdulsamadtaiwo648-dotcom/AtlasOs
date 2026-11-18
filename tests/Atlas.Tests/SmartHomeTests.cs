using Atlas.SmartHome.Models;
using Atlas.SmartHome.Providers.SimulationProvider;
using Atlas.SmartHome.Services;

namespace Atlas.Tests;

public class SmartHomeTests
{
    private static async Task<(SimulationProvider provider, SmartHomeEngine engine)> CreateAsync()
    {
        SimulationProvider provider = new();
        await provider.InitializeAsync();
        return (provider, new SmartHomeEngine(provider));
    }

    [Fact]
    public async Task Initialize_SeedsRoomsAndDevices()
    {
        (SimulationProvider provider, _) = await CreateAsync();

        Assert.NotEmpty(await provider.GetDevicesAsync());
        Assert.NotEmpty(await provider.GetRoomsAsync());
    }

    [Fact]
    public async Task TurnOnLight_ByName_UpdatesState()
    {
        (SimulationProvider provider, SmartHomeEngine engine) = await CreateAsync();

        string reply = await engine.ProcessCommandAsync("turn on living room light");

        Device? light = await provider.GetDeviceByNameAsync("Living Room Light");
        Assert.Contains("turned on", reply);
        Assert.True(light!.State.IsOn);
    }

    [Fact]
    public async Task TurnOffEverything_SwitchesLightsOff()
    {
        (SimulationProvider provider, SmartHomeEngine engine) = await CreateAsync();
        await engine.ProcessCommandAsync("turn on living room light");

        await engine.ProcessCommandAsync("turn off everything");

        IEnumerable<Device> lights = (await provider.GetDevicesAsync()).Where(d => d.Type == DeviceType.Light);
        Assert.All(lights, l => Assert.False(l.State.IsOn));
    }
}
