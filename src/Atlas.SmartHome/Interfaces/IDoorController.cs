using Atlas.SmartHome.Models;

namespace Atlas.SmartHome.Interfaces;

/// <summary>
/// Controls doors and garages.
/// </summary>
public interface IDoorController
{
    Task OpenDoorAsync(string deviceId);
    Task CloseDoorAsync(string deviceId);
    Task LockDoorAsync(string deviceId);
    Task UnlockDoorAsync(string deviceId);
}
