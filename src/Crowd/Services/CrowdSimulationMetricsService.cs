using Crowd.Models;

namespace Crowd.Services;

public static class CrowdSimulationMetricsService
{
    public static CrowdSimulationCoreMetrics BuildCoreMetrics(CrowdSimulationResult result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return BuildCoreMetrics(result.Model, result.AgentPaths, result.SimulatedDuration);
    }

    public static CrowdSimulationCoreMetrics BuildCoreMetrics(
        CrowdModel model,
        IReadOnlyList<CrowdAgentPath> agentPaths,
        double clearanceTime)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (agentPaths == null)
        {
            throw new ArgumentNullException(nameof(agentPaths));
        }

        List<double> completedTravelTimes = agentPaths
            .Where(path => path.ReachedExit && path.FinishTime.HasValue)
            .Select(path => Math.Max(0.0, path.FinishTime!.Value - path.SpawnTime))
            .ToList();

        double? meanTravelTime = completedTravelTimes.Count == 0 ? null : completedTravelTimes.Average();
        double? minimumTravelTime = completedTravelTimes.Count == 0 ? null : completedTravelTimes.Min();
        double? maximumTravelTime = completedTravelTimes.Count == 0 ? null : completedTravelTimes.Max();

        int completedCount = Math.Max(1, completedTravelTimes.Count);
        List<CrowdExitSplitMetric> exitSplits = model.Exits
            .Select((_, index) =>
            {
                int count = agentPaths.Count(path => path.ReachedExit && path.ExitIndex == index);
                double share = completedTravelTimes.Count == 0 ? 0.0 : (double)count / completedCount;
                return new CrowdExitSplitMetric(index, count, share);
            })
            .ToList();

        return new CrowdSimulationCoreMetrics(
            completedTravelTimes.Count,
            meanTravelTime,
            minimumTravelTime,
            maximumTravelTime,
            clearanceTime,
            exitSplits);
    }
}
