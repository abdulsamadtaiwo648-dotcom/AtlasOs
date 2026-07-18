using Atlas.Vision.Interfaces;

namespace Atlas.Vision.Services;

/// <summary>
/// The brain of the Atlas Vision module.
/// Receives natural language commands and dispatches the right vision operations
/// through IVisionProvider.
/// </summary>
public class VisionEngine
{
    private readonly IVisionProvider _provider;

    public VisionEngine(IVisionProvider provider)
    {
        _provider = provider;
    }

    public async Task<string> ProcessCommandAsync(string input)
    {
        string text = input.ToLowerInvariant();

        // ── List available cameras ─────────────────────────
        if (text.Contains("camera") && (text.Contains("list") || text.Contains("show") || text.Contains("available")))
        {
            var cameras = await _provider.GetAvailableCamerasAsync();
            return "Available cameras:\n" + string.Join("\n", cameras.Select(c => $"  • {c}"));
        }

        // ── Motion detection ───────────────────────────────
        if (text.Contains("motion") || text.Contains("movement"))
        {
            string camId = ExtractCameraId(text) ?? "front-door-cam";
            var motion = await _provider.DetectMotionAsync(camId);

            if (motion == null)
                return $"No motion detected on {camId}.";

            string level = motion.IsSignificant ? "Significant motion" : "Minor movement";
            return $"{level} detected on {camId} at {motion.DetectedAt:HH:mm:ss} UTC. Change: {motion.ChangePercentage}%.";
        }

        // ── Face detection / who is at the door ───────────
        if (text.Contains("face") || text.Contains("who is") || text.Contains("detect") || text.Contains("door"))
        {
            string camId = ExtractCameraId(text) ?? "front-door-cam";
            var frame = await _provider.CaptureFrameAsync(camId);
            var faces = await _provider.DetectFacesAsync(frame);

            if (!faces.Any())
                return $"No faces detected on {camId}.";

            if (faces.Count == 1)
            {
                string name = faces[0].PersonName ?? "an unrecognized person";
                return $"I can see {name} at the {camId}. Confidence: {faces[0].Confidence:P0}.";
            }

            return $"I can see {faces.Count} people on {camId}. None have been identified yet.";
        }

        // ── Security alerts ────────────────────────────────
        if (text.Contains("alert") || text.Contains("security") || text.Contains("threat"))
        {
            var alerts = await _provider.GetActiveAlertsAsync();
            if (!alerts.Any())
                return "No active security alerts.";

            return $"{alerts.Count} active alert(s):\n" +
                   string.Join("\n", alerts.Select(a => $"  [{a.Level}] {a.Message}"));
        }

        return "I didn't understand that vision command.";
    }

    private static string? ExtractCameraId(string text)
    {
        if (text.Contains("front door") || text.Contains("front-door")) return "front-door-cam";
        if (text.Contains("backyard") || text.Contains("back yard")) return "backyard-cam";
        if (text.Contains("living room")) return "living-room-cam";
        return null;
    }
}
