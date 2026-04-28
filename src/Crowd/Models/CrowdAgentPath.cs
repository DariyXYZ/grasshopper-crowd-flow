using Rhino.Geometry;

namespace Crowd.Models;

/// <summary>Records the full trajectory polyline and outcome for a single agent across the simulation.</summary>
public sealed class CrowdAgentPath
{
    /// <summary>Initializes a new <see cref="CrowdAgentPath"/> with agent identity, target exit, recorded polyline, and timing data.</summary>
    public CrowdAgentPath(int agentId, int exitIndex, Polyline polyline, bool reachedExit, double spawnTime, double? finishTime)
    {
        AgentId = agentId;
        ExitIndex = exitIndex;
        Polyline = polyline ?? throw new ArgumentNullException(nameof(polyline));
        ReachedExit = reachedExit;
        SpawnTime = spawnTime;
        FinishTime = finishTime;
    }

    /// <summary>Unique identifier of the agent this path belongs to.</summary>
    public int AgentId { get; }

    /// <summary>Index of the exit this agent was targeting when it finished or the simulation ended.</summary>
    public int ExitIndex { get; }

    /// <summary>Sequence of world positions recording the agent's movement over time.</summary>
    public Polyline Polyline { get; }

    /// <summary>True when the agent successfully reached its target exit.</summary>
    public bool ReachedExit { get; }

    /// <summary>Simulation time at which this agent was spawned.</summary>
    public double SpawnTime { get; }

    /// <summary>Simulation time at which this agent exited; null if the agent did not finish.</summary>
    public double? FinishTime { get; }
}
