namespace Crowd.Models;

/// <summary>Reports how many agents used a specific exit and their proportional share of total completions.</summary>
public sealed class CrowdExitSplitMetric
{
    /// <summary>Initializes a new <see cref="CrowdExitSplitMetric"/> with exit index, agent count, and utilisation share.</summary>
    public CrowdExitSplitMetric(int exitIndex, int completedAgents, double share)
    {
        ExitIndex = exitIndex;
        CompletedAgents = completedAgents;
        Share = share;
    }

    /// <summary>Zero-based index of the exit this metric describes.</summary>
    public int ExitIndex { get; }

    /// <summary>Number of agents that completed the simulation via this exit.</summary>
    public int CompletedAgents { get; }

    /// <summary>Fraction of total completing agents that used this exit, in the range [0, 1].</summary>
    public double Share { get; }
}
