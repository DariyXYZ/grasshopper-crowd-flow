namespace Crowd.Models;

public sealed class CrowdAgentProfile
{
    public CrowdAgentProfile(
        double radius,
        double preferredSpeed,
        double maxSpeed,
        double separationWeight,
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
        double turnAnticipation)
    {
        Radius = radius;
        PreferredSpeed = preferredSpeed;
        MaxSpeed = maxSpeed;
        SeparationWeight = separationWeight;
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
        TurnAnticipation = turnAnticipation;
    }

    public double Radius { get; }

    public double PreferredSpeed { get; }

    public double MaxSpeed { get; }

    public double SeparationWeight { get; }

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

    public double TurnAnticipation { get; }

    public static CrowdAgentProfile Default { get; } = new(
        radius: 0.35,
        preferredSpeed: 1.35,
        maxSpeed: 1.8,
        separationWeight: 1.4,
        arrivalThreshold: 0.5,
        variationPercent: 0.14,
        steeringNoise: 0.12,
        densityWeight: 1.05,
        spawnJitter: 0.95,
        exitChoiceRandomness: 0.28,
        congestionSensitivity: 1.2,
        exitCommitment: 0.64,
        reassessmentInterval: 1.35,
        wallAvoidance: 1.25,
        turnAnticipation: 1.45);
}
