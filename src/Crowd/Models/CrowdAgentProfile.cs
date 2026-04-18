namespace Crowd.Models;

public sealed class CrowdAgentProfile
{
    public CrowdAgentProfile(
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
        double variationPercent,
        double steeringNoise,
        double densityWeight,
        double spawnJitter,
        double exitChoiceRandomness,
        double congestionSensitivity,
        double exitCommitment,
        double reassessmentInterval,
        double wallAvoidance,
        double wallBufferDistance,
        double turnAnticipation,
        double preferredSideBias)
    {
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
        VariationPercent = variationPercent;
        SteeringNoise = steeringNoise;
        DensityWeight = densityWeight;
        SpawnJitter = spawnJitter;
        ExitChoiceRandomness = exitChoiceRandomness;
        CongestionSensitivity = congestionSensitivity;
        ExitCommitment = exitCommitment;
        ReassessmentInterval = reassessmentInterval;
        WallAvoidance = wallAvoidance;
        WallBufferDistance = wallBufferDistance;
        TurnAnticipation = turnAnticipation;
        PreferredSideBias = preferredSideBias;
    }

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

    public double VariationPercent { get; }

    public double SteeringNoise { get; }

    public double DensityWeight { get; }

    public double SpawnJitter { get; }

    public double ExitChoiceRandomness { get; }

    public double CongestionSensitivity { get; }

    public double ExitCommitment { get; }

    public double ReassessmentInterval { get; }

    public double WallAvoidance { get; }

    public double WallBufferDistance { get; }

    public double TurnAnticipation { get; }

    public double PreferredSideBias { get; }

    public static CrowdAgentProfile Default { get; } = new(
        radius: 0.35,
        preferredSpeed: 1.35,
        maxSpeed: 1.8,
        timeGap: 1.15,
        reactionTime: 0.42,
        anticipationTime: 1.0,
        separationWeight: 1.35,
        neighborRepulsionStrength: 1.05,
        neighborRepulsionRange: 1.2,
        comfortDistance: 0.55,
        arrivalThreshold: 0.5,
        variationPercent: 0.16,
        steeringNoise: 0.08,
        densityWeight: 0.95,
        spawnJitter: 0.8,
        exitChoiceRandomness: 0.24,
        congestionSensitivity: 1.1,
        exitCommitment: 0.66,
        reassessmentInterval: 1.35,
        wallAvoidance: 1.1,
        wallBufferDistance: 0.22,
        turnAnticipation: 1.45,
        preferredSideBias: 0.16);
}
