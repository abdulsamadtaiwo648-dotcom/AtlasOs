using Atlas.SmartHome.Models;

namespace Atlas.SmartHome.Interfaces;

/// <summary>
/// Controls motorized curtains and blinds.
/// </summary>
public interface ICurtainController
{
    Task OpenCurtainAsync(string deviceId);
    Task CloseCurtainAsync(string deviceId);
    Task SetCurtainPositionAsync(string deviceId, int openPercentage);
}
