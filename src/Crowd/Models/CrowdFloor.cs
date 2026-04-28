using Rhino.Geometry;

namespace Crowd.Models;

/// <summary>Represents the walkable floor area defined by a boundary curve, orientation plane, and grid resolution.</summary>
public sealed class CrowdFloor
{
    /// <summary>Initializes a new <see cref="CrowdFloor"/> with a boundary curve, world plane, and cell size.</summary>
    public CrowdFloor(Curve boundary, Plane plane, double cellSize)
    {
        Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
        Plane = plane;
        CellSize = cellSize;
    }

    /// <summary>Closed curve that defines the outer walkable boundary of the floor.</summary>
    public Curve Boundary { get; }

    /// <summary>Orientation plane of the floor used for coordinate-space calculations.</summary>
    public Plane Plane { get; }

    /// <summary>Grid cell size used when rasterising the floor into a walkability map.</summary>
    public double CellSize { get; }
}
