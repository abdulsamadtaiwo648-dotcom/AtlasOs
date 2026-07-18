using Atlas.SmartHome.Interfaces;
using Atlas.SmartHome.Models;

namespace Atlas.SmartHome.Services;

/// <summary>
/// The brain of the smart home module. Analyzes natural language input and dispatches 
/// the correct commands to the ISmartHomeProvider.
/// </summary>
public class SmartHomeEngine
{
    private readonly ISmartHomeProvider _provider;

    public SmartHomeEngine(ISmartHomeProvider provider)
    {
        _provider = provider;
    }

    public async Task<string> ProcessCommandAsync(string input)
    {
        string text = input.ToLowerInvariant();
        var devices = await _provider.GetDevicesAsync();

        bool isTurnOn = text.Contains("turn on") || text.Contains("open") || text.Contains("start");
        bool isTurnOff = text.Contains("turn off") || text.Contains("close") || text.Contains("stop");
        bool isAll = text.Contains("every") || text.Contains("all");

        // Handle "all" commands
        if (isAll)
        {
            if (isTurnOff && text.Contains("light"))
            {
                var lights = devices.Where(d => d.Type == DeviceType.Light);
                foreach (var light in lights) await _provider.TurnOffLightAsync(light.Id);
                return "All lights have been turned off.";
            }

            if (isTurnOff)
            {
                foreach (var device in devices)
                {
                    if (device.Type == DeviceType.Light) await _provider.TurnOffLightAsync(device.Id);
                    else if (device.Type == DeviceType.Fan) await _provider.TurnOffFanAsync(device.Id);
                    else if (device.Type == DeviceType.Thermostat) await _provider.TurnOffThermostatAsync(device.Id);
                    // Add more types as needed
                }
                return "Everything has been turned off.";
            }
        }

        // Try to match specific device names
        var matchedDevice = devices.FirstOrDefault(d => text.Contains(d.Name.ToLowerInvariant()));

        if (matchedDevice != null)
        {
            if (isTurnOn)
            {
                await _provider.ExecuteGenericCommandAsync(matchedDevice.Id, "TurnOn", new());
                return $"{matchedDevice.Name} turned on successfully.";
            }
            if (isTurnOff)
            {
                await _provider.ExecuteGenericCommandAsync(matchedDevice.Id, "TurnOff", new());
                return $"{matchedDevice.Name} turned off successfully.";
            }

            // Handling queries like "Is the garage open?"
            if (text.Contains("is") || text.Contains("status") || text.Contains("state"))
            {
                var state = matchedDevice.State;
                if (matchedDevice.Type == DeviceType.GarageDoor || matchedDevice.Type == DeviceType.Door)
                {
                    string status = state.OpenPercentage > 0 ? "open" : "closed";
                    return $"The {matchedDevice.Name.ToLowerInvariant()} is currently {status}.";
                }
                
                string onOff = state.IsOn ? "on" : "off";
                return $"The {matchedDevice.Name.ToLowerInvariant()} is currently {onOff}.";
            }
        }

        return "I could not find the device or understand the smart home command.";
    }
}
