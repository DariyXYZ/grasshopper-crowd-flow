using Rhino.Geometry;

namespace Crowd.Models;

public sealed class CrowdFrame
{
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

    public double Time { get; }

    public IReadOnlyList<Point3d> ActivePositions { get; }

    public IReadOnlyList<double> ActiveSpeeds { get; }

    public int ActiveCount { get; }

    public int FinishedCount { get; }
}
