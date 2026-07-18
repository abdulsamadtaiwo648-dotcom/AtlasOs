using Atlas.SmartHome.Models;

namespace Atlas.SmartHome.Interfaces;

/// <summary>
/// Responsible for discovering and querying devices in the smart home ecosystem.
/// </summary>
public interface IDeviceDiscovery
{
    Task<List<Device>> GetDevicesAsync();
    Task<Device?> GetDeviceByIdAsync(string deviceId);
    Task<Device?> GetDeviceByNameAsync(string name);
    Task<List<Room>> GetRoomsAsync();
}
