using Rhino.Geometry;

namespace Crowd.Models;

public sealed class CrowdHeatmapLegendResult
{
    public CrowdHeatmapLegendResult(
        Mesh mesh,
        IReadOnlyList<GeometryBase> labelGeometry,
        double minimumValue,
        double maximumValue)
    {
        Mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        LabelGeometry = labelGeometry ?? throw new ArgumentNullException(nameof(labelGeometry));
        MinimumValue = minimumValue;
        MaximumValue = maximumValue;
    }

    public Mesh Mesh { get; }

    public IReadOnlyList<GeometryBase> LabelGeometry { get; }

    public double MinimumValue { get; }

    public double MaximumValue { get; }
}
