namespace Crowd.Models;

public sealed class CrowdSimulationCoreMetrics
{
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

    public int CompletedAgentCount { get; }

    public double? MeanTravelTime { get; }

    public double? MinimumTravelTime { get; }

    public double? MaximumTravelTime { get; }

    public double ClearanceTime { get; }

    public IReadOnlyList<CrowdExitSplitMetric> ExitSplits { get; }
}
