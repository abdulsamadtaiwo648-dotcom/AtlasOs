using Atlas.SmartHome.Models;

namespace Atlas.SmartHome.Interfaces;

/// <summary>
/// Controls camera devices.
/// </summary>
public interface ICameraController
{
    Task<string> GetCameraStreamUrlAsync(string deviceId);
    Task TakeSnapshotAsync(string deviceId, string savePath);
}
