using Rhino.Geometry;

namespace Crowd.Models;

public sealed class CrowdFloor
{
    public CrowdFloor(Curve boundary, Plane plane, double cellSize)
    {
        Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
        Plane = plane;
        CellSize = cellSize;
    }

    public Curve Boundary { get; }

    public Plane Plane { get; }

    public double CellSize { get; }
}
