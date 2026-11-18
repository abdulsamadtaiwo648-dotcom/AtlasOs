namespace Atlas.SmartHome.Models;

public enum DeviceType
{
    Light,
    Fan,
    Door,
    GarageDoor,
    Camera,
    Thermostat,
    Curtain,
    TV,
    Sensor,
    Other
}

public class DeviceState
{
    public bool IsOn { get; set; }
    public int Brightness { get; set; }
    public string Color { get; set; } = "";
    public string Speed { get; set; } = "";
    public int OpenPercentage { get; set; }
    public double Temperature { get; set; }
    public string CustomState { get; set; } = "";
}

public class Device
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public DeviceType Type { get; set; }
    public string RoomId { get; set; } = "";
    public string ProviderName { get; set; } = "";
    public DeviceState State { get; set; } = new();
}

public class Room
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public List<string> DeviceIds { get; set; } = new();
}

public class AutomationRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
}
