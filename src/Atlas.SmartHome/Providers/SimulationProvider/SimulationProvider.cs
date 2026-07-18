using System.Collections.Concurrent;
using Atlas.SmartHome.Interfaces;
using Atlas.SmartHome.Models;

namespace Atlas.SmartHome.Providers.SimulationProvider;

/// <summary>
/// A simulated smart home provider that holds device states in memory.
/// Ideal for testing and local operation without physical hardware.
/// </summary>
public class SimulationProvider : ISmartHomeProvider
{
    private readonly ConcurrentDictionary<string, Device> _devices = new();
    private readonly ConcurrentDictionary<string, Room> _rooms = new();

    public string Name => "Simulation";

    public Task InitializeAsync()
    {
        // Seed default rooms
        var livingRoom = new Room { Name = "Living Room" };
        var kitchen = new Room { Name = "Kitchen" };
        var bedroom = new Room { Name = "Bedroom" };
        var garage = new Room { Name = "Garage" };

        _rooms.TryAdd(livingRoom.Id, livingRoom);
        _rooms.TryAdd(kitchen.Id, kitchen);
        _rooms.TryAdd(bedroom.Id, bedroom);
        _rooms.TryAdd(garage.Id, garage);

        // Seed default devices
        SeedDevice("Living Room Light", DeviceType.Light, livingRoom.Id);
        SeedDevice("Kitchen Fan", DeviceType.Fan, kitchen.Id);
        SeedDevice("Bedroom Light", DeviceType.Light, bedroom.Id);
        SeedDevice("Garage Door", DeviceType.GarageDoor, garage.Id);
        SeedDevice("Living Room Thermostat", DeviceType.Thermostat, livingRoom.Id);
        SeedDevice("Living Room TV", DeviceType.TV, livingRoom.Id);

        return Task.CompletedTask;
    }

    private void SeedDevice(string name, DeviceType type, string roomId)
    {
        var device = new Device
        {
            Name = name,
            Type = type,
            RoomId = roomId,
            ProviderName = Name
        };

        _devices.TryAdd(device.Id, device);
        
        if (_rooms.TryGetValue(roomId, out var room))
        {
            room.DeviceIds.Add(device.Id);
        }
    }

    // ==========================================
    // IDeviceDiscovery
    // ==========================================
    public Task<List<Device>> GetDevicesAsync()
    {
        return Task.FromResult(_devices.Values.ToList());
    }

    public Task<Device?> GetDeviceByIdAsync(string deviceId)
    {
        _devices.TryGetValue(deviceId, out var device);
        return Task.FromResult(device);
    }

    public Task<Device?> GetDeviceByNameAsync(string name)
    {
        var device = _devices.Values.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(device);
    }

    public Task<List<Room>> GetRoomsAsync()
    {
        return Task.FromResult(_rooms.Values.ToList());
    }

    // ==========================================
    // ILightController
    // ==========================================
    public Task TurnOnLightAsync(string deviceId) => UpdateState(deviceId, state => state.IsOn = true);
    public Task TurnOffLightAsync(string deviceId) => UpdateState(deviceId, state => state.IsOn = false);
    public Task SetLightBrightnessAsync(string deviceId, int brightness) => UpdateState(deviceId, state => state.Brightness = brightness);
    public Task SetLightColorAsync(string deviceId, string colorHex) => UpdateState(deviceId, state => state.Color = colorHex);

    // ==========================================
    // IFanController
    // ==========================================
    public Task TurnOnFanAsync(string deviceId) => UpdateState(deviceId, state => state.IsOn = true);
    public Task TurnOffFanAsync(string deviceId) => UpdateState(deviceId, state => state.IsOn = false);
    public Task SetFanSpeedAsync(string deviceId, string speed) => UpdateState(deviceId, state => state.Speed = speed);

    // ==========================================
    // IDoorController
    // ==========================================
    public Task OpenDoorAsync(string deviceId) => UpdateState(deviceId, state => state.OpenPercentage = 100);
    public Task CloseDoorAsync(string deviceId) => UpdateState(deviceId, state => state.OpenPercentage = 0);
    public Task LockDoorAsync(string deviceId) => UpdateState(deviceId, state => state.CustomState = "Locked");
    public Task UnlockDoorAsync(string deviceId) => UpdateState(deviceId, state => state.CustomState = "Unlocked");

    // ==========================================
    // ICameraController
    // ==========================================
    public Task<string> GetCameraStreamUrlAsync(string deviceId)
    {
        return Task.FromResult("http://localhost:8080/simulation/stream");
    }

    public Task TakeSnapshotAsync(string deviceId, string savePath)
    {
        // Simulate saving snapshot
        return Task.CompletedTask;
    }

    // ==========================================
    // IThermostatController
    // ==========================================
    public Task TurnOnThermostatAsync(string deviceId) => UpdateState(deviceId, state => state.IsOn = true);
    public Task TurnOffThermostatAsync(string deviceId) => UpdateState(deviceId, state => state.IsOn = false);
    public Task SetTemperatureAsync(string deviceId, double temperature) => UpdateState(deviceId, state => state.Temperature = temperature);
    public Task SetModeAsync(string deviceId, string mode) => UpdateState(deviceId, state => state.CustomState = mode);

    // ==========================================
    // ICurtainController
    // ==========================================
    public Task OpenCurtainAsync(string deviceId) => UpdateState(deviceId, state => state.OpenPercentage = 100);
    public Task CloseCurtainAsync(string deviceId) => UpdateState(deviceId, state => state.OpenPercentage = 0);
    public Task SetCurtainPositionAsync(string deviceId, int openPercentage) => UpdateState(deviceId, state => state.OpenPercentage = openPercentage);

    // ==========================================
    // Generic
    // ==========================================
    public Task ExecuteGenericCommandAsync(string deviceId, string command, Dictionary<string, object> parameters)
    {
        // For devices like TV, Speaker
        if (command.Equals("TurnOn", StringComparison.OrdinalIgnoreCase))
            return UpdateState(deviceId, state => state.IsOn = true);
        
        if (command.Equals("TurnOff", StringComparison.OrdinalIgnoreCase))
            return UpdateState(deviceId, state => state.IsOn = false);
            
        return UpdateState(deviceId, state => state.CustomState = command);
    }

    private Task UpdateState(string deviceId, Action<DeviceState> updateAction)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            updateAction(device.State);
        }
        else
        {
            throw new Exception($"Simulated device not found: {deviceId}");
        }

        return Task.CompletedTask;
    }
}
