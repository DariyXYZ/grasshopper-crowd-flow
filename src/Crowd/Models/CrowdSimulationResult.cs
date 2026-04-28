namespace Crowd.Models;

/// <summary>Holds all outputs produced by a completed crowd simulation run.</summary>
public sealed class CrowdSimulationResult
{
    /// <summary>Initializes a new <see cref="CrowdSimulationResult"/> with model, frames, paths, metrics, profile, and completion data.</summary>
    public CrowdSimulationResult(
        CrowdModel model,
        IReadOnlyList<CrowdFrame> frames,
        IReadOnlyList<CrowdAgentPath> agentPaths,
        CrowdSimulationCoreMetrics coreMetrics,
        CrowdSimulationProfile profile,
        int totalSpawned,
        int totalFinished,
        double simulatedDuration,
        bool completedAllAgents)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Frames = frames ?? throw new ArgumentNullException(nameof(frames));
        AgentPaths = agentPaths ?? throw new ArgumentNullException(nameof(agentPaths));
        CoreMetrics = coreMetrics ?? throw new ArgumentNullException(nameof(coreMetrics));
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        TotalSpawned = totalSpawned;
        TotalFinished = totalFinished;
        SimulatedDuration = simulatedDuration;
        CompletedAllAgents = completedAllAgents;
    }

    /// <summary>The input model that was used to run this simulation.</summary>
    public CrowdModel Model { get; }

    /// <summary>Ordered list of per-step frames capturing agent positions over time.</summary>
    public IReadOnlyList<CrowdFrame> Frames { get; }

    /// <summary>Full trajectory path recorded for each agent.</summary>
    public IReadOnlyList<CrowdAgentPath> AgentPaths { get; }

    /// <summary>Aggregated performance metrics computed from the simulation run.</summary>
    public CrowdSimulationCoreMetrics CoreMetrics { get; }

    /// <summary>Simulation configuration profile used during this run.</summary>
    public CrowdSimulationProfile Profile { get; }

    /// <summary>Total number of agents that were spawned during the simulation.</summary>
    public int TotalSpawned { get; }

    /// <summary>Total number of agents that successfully reached an exit.</summary>
    public int TotalFinished { get; }

    /// <summary>Wall-clock simulated time in seconds from start to last event.</summary>
    public double SimulatedDuration { get; }

    /// <summary>True when every spawned agent reached an exit before the simulation ended.</summary>
    public bool CompletedAllAgents { get; }
}
