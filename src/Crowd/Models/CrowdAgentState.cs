using Rhino.Geometry;

namespace Crowd.Models;

public sealed class CrowdAgentState
{
    public CrowdAgentState(
        int id,
        Point3d position,
        int exitIndex,
        bool hasFixedExit,
        double spawnTime,
        double radius,
        double preferredSpeed,
        double maxSpeed,
        double timeGap,
        double reactionTime,
        double anticipationTime,
        double separationWeight,
        double neighborRepulsionStrength,
        double neighborRepulsionRange,
        double comfortDistance,
        double arrivalThreshold,
        double noiseOffset,
        double sideBias,
        double routeCommitment,
        double exitChoiceRandomness,
        double congestionSensitivity,
        double exitCommitment,
        double reassessmentInterval,
        double wallBufferDistance,
        double wanderStrength,
        double curvaturePreference,
        double startScatterStrength,
        double focusDelay,
        double initialFieldDistance,
        double laneCommitmentBias)
    {
        Id = id;
        Position = position;
        ExitIndex = exitIndex;
        HasFixedExit = hasFixedExit;
        SpawnTime = spawnTime;
        Radius = radius;
        PreferredSpeed = preferredSpeed;
        MaxSpeed = maxSpeed;
        TimeGap = timeGap;
        ReactionTime = reactionTime;
        AnticipationTime = anticipationTime;
        SeparationWeight = separationWeight;
        NeighborRepulsionStrength = neighborRepulsionStrength;
        NeighborRepulsionRange = neighborRepulsionRange;
        ComfortDistance = comfortDistance;
        ArrivalThreshold = arrivalThreshold;
        NoiseOffset = noiseOffset;
        SideBias = sideBias;
        RouteCommitment = routeCommitment;
        ExitChoiceRandomness = exitChoiceRandomness;
        CongestionSensitivity = congestionSensitivity;
        ExitCommitment = exitCommitment;
        ReassessmentInterval = reassessmentInterval;
        WallBufferDistance = wallBufferDistance;
        WanderStrength = wanderStrength;
        CurvaturePreference = curvaturePreference;
        StartScatterStrength = startScatterStrength;
        FocusDelay = focusDelay;
        LastFieldDistance = initialFieldDistance;
        LaneCommitmentBias = laneCommitmentBias;
        NextExitDecisionTime = spawnTime + reassessmentInterval;
        Velocity = Vector3d.Zero;
        DesiredVelocity = Vector3d.Zero;
    }

    public int Id { get; }

    public Point3d Position { get; set; }

    public Vector3d Velocity { get; set; }

    public Vector3d DesiredVelocity { get; set; }

    public int ExitIndex { get; set; }

    public bool HasFixedExit { get; }

    public double SpawnTime { get; }

    public double Radius { get; }

    public double PreferredSpeed { get; }

    public double MaxSpeed { get; }

    public double TimeGap { get; }

    public double ReactionTime { get; }

    public double AnticipationTime { get; }

    public double SeparationWeight { get; }

    public double NeighborRepulsionStrength { get; }

    public double NeighborRepulsionRange { get; }

    public double ComfortDistance { get; }

    public double ArrivalThreshold { get; }

    public double NoiseOffset { get; }

    public double SideBias { get; }

    public double RouteCommitment { get; }

    public double ExitChoiceRandomness { get; }

    public double CongestionSensitivity { get; }

    public double ExitCommitment { get; }

    public double ReassessmentInterval { get; }

    public double WallBufferDistance { get; }

    public double WanderStrength { get; }

    public double CurvaturePreference { get; }

    public double StartScatterStrength { get; }

    public double FocusDelay { get; }

    public double LastFieldDistance { get; set; }

    public double StuckDuration { get; set; }

    public double LaneCommitmentBias { get; }

    public double NextExitDecisionTime { get; set; }

    public double? FinishTime { get; set; }

    public bool IsFinished => FinishTime.HasValue;
}
