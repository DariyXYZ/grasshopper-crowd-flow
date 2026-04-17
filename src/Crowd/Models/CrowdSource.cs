using Rhino.Geometry;

namespace Crowd.Models;

public sealed class CrowdSource
{
    public CrowdSource(Point3d location, int totalAgents, double spawnRate, int? exitIndex, double startTime)
    {
        Location = location;
        TotalAgents = totalAgents;
        SpawnRate = spawnRate;
        ExitIndex = exitIndex;
        StartTime = startTime;
    }

    public Point3d Location { get; }

    public int TotalAgents { get; }

    public double SpawnRate { get; }

    public int? ExitIndex { get; }

    public double StartTime { get; }
}
