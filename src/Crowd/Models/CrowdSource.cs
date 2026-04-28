using Rhino.Geometry;

namespace Crowd.Models;

/// <summary>Defines a point in space where agents are spawned into the simulation.</summary>
public sealed class CrowdSource
{
    /// <summary>Initializes a new <see cref="CrowdSource"/> with spawn location, agent count, rate, optional exit assignment, and start time.</summary>
    public CrowdSource(Point3d location, int totalAgents, double spawnRate, int? exitIndex, double startTime)
    {
        Location = location;
        TotalAgents = totalAgents;
        SpawnRate = spawnRate;
        ExitIndex = exitIndex;
        StartTime = startTime;
    }

    /// <summary>World position at which new agents are spawned.</summary>
    public Point3d Location { get; }

    /// <summary>Total number of agents this source will spawn over the simulation.</summary>
    public int TotalAgents { get; }

    /// <summary>Number of agents spawned per second.</summary>
    public double SpawnRate { get; }

    /// <summary>Optional fixed exit index assigned to every agent from this source; null means free choice.</summary>
    public int? ExitIndex { get; }

    /// <summary>Simulation time at which this source begins spawning agents.</summary>
    public double StartTime { get; }
}
