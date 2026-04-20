using Rhino.Geometry;

namespace Crowd.Models;

/// <summary>
/// Stores the result of a Rhino viewport capture used for report imagery.
/// </summary>
public sealed class CrowdViewportCaptureResult
{
    public CrowdViewportCaptureResult(bool saved, string filePath, string status, BoundingBox usedBounds)
    {
        Saved = saved;
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        UsedBounds = usedBounds;
    }

    public bool Saved { get; }

    public string FilePath { get; }

    public string Status { get; }

    public BoundingBox UsedBounds { get; }
}
