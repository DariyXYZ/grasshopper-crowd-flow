namespace Crowd.Models;

public sealed class CrowdExitSplitMetric
{
    public CrowdExitSplitMetric(int exitIndex, int completedAgents, double share)
    {
        ExitIndex = exitIndex;
        CompletedAgents = completedAgents;
        Share = share;
    }

    public int ExitIndex { get; }

    public int CompletedAgents { get; }

    public double Share { get; }
}
