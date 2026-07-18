using Atlas.SmartHome.Models;

namespace Atlas.SmartHome.Interfaces;

/// <summary>
/// Controls fan devices.
/// </summary>
public interface IFanController
{
    Task TurnOnFanAsync(string deviceId);
    Task TurnOffFanAsync(string deviceId);
    Task SetFanSpeedAsync(string deviceId, string speed);
}
