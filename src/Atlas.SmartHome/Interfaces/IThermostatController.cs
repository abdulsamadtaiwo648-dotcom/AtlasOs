using Atlas.SmartHome.Models;

namespace Atlas.SmartHome.Interfaces;

/// <summary>
/// Controls thermostats and HVAC devices.
/// </summary>
public interface IThermostatController
{
    Task TurnOnThermostatAsync(string deviceId);
    Task TurnOffThermostatAsync(string deviceId);
    Task SetTemperatureAsync(string deviceId, double temperature);
    Task SetModeAsync(string deviceId, string mode);
}
