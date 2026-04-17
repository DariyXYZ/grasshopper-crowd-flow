using Rhino.Geometry;

namespace Crowd.Models;

public sealed class CrowdExit
{
    public CrowdExit(Point3d location, double radius)
    {
        Location = location;
        Radius = radius;
    }

    public Point3d Location { get; }

    public double Radius { get; }
}
