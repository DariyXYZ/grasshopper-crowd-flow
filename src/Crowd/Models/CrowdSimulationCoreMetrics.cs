namespace Crowd.Models;

/// <summary>Aggregated performance statistics computed at the end of a crowd simulation.</summary>
public sealed class CrowdSimulationCoreMetrics
{
    /// <summary>Initializes a new <see cref="CrowdSimulationCoreMetrics"/> with completion count, travel-time statistics, clearance time, and per-exit splits.</summary>
    public CrowdSimulationCoreMetrics(
        int completedAgentCount,
        double? meanTravelTime,
        double? minimumTravelTime,
        double? maximumTravelTime,
        double clearanceTime,
        IReadOnlyList<CrowdExitSplitMetric> exitSplits)
    {
        CompletedAgentCount = completedAgentCount;
        MeanTravelTime = meanTravelTime;
        MinimumTravelTime = minimumTravelTime;
        MaximumTravelTime = maximumTravelTime;
        ClearanceTime = clearanceTime;
        ExitSplits = exitSplits ?? throw new ArgumentNullException(nameof(exitSplits));
    }

    /// <summary>Number of agents that successfully reached an exit.</summary>
    public int CompletedAgentCount { get; }

    /// <summary>Average travel time across all completing agents; null when no agents completed.</summary>
    public double? MeanTravelTime { get; }

    /// <summary>Shortest individual travel time recorded; null when no agents completed.</summary>
    public double? MinimumTravelTime { get; }

    /// <summary>Longest individual travel time recorded; null when no agents completed.</summary>
    public double? MaximumTravelTime { get; }

    /// <summary>Simulation time at which the last agent exited the scene.</summary>
    public double ClearanceTime { get; }

    /// <summary>Per-exit breakdown of agent counts and utilisation share.</summary>
    public IReadOnlyList<CrowdExitSplitMetric> ExitSplits { get; }
}
