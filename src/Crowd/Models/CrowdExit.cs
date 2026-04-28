using Rhino.Geometry;

namespace Crowd.Models;

/// <summary>Represents a circular exit zone that agents navigate toward and are removed upon entering.</summary>
public sealed class CrowdExit
{
    /// <summary>Initializes a new <see cref="CrowdExit"/> with a world position and capture radius.</summary>
    public CrowdExit(Point3d location, double radius)
    {
        Location = location;
        Radius = radius;
    }

    /// <summary>World position of the exit center.</summary>
    public Point3d Location { get; }

    /// <summary>Capture radius within which arriving agents are considered to have exited.</summary>
    public double Radius { get; }
}
