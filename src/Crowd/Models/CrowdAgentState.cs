using Rhino.Geometry;

namespace Crowd.Models;

/// <summary>Holds the mutable runtime state of a single agent throughout a simulation.</summary>
public sealed class CrowdAgentState
{
    /// <summary>Initializes a new <see cref="CrowdAgentState"/> with all per-agent identity, locomotion, and decision parameters.</summary>
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

    /// <summary>Unique identifier for this agent within the simulation.</summary>
    public int Id { get; }

    /// <summary>Current world position of the agent.</summary>
    public Point3d Position { get; set; }

    /// <summary>Current velocity vector of the agent.</summary>
    public Vector3d Velocity { get; set; }

    /// <summary>Target velocity the agent is steering toward this step.</summary>
    public Vector3d DesiredVelocity { get; set; }

    /// <summary>Index of the exit the agent is currently targeting.</summary>
    public int ExitIndex { get; set; }

    /// <summary>True when the agent was assigned a fixed exit at spawn and cannot re-evaluate.</summary>
    public bool HasFixedExit { get; }

    /// <summary>Simulation time at which this agent was spawned.</summary>
    public double SpawnTime { get; }

    /// <summary>Physical radius of the agent used for collision checks.</summary>
    public double Radius { get; }

    /// <summary>Per-agent preferred walking speed derived from the profile with variation applied.</summary>
    public double PreferredSpeed { get; }

    /// <summary>Per-agent maximum speed cap.</summary>
    public double MaxSpeed { get; }

    /// <summary>Minimum time gap this agent keeps to agents ahead.</summary>
    public double TimeGap { get; }

    /// <summary>Reaction delay before this agent responds to neighbor velocity changes.</summary>
    public double ReactionTime { get; }

    /// <summary>Collision-anticipation look-ahead time for this agent.</summary>
    public double AnticipationTime { get; }

    /// <summary>Lateral separation force scaling factor for this agent.</summary>
    public double SeparationWeight { get; }

    /// <summary>Magnitude of neighbor repulsion force for this agent.</summary>
    public double NeighborRepulsionStrength { get; }

    /// <summary>Distance at which neighbor repulsion activates for this agent.</summary>
    public double NeighborRepulsionRange { get; }

    /// <summary>Preferred personal-space distance this agent maintains from others.</summary>
    public double ComfortDistance { get; }

    /// <summary>Arrival distance threshold at which this agent is considered to have reached its exit.</summary>
    public double ArrivalThreshold { get; }

    /// <summary>Per-agent random phase offset added to steering noise each step.</summary>
    public double NoiseOffset { get; }

    /// <summary>Per-agent lateral lane bias (positive = right, negative = left).</summary>
    public double SideBias { get; }

    /// <summary>Reluctance of this agent to deviate from its current planned route.</summary>
    public double RouteCommitment { get; }

    /// <summary>Per-agent probability weight for random exit selection.</summary>
    public double ExitChoiceRandomness { get; }

    /// <summary>Multiplier controlling how strongly congestion deters this agent.</summary>
    public double CongestionSensitivity { get; }

    /// <summary>Resistance to switching exits once an exit has been committed to.</summary>
    public double ExitCommitment { get; }

    /// <summary>Time interval between exit re-evaluations for this agent.</summary>
    public double ReassessmentInterval { get; }

    /// <summary>Minimum wall clearance distance this agent tries to maintain.</summary>
    public double WallBufferDistance { get; }

    /// <summary>Magnitude of random wander applied when the agent is not near congestion.</summary>
    public double WanderStrength { get; }

    /// <summary>Bias toward smoother, lower-curvature paths during route following.</summary>
    public double CurvaturePreference { get; }

    /// <summary>Initial random scatter strength applied in the first moments after spawn.</summary>
    public double StartScatterStrength { get; }

    /// <summary>Delay after spawn before the agent fully focuses on its target exit.</summary>
    public double FocusDelay { get; }

    /// <summary>Last sampled distance field value used to detect stall or progress.</summary>
    public double LastFieldDistance { get; set; }

    /// <summary>Accumulated time the agent has been considered stuck without progress.</summary>
    public double StuckDuration { get; set; }

    /// <summary>Per-agent lane-keeping commitment bias.</summary>
    public double LaneCommitmentBias { get; }

    /// <summary>Simulation time at which the agent will next re-evaluate its exit choice.</summary>
    public double NextExitDecisionTime { get; set; }

    /// <summary>Simulation time at which this agent reached an exit; null if not yet finished.</summary>
    public double? FinishTime { get; set; }

    /// <summary>True when the agent has reached an exit and been removed from active simulation.</summary>
    public bool IsFinished => FinishTime.HasValue;
}
