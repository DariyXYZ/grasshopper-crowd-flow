using Rhino.Geometry;

namespace Crowd.Models;

public sealed class CrowdHeatmapResult
{
    public CrowdHeatmapResult(
        Mesh mesh,
        IReadOnlyList<Point3d> cellCenters,
        IReadOnlyList<double> values,
        double peakValue,
        double averageValue,
        string mode)
    {
        Mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        CellCenters = cellCenters ?? throw new ArgumentNullException(nameof(cellCenters));
        Values = values ?? throw new ArgumentNullException(nameof(values));
        PeakValue = peakValue;
        AverageValue = averageValue;
        Mode = mode ?? throw new ArgumentNullException(nameof(mode));
    }

    public Mesh Mesh { get; }

    public IReadOnlyList<Point3d> CellCenters { get; }

    public IReadOnlyList<double> Values { get; }

    public double PeakValue { get; }

    public double AverageValue { get; }

    public string Mode { get; }
}
