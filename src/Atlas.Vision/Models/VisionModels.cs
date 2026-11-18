namespace Atlas.Vision.Models;

public class CameraFrame
{
    public string CameraId { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime CapturedAt { get; set; }
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
}

public class DetectedFace
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double Confidence { get; set; }
    public string? PersonName { get; set; }
}

public class MotionEvent
{
    public string CameraId { get; set; } = "";
    public DateTime DetectedAt { get; set; }
    public double ChangePercentage { get; set; }
    public bool IsSignificant => ChangePercentage >= 15;
}

public class SecurityAlert
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CameraId { get; set; } = "";
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }
}
