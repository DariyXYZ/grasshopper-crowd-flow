using Rhino.Geometry;

namespace Crowd.Models;

public sealed class CrowdObstacle
{
    public CrowdObstacle(Curve boundary)
    {
        Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
    }

    public Curve Boundary { get; }
}
