using Rhino.Geometry;

namespace Crowd.Models;

public sealed class CrowdHeatmapResult
{
    public CrowdHeatmapResult(
        Mesh mesh,
        IReadOnlyList<double> values,
        BoundingBox bounds,
        double cellSize,
        double heightScale,
        double minimumValue,
        double peakValue,
        string mode,
        string legendTitle)
    {
        Mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        Values = values ?? throw new ArgumentNullException(nameof(values));
        Bounds = bounds;
        CellSize = cellSize;
        HeightScale = heightScale;
        MinimumValue = minimumValue;
        PeakValue = peakValue;
        Mode = mode ?? throw new ArgumentNullException(nameof(mode));
        LegendTitle = legendTitle ?? throw new ArgumentNullException(nameof(legendTitle));
    }

    public Mesh Mesh { get; }

    public IReadOnlyList<double> Values { get; }

    public BoundingBox Bounds { get; }

    public double CellSize { get; }

    public double HeightScale { get; }

    public double MinimumValue { get; }

    public double PeakValue { get; }

    public string Mode { get; }

    public string LegendTitle { get; }
}
