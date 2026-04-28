using Rhino.Geometry;

namespace Crowd.Models;

/// <summary>Represents an impassable obstacle region defined by a closed boundary curve.</summary>
public sealed class CrowdObstacle
{
    /// <summary>Initializes a new <see cref="CrowdObstacle"/> with the given boundary curve.</summary>
    public CrowdObstacle(Curve boundary)
    {
        Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
    }

    /// <summary>Closed curve that defines the obstacle's impassable area.</summary>
    public Curve Boundary { get; }
}
