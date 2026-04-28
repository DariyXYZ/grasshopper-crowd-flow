using Rhino.Geometry;

namespace Crowd.Models;

/// <summary>Snapshot of all active agent positions and speeds at a single point in simulation time.</summary>
public sealed class CrowdFrame
{
    /// <summary>Initializes a new <see cref="CrowdFrame"/> with the simulation time, positions, speeds, and agent counts.</summary>
    public CrowdFrame(
        double time,
        IReadOnlyList<Point3d> activePositions,
        IReadOnlyList<double> activeSpeeds,
        int activeCount,
        int finishedCount)
    {
        Time = time;
        ActivePositions = activePositions ?? throw new ArgumentNullException(nameof(activePositions));
        ActiveSpeeds = activeSpeeds ?? throw new ArgumentNullException(nameof(activeSpeeds));
        ActiveCount = activeCount;
        FinishedCount = finishedCount;
    }

    /// <summary>Simulation time in seconds at which this frame was recorded.</summary>
    public double Time { get; }

    /// <summary>World positions of all agents that are still active at this frame.</summary>
    public IReadOnlyList<Point3d> ActivePositions { get; }

    /// <summary>Scalar speeds corresponding to each active agent position.</summary>
    public IReadOnlyList<double> ActiveSpeeds { get; }

    /// <summary>Number of agents still moving at this frame.</summary>
    public int ActiveCount { get; }

    /// <summary>Cumulative number of agents that have reached an exit by this frame.</summary>
    public int FinishedCount { get; }
}
