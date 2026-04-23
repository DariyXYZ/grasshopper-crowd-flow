namespace Crowd.Models;

public sealed class CrowdSimulationProfile
{
    private const string EngineBuild = "2026-04-23.6";

    public CrowdSimulationProfile(
        double gridBuildMilliseconds,
        double pathFieldBuildMilliseconds,
        double simulationLoopMilliseconds,
        double resultBuildMilliseconds,
        double totalMilliseconds,
        int gridWidth,
        int gridHeight,
        int exitCount,
        int frameCount,
        int spawnedAgentCount,
        int completedAgentCount,
        int activeAgentCount,
        string terminationReason,
        string activeTailSummary,
        double lastCompletionAge,
        double maximumSimulationDuration,
        double simulatedDuration)
    {
        GridBuildMilliseconds = gridBuildMilliseconds;
        PathFieldBuildMilliseconds = pathFieldBuildMilliseconds;
        SimulationLoopMilliseconds = simulationLoopMilliseconds;
        ResultBuildMilliseconds = resultBuildMilliseconds;
        TotalMilliseconds = totalMilliseconds;
        GridWidth = gridWidth;
        GridHeight = gridHeight;
        ExitCount = exitCount;
        FrameCount = frameCount;
        SpawnedAgentCount = spawnedAgentCount;
        CompletedAgentCount = completedAgentCount;
        ActiveAgentCount = activeAgentCount;
        TerminationReason = terminationReason ?? string.Empty;
        ActiveTailSummary = activeTailSummary ?? string.Empty;
        LastCompletionAge = lastCompletionAge;
        MaximumSimulationDuration = maximumSimulationDuration;
        SimulatedDuration = simulatedDuration;
    }

    public double GridBuildMilliseconds { get; }

    public double PathFieldBuildMilliseconds { get; }

    public double SimulationLoopMilliseconds { get; }

    public double ResultBuildMilliseconds { get; }

    public double TotalMilliseconds { get; }

    public int GridWidth { get; }

    public int GridHeight { get; }

    public int ExitCount { get; }

    public int FrameCount { get; }

    public int SpawnedAgentCount { get; }

    public int CompletedAgentCount { get; }

    public int ActiveAgentCount { get; }

    public string TerminationReason { get; }

    public string ActiveTailSummary { get; }

    public double LastCompletionAge { get; }

    public double MaximumSimulationDuration { get; }

    public double SimulatedDuration { get; }

    public override string ToString()
    {
        return string.Join(
            Environment.NewLine,
            $"Engine build: {EngineBuild}",
            $"Total: {TotalMilliseconds:0.0} ms",
            $"Grid: {GridBuildMilliseconds:0.0} ms ({GridWidth} x {GridHeight})",
            $"Path fields: {PathFieldBuildMilliseconds:0.0} ms ({ExitCount} exits)",
            $"Simulation: {SimulationLoopMilliseconds:0.0} ms ({FrameCount} frames, {SpawnedAgentCount} spawned, {CompletedAgentCount} completed, {ActiveAgentCount} active)",
            $"Result metrics: {ResultBuildMilliseconds:0.0} ms",
            $"Simulated duration: {SimulatedDuration:0.###} s / max {MaximumSimulationDuration:0.###} s",
            $"Last completion age: {LastCompletionAge:0.###} s",
            $"Active tail: {ActiveTailSummary}",
            $"Stop reason: {TerminationReason}");
    }
}
