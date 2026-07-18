using Atlas.Vision.Interfaces;
using Atlas.Vision.Models;

namespace Atlas.Vision.Providers;

/// <summary>
/// A simulated vision provider for testing. Generates fake face detection and motion data.
/// Replace with WebcamVisionProvider when a physical camera is available.
/// </summary>
public class SimulationVisionProvider : IVisionProvider
{
    private readonly Random _rng = new();
    private readonly List<SecurityAlert> _alerts = new();

    public string Name => "Simulation";

    public Task InitializeAsync() => Task.CompletedTask;

    public Task<List<string>> GetAvailableCamerasAsync()
        => Task.FromResult(new List<string> { "front-door-cam", "backyard-cam", "living-room-cam" });

    public Task<CameraFrame> CaptureFrameAsync(string cameraId)
    {
        return Task.FromResult(new CameraFrame
        {
            CameraId = cameraId,
            Width = 1280,
            Height = 720,
            CapturedAt = DateTime.UtcNow,
            ImageData = new byte[100] // placeholder
        });
    }

    public Task<List<DetectedFace>> DetectFacesAsync(CameraFrame frame)
    {
        // Randomly simulate 0-2 faces detected
        int count = _rng.Next(0, 3);
        var faces = new List<DetectedFace>();

        for (int i = 0; i < count; i++)
        {
            faces.Add(new DetectedFace
            {
                X = _rng.Next(100, 400),
                Y = _rng.Next(100, 300),
                Width = 80,
                Height = 100,
                Confidence = Math.Round(_rng.NextDouble() * 0.4 + 0.6, 2),
                PersonName = null // Unknown person (no trained model yet)
            });
        }

        return Task.FromResult(faces);
    }

    public Task<MotionEvent?> DetectMotionAsync(string cameraId)
    {
        // 30% chance of motion
        if (_rng.Next(0, 10) < 3)
        {
            return Task.FromResult<MotionEvent?>(new MotionEvent
            {
                CameraId = cameraId,
                DetectedAt = DateTime.UtcNow,
                ChangePercentage = Math.Round(_rng.NextDouble() * 30 + 5, 2)
            });
        }

        return Task.FromResult<MotionEvent?>(null);
    }

    public Task<List<SecurityAlert>> GetActiveAlertsAsync()
        => Task.FromResult(_alerts.Where(a => !a.IsAcknowledged).ToList());
}
