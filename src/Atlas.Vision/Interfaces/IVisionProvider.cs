using Atlas.Vision.Models;

namespace Atlas.Vision.Interfaces;

/// <summary>
/// The primary interface Atlas uses to talk to any vision system.
/// Implementations include WebcamVisionProvider and SimulationVisionProvider.
/// </summary>
public interface IVisionProvider
{
    string Name { get; }
    Task InitializeAsync();
    Task<CameraFrame> CaptureFrameAsync(string cameraId);
    Task<List<DetectedFace>> DetectFacesAsync(CameraFrame frame);
    Task<MotionEvent?> DetectMotionAsync(string cameraId);
    Task<List<SecurityAlert>> GetActiveAlertsAsync();
    Task<List<string>> GetAvailableCamerasAsync();
}
