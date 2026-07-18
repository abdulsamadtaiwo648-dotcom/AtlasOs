using Atlas.SmartHome.Models;

namespace Atlas.SmartHome.Interfaces;

/// <summary>
/// The primary provider interface that Atlas interacts with to control smart home environments.
/// All specific ecosystems (Home Assistant, MQTT, Tuya, Simulation) must implement this.
/// </summary>
public interface ISmartHomeProvider : 
    IDeviceDiscovery, 
    ILightController, 
    IFanController, 
    IDoorController, 
    ICameraController, 
    IThermostatController, 
    ICurtainController
{
    /// <summary>
    /// Gets the name of the provider (e.g. "Simulation", "HomeAssistant").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Initializes the provider connection.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Fallback method for sending a raw or generic command to a device if it doesn't fit standard interfaces.
    /// </summary>
    Task ExecuteGenericCommandAsync(string deviceId, string command, Dictionary<string, object> parameters);
}
