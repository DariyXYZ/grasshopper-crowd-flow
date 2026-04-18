using Crowd.Models;
using Rhino.Geometry;

namespace Crowd.Services;

public static class CrowdModelService
{
    public static CrowdAgentProfile CreateAgentProfile(
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
        CrowdAgentProfile defaults = CrowdAgentProfile.Default;
        return CreateAgentProfile(
            radius,
            preferredSpeed,
            maxSpeed,
            defaults.TimeGap,
            defaults.ReactionTime,
            defaults.AnticipationTime,
            separationWeight,
            defaults.NeighborRepulsionStrength,
            defaults.NeighborRepulsionRange,
            defaults.ComfortDistance,
            arrivalThreshold,
            variationPercent,
            steeringNoise,
            densityWeight,
            spawnJitter,
            exitChoiceRandomness,
            congestionSensitivity,
            exitCommitment,
            reassessmentInterval,
            wallAvoidance,
            defaults.WallBufferDistance,
            turnAnticipation,
            defaults.PreferredSideBias);
    }

    public static CrowdFloor CreateFloor(Curve boundary, double cellSize)
    {
        if (boundary == null)
        {
            throw new ArgumentNullException(nameof(boundary));
        }
        if (!boundary.IsClosed)
        {
            throw new ArgumentException("Floor boundary must be closed.", nameof(boundary));
        }

        if (!boundary.IsPlanar())
        {
            throw new ArgumentException("Floor boundary must be planar.", nameof(boundary));
        }

        if (!boundary.TryGetPlane(out Plane plane))
        {
            throw new ArgumentException("Unable to derive a plane from the floor boundary.", nameof(boundary));
        }

        if (cellSize <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be greater than zero.");
        }

        return new CrowdFloor(boundary.DuplicateCurve(), plane, cellSize);
    }

    public static CrowdObstacle CreateObstacle(Curve boundary)
    {
        if (boundary == null)
        {
            throw new ArgumentNullException(nameof(boundary));
        }
        if (!boundary.IsClosed)
        {
            throw new ArgumentException("Obstacle boundary must be closed.", nameof(boundary));
        }

        return new CrowdObstacle(boundary.DuplicateCurve());
    }

    public static CrowdSource CreateSource(Point3d location, int totalAgents, double spawnRate, int? exitIndex, double startTime)
    {
        if (totalAgents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAgents), "Total agents must be zero or greater.");
        }

        if (spawnRate <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(spawnRate), "Spawn rate must be greater than zero.");
        }

        if (startTime < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(startTime), "Start time must be zero or greater.");
        }

        return new CrowdSource(location, totalAgents, spawnRate, exitIndex, startTime);
    }

    public static CrowdExit CreateExit(Point3d location, double radius)
    {
        if (radius <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Exit radius must be greater than zero.");
        }

        return new CrowdExit(location, radius);
    }

    public static CrowdAgentProfile CreateAgentProfile(
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
        if (radius <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be greater than zero.");
        }

        if (preferredSpeed <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(preferredSpeed), "Preferred speed must be greater than zero.");
        }

        if (maxSpeed < preferredSpeed)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSpeed), "Max speed must be greater than or equal to preferred speed.");
        }

        if (timeGap <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeGap), "Time gap must be greater than zero.");
        }

        if (reactionTime <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(reactionTime), "Reaction time must be greater than zero.");
        }

        if (anticipationTime <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(anticipationTime), "Anticipation time must be greater than zero.");
        }

        if (separationWeight < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(separationWeight), "Separation weight must be zero or greater.");
        }

        if (neighborRepulsionStrength < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(neighborRepulsionStrength), "Neighbor repulsion strength must be zero or greater.");
        }

        if (neighborRepulsionRange <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(neighborRepulsionRange), "Neighbor repulsion range must be greater than zero.");
        }

        if (comfortDistance < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(comfortDistance), "Comfort distance must be zero or greater.");
        }

        if (arrivalThreshold <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrivalThreshold), "Arrival threshold must be greater than zero.");
        }

        if (variationPercent < 0.0 || variationPercent > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(variationPercent), "Variation percent must be between 0 and 1.");
        }

        if (steeringNoise < 0.0 || steeringNoise > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(steeringNoise), "Steering noise must be between 0 and 1.");
        }

        if (densityWeight < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(densityWeight), "Density weight must be zero or greater.");
        }

        if (spawnJitter < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(spawnJitter), "Spawn jitter must be zero or greater.");
        }

        if (exitChoiceRandomness < 0.0 || exitChoiceRandomness > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(exitChoiceRandomness), "Exit choice randomness must be between 0 and 1.");
        }

        if (congestionSensitivity < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(congestionSensitivity), "Congestion sensitivity must be zero or greater.");
        }

        if (exitCommitment < 0.0 || exitCommitment > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(exitCommitment), "Exit commitment must be between 0 and 1.");
        }

        if (reassessmentInterval < 0.1)
        {
            throw new ArgumentOutOfRangeException(nameof(reassessmentInterval), "Reassessment interval must be at least 0.1 seconds.");
        }

        if (wallAvoidance < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(wallAvoidance), "Wall avoidance must be zero or greater.");
        }

        if (wallBufferDistance < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(wallBufferDistance), "Wall buffer distance must be zero or greater.");
        }

        if (turnAnticipation < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(turnAnticipation), "Turn anticipation must be zero or greater.");
        }

        if (preferredSideBias < 0.0 || preferredSideBias > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(preferredSideBias), "Preferred side bias must be between 0 and 1.");
        }

        return new CrowdAgentProfile(
            radius,
            preferredSpeed,
            maxSpeed,
            timeGap,
            reactionTime,
            anticipationTime,
            separationWeight,
            neighborRepulsionStrength,
            neighborRepulsionRange,
            comfortDistance,
            arrivalThreshold,
            variationPercent,
            steeringNoise,
            densityWeight,
            spawnJitter,
            exitChoiceRandomness,
            congestionSensitivity,
            exitCommitment,
            reassessmentInterval,
            wallAvoidance,
            wallBufferDistance,
            turnAnticipation,
            preferredSideBias);
    }

    public static CrowdModel CreateModel(
        CrowdFloor floor,
        IReadOnlyList<CrowdObstacle> obstacles,
        IReadOnlyList<CrowdSource> sources,
        IReadOnlyList<CrowdExit> exits,
        CrowdAgentProfile? profile,
        double timeStep)
    {
        if (floor == null)
        {
            throw new ArgumentNullException(nameof(floor));
        }

        if (obstacles == null)
        {
            throw new ArgumentNullException(nameof(obstacles));
        }

        if (sources == null)
        {
            throw new ArgumentNullException(nameof(sources));
        }

        if (exits == null)
        {
            throw new ArgumentNullException(nameof(exits));
        }

        if (sources.Count == 0)
        {
            throw new ArgumentException("At least one crowd source is required.", nameof(sources));
        }

        if (exits.Count == 0)
        {
            throw new ArgumentException("At least one exit is required.", nameof(exits));
        }

        if (timeStep <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeStep), "Time step must be greater than zero.");
        }

        return new CrowdModel(floor, obstacles, sources, exits, profile ?? CrowdAgentProfile.Default, timeStep);
    }
}
