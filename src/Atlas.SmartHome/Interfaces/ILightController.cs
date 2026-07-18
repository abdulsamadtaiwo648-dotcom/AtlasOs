using Atlas.SmartHome.Models;

namespace Atlas.SmartHome.Interfaces;

/// <summary>
/// Controls lighting devices.
/// </summary>
public interface ILightController
{
    Task TurnOnLightAsync(string deviceId);
    Task TurnOffLightAsync(string deviceId);
    Task SetLightBrightnessAsync(string deviceId, int brightness);
    Task SetLightColorAsync(string deviceId, string colorHex);
}
