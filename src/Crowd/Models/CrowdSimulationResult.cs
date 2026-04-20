namespace Crowd.Models;

public sealed class CrowdSimulationResult
{
    public CrowdSimulationResult(
        CrowdModel model,
        IReadOnlyList<CrowdFrame> frames,
        IReadOnlyList<CrowdAgentPath> agentPaths,
        CrowdSimulationCoreMetrics coreMetrics,
        int totalSpawned,
        int totalFinished,
        double simulatedDuration,
        bool completedAllAgents)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Frames = frames ?? throw new ArgumentNullException(nameof(frames));
        AgentPaths = agentPaths ?? throw new ArgumentNullException(nameof(agentPaths));
        CoreMetrics = coreMetrics ?? throw new ArgumentNullException(nameof(coreMetrics));
        TotalSpawned = totalSpawned;
        TotalFinished = totalFinished;
        SimulatedDuration = simulatedDuration;
        CompletedAllAgents = completedAllAgents;
    }

    public CrowdModel Model { get; }

    public IReadOnlyList<CrowdFrame> Frames { get; }

    public IReadOnlyList<CrowdAgentPath> AgentPaths { get; }

    public CrowdSimulationCoreMetrics CoreMetrics { get; }

    public int TotalSpawned { get; }

    public int TotalFinished { get; }

    public double SimulatedDuration { get; }

    public bool CompletedAllAgents { get; }
}
