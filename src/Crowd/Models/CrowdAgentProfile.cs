namespace Crowd.Models;

/// <summary>Defines the behavioral and physical parameters for a crowd agent archetype.</summary>
public sealed class CrowdAgentProfile
{
    /// <summary>Initializes a new instance of <see cref="CrowdAgentProfile"/> with all locomotion and decision parameters.</summary>
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

    /// <summary>Physical radius of the agent used for collision and spacing calculations.</summary>
    public double Radius { get; }

    /// <summary>Target walking speed the agent tries to maintain under uncongested conditions.</summary>
    public double PreferredSpeed { get; }

    /// <summary>Upper bound on the agent's movement speed.</summary>
    public double MaxSpeed { get; }

    /// <summary>Minimum time gap the agent tries to keep to agents ahead.</summary>
    public double TimeGap { get; }

    /// <summary>Delay before the agent reacts to velocity changes of nearby agents.</summary>
    public double ReactionTime { get; }

    /// <summary>How far ahead in time the agent anticipates collisions with neighbors.</summary>
    public double AnticipationTime { get; }

    /// <summary>Scaling factor for the lateral separation force between agents.</summary>
    public double SeparationWeight { get; }

    /// <summary>Magnitude of the repulsion force applied when neighbors are too close.</summary>
    public double NeighborRepulsionStrength { get; }

    /// <summary>Distance at which neighbor repulsion begins to take effect.</summary>
    public double NeighborRepulsionRange { get; }

    /// <summary>Preferred personal-space distance the agent tries to keep from others.</summary>
    public double ComfortDistance { get; }

    /// <summary>Distance to the exit center at which the agent is considered to have arrived.</summary>
    public double ArrivalThreshold { get; }

    /// <summary>Per-agent randomisation range applied to speed and spacing values at spawn.</summary>
    public double VariationPercent { get; }

    /// <summary>Small random perturbation added to the steering direction each step.</summary>
    public double SteeringNoise { get; }

    /// <summary>How strongly local crowd density slows the agent's preferred speed.</summary>
    public double DensityWeight { get; }

    /// <summary>Spatial scatter radius applied to the agent position at spawn time.</summary>
    public double SpawnJitter { get; }

    /// <summary>Probability weight for choosing an exit randomly instead of by field cost.</summary>
    public double ExitChoiceRandomness { get; }

    /// <summary>Multiplier controlling how much congestion ahead deters the agent from its current path.</summary>
    public double CongestionSensitivity { get; }

    /// <summary>Resistance to changing the committed exit once one has been selected.</summary>
    public double ExitCommitment { get; }

    /// <summary>Time interval between exit-choice re-evaluations.</summary>
    public double ReassessmentInterval { get; }

    /// <summary>Strength of the steering force that keeps agents away from walls.</summary>
    public double WallAvoidance { get; }

    /// <summary>Minimum distance the agent tries to maintain from wall surfaces.</summary>
    public double WallBufferDistance { get; }

    /// <summary>Look-ahead weight used to smooth steering around tight corners.</summary>
    public double TurnAnticipation { get; }

    /// <summary>Lateral bias controlling whether the agent tends to keep left or right.</summary>
    public double PreferredSideBias { get; }

    /// <summary>Default agent profile tuned for typical pedestrian behaviour.</summary>
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
