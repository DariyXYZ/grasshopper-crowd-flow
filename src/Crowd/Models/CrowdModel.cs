namespace Crowd.Models;

/// <summary>Aggregates all scene inputs required to run a crowd simulation.</summary>
public sealed class CrowdModel
{
    /// <summary>Initializes a new <see cref="CrowdModel"/> with floor, obstacles, sources, exits, agent profile, and time step.</summary>
    public CrowdModel(
        CrowdFloor floor,
        IReadOnlyList<CrowdObstacle> obstacles,
        IReadOnlyList<CrowdSource> sources,
        IReadOnlyList<CrowdExit> exits,
        CrowdAgentProfile agentProfile,
        double timeStep)
    {
        Floor = floor ?? throw new ArgumentNullException(nameof(floor));
        Obstacles = obstacles ?? throw new ArgumentNullException(nameof(obstacles));
        Sources = sources ?? throw new ArgumentNullException(nameof(sources));
        Exits = exits ?? throw new ArgumentNullException(nameof(exits));
        AgentProfile = agentProfile ?? throw new ArgumentNullException(nameof(agentProfile));
        TimeStep = timeStep;
    }

    /// <summary>Walkable floor geometry used to build the navigation grid.</summary>
    public CrowdFloor Floor { get; }

    /// <summary>List of obstacle regions that block agent movement.</summary>
    public IReadOnlyList<CrowdObstacle> Obstacles { get; }

    /// <summary>List of agent spawn sources.</summary>
    public IReadOnlyList<CrowdSource> Sources { get; }

    /// <summary>List of destination exits agents navigate toward.</summary>
    public IReadOnlyList<CrowdExit> Exits { get; }

    /// <summary>Behavioral profile applied to all agents in the simulation.</summary>
    public CrowdAgentProfile AgentProfile { get; }

    /// <summary>Duration of each simulation step in seconds.</summary>
    public double TimeStep { get; }
}
