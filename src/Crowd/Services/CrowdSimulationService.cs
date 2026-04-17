using Crowd.Models;
using Crowd.Utilities;
using Rhino.Geometry;

namespace Crowd.Services;

public static class CrowdSimulationService
{
    private const double StalledMoveFactor = 0.5;
    private const double NoiseFrequency = 0.65;
    private const double AnticipationTime = 0.6;
    private const double VelocityBlendFactor = 0.82;
    private const double DesiredVelocityBlendFactor = 0.9;
    private const double CandidateFieldWeight = 1.9;
    private const double CandidateHeadingWeight = 1.1;
    private const double CandidateDensityWeight = 1.5;
    private const double CandidateClearanceWeight = 1.3;
    private const double CandidateRandomnessWeight = 0.18;
    private const double WallInfluenceFactor = 2.4;
    private const double WallRepulsionWeight = 1.6;
    private const double TimeToCollisionHorizon = 1.4;
    private const double TimeToCollisionWeight = 1.8;
    private const double CandidateTurnWeight = 1.15;
    private const double CandidateBlendTemperature = 0.55;
    private const double StartScatterDurationMin = 1.4;
    private const double StartScatterDurationMax = 3.2;
    private const double FinalApproachDistanceFactor = 4.5;
    private const double FinalApproachSeparationFactor = 0.4;
    private const double FinalApproachNoiseFactor = 0.15;
    private const double FinalApproachWanderFactor = 0.08;
    private const double FinalApproachLaneBiasFactor = 0.2;
    private const double FinalApproachPullWeight = 1.35;
    private const double ExitAbsorptionDistanceFactor = 2.8;
    private const double ExitSnapSpeedThreshold = 0.35;
    private const double ExitApproachDampingFactor = 0.32;
    private const double ExitSpiralDetectionDistanceFactor = 4.0;
    private const double EntranceDiffusionFieldFactor = 0.52;
    private const double EntranceDiffusionRandomFactor = 1.8;
    private const double EntranceDiffusionSideFactor = 1.65;
    private const double CurvaturePenaltyWeight = 1.1;
    private const double ExitDistanceWeight = 1.0;
    private const double ExitCongestionWeight = 1.15;
    private const double ExitSwitchThreshold = 0.16;
    private const double ExitAwarenessRadius = 4.5;
    private const double ExitQueueRadius = 6.0;
    private const double ExitSoftmaxTemperature = 0.42;

    /// <summary>
    /// Simulates pedestrians on a 2D walkable floor using a grid-derived distance field and lightweight local avoidance.
    /// The solver is intentionally compact: it targets architectural flow studies rather than high-fidelity behavioral research.
    /// </summary>
    /// <param name="model">Crowd model assembled from floor, obstacles, sources, exits, and agent profile.</param>
    /// <returns>Recorded frames, agent paths, and summary counts for downstream Grasshopper visualization.</returns>
    public static CrowdSimulationResult Run(CrowdModel model)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        CrowdGrid grid = new(model.Floor, model.Obstacles);
        List<double[,]> exitFields = model.Exits.Select(exit => CrowdPathFieldBuilder.Build(grid, exit)).ToList();
        List<CrowdAgentState> agents = new();
        Dictionary<int, List<Point3d>> trajectories = new();
        List<CrowdFrame> frames = new();
        double[] sourceRemainders = new double[model.Sources.Count];
        int[] sourceSpawned = new int[model.Sources.Count];
        int nextAgentId = 1;
        double time = 0.0;
        Random random = new(12345);
        int totalExpectedAgents = model.Sources.Sum(source => source.TotalAgents);
        double maxSimulationDuration = EstimateMaximumSimulationDuration(model);

        RecordFrame(frames, agents, time);

        while (time < maxSimulationDuration)
        {
            SpawnAgents(model, grid, exitFields, agents, trajectories, sourceRemainders, sourceSpawned, ref nextAgentId, time, random);
            UpdateAgents(model, grid, exitFields, agents, trajectories, time, random);

            time += model.TimeStep;
            RecordFrame(frames, agents, time);

            if (sourceSpawned.Sum() >= totalExpectedAgents && agents.Count(agent => !agent.IsFinished) == 0)
            {
                break;
            }
        }

        List<CrowdAgentPath> paths = agents
            .OrderBy(agent => agent.Id)
            .Select(agent => new CrowdAgentPath(
                agent.Id,
                new Polyline(trajectories.TryGetValue(agent.Id, out List<Point3d>? points) ? points : Enumerable.Empty<Point3d>()),
                agent.IsFinished,
                agent.SpawnTime,
                agent.FinishTime))
            .ToList();

        return new CrowdSimulationResult(
            model,
            frames,
            paths,
            totalSpawned: agents.Count,
            totalFinished: agents.Count(agent => agent.IsFinished),
            simulatedDuration: time,
            completedAllAgents: agents.Count == totalExpectedAgents && agents.All(agent => agent.IsFinished));
    }

    private static void SpawnAgents(
        CrowdModel model,
        CrowdGrid grid,
        IReadOnlyList<double[,]> exitFields,
        List<CrowdAgentState> agents,
        Dictionary<int, List<Point3d>> trajectories,
        double[] sourceRemainders,
        int[] sourceSpawned,
        ref int nextAgentId,
        double time,
        Random random)
    {
        for (int sourceIndex = 0; sourceIndex < model.Sources.Count; sourceIndex++)
        {
            CrowdSource source = model.Sources[sourceIndex];
            if (time + 1e-9 < source.StartTime || sourceSpawned[sourceIndex] >= source.TotalAgents)
            {
                continue;
            }

            sourceRemainders[sourceIndex] += source.SpawnRate * model.TimeStep;
            int available = source.TotalAgents - sourceSpawned[sourceIndex];
            int toSpawn = Math.Min(available, (int)Math.Floor(sourceRemainders[sourceIndex]));
            if (toSpawn <= 0)
            {
                continue;
            }

            sourceRemainders[sourceIndex] -= toSpawn;
            for (int count = 0; count < toSpawn; count++)
            {
                if (!grid.TryGetClosestWalkableCell(source.Location, out int sx, out int sy))
                {
                    continue;
                }

                Point3d spawnPoint = CreateJitteredSpawnPoint(source.Location, grid, model.AgentProfile, random);
                if (IsOccupied(spawnPoint, agents, model.AgentProfile.Radius * 1.2))
                {
                    continue;
                }

                CrowdAgentState agent = CreateAgent(nextAgentId++, spawnPoint, source, grid, exitFields, agents, time, model.AgentProfile, random);
                agents.Add(agent);
                trajectories[agent.Id] = new List<Point3d> { spawnPoint };
                sourceSpawned[sourceIndex]++;
            }
        }
    }

    private static void UpdateAgents(
        CrowdModel model,
        CrowdGrid grid,
        IReadOnlyList<double[,]> exitFields,
        List<CrowdAgentState> agents,
        Dictionary<int, List<Point3d>> trajectories,
        double time,
        Random random)
    {
        List<CrowdAgentState> activeAgents = agents.Where(agent => !agent.IsFinished).ToList();
        foreach (CrowdAgentState agent in activeAgents)
        {
            MaybeReevaluateExit(agent, model, grid, exitFields, activeAgents, time, random);
            CrowdExit targetExit = model.Exits[agent.ExitIndex];
            if (TryAbsorbAtExit(agent, targetExit, time))
            {
                AppendTrajectoryPoint(trajectories, agent.Id, agent.Position);
                continue;
            }

            Vector3d desiredVelocity = CalculateDesiredVelocity(agent, model, grid, targetExit, exitFields[agent.ExitIndex], activeAgents, time);
            desiredVelocity = ApplyExitApproachDamping(agent, targetExit, desiredVelocity);
            agent.DesiredVelocity = BlendVelocity(agent.DesiredVelocity, desiredVelocity, agent.MaxSpeed, DesiredVelocityBlendFactor);
            Vector3d motionVelocity = BlendVelocity(agent.Velocity, agent.DesiredVelocity, agent.MaxSpeed, VelocityBlendFactor);

            Point3d proposed = agent.Position + (motionVelocity * model.TimeStep);
            if (!grid.IsWalkable(proposed) || IsOccupiedByOthers(agent, proposed, activeAgents, agent.Radius * 1.5))
            {
                proposed = agent.Position + (motionVelocity * (model.TimeStep * StalledMoveFactor));
                if (!grid.IsWalkable(proposed) || IsOccupiedByOthers(agent, proposed, activeAgents, agent.Radius * 1.35))
                {
                    proposed = agent.Position;
                }
            }

            agent.Velocity = (proposed - agent.Position) / Math.Max(model.TimeStep, 1e-6);
            agent.Position = proposed;

            AppendTrajectoryPoint(trajectories, agent.Id, proposed);

            if (TryAbsorbAtExit(agent, targetExit, time + model.TimeStep))
            {
                AppendTrajectoryPoint(trajectories, agent.Id, agent.Position);
                continue;
            }
        }
    }

    private static Vector3d CalculateDesiredVelocity(
        CrowdAgentState agent,
        CrowdModel model,
        CrowdGrid grid,
        CrowdExit targetExit,
        double[,] field,
        IReadOnlyList<CrowdAgentState> activeAgents,
        double time)
    {
        if (!grid.TryGetClosestWalkableCell(agent.Position, out int x, out int y))
        {
            return Vector3d.Zero;
        }

        Point3d targetPoint = SelectPreferredTargetPoint(agent, model, grid, field, activeAgents, x, y, time);
        Vector3d localDirection = targetPoint - agent.Position;
        if (!localDirection.Unitize())
        {
            localDirection = Vector3d.Zero;
        }

        double routeFocus = GetRouteFocusFactor(agent, time);
        Vector3d flowDirection = SampleContinuousFlowDirection(agent.Position, grid, field);
        Vector3d direction = CombineDirections(flowDirection, localDirection, 0.56 + (0.24 * routeFocus));
        if (!direction.Unitize())
        {
            direction = localDirection;
        }

        double finalApproach = GetFinalApproachFactor(agent, targetExit);

        Vector3d separation = Vector3d.Zero;
        Vector3d collisionAvoidance = Vector3d.Zero;
        double neighborhoodRadius = agent.Radius * 3.0;
        Vector3d interactionHeading = agent.Velocity;
        if (!interactionHeading.Unitize())
        {
            interactionHeading = direction;
        }

        foreach (CrowdAgentState other in activeAgents)
        {
            if (other.Id == agent.Id)
            {
                continue;
            }

            Point3d predictedSelf = agent.Position + (agent.Velocity * AnticipationTime);
            Point3d predictedOther = other.Position + (other.Velocity * AnticipationTime);

            Vector3d away = agent.Position - other.Position;
            double distance = away.Length;
            double predictedDistance = predictedSelf.DistanceTo(predictedOther);
            if (distance <= 1e-6 || distance > neighborhoodRadius)
            {
                continue;
            }

            away.Unitize();
            Vector3d toOther = other.Position - agent.Position;
            if (!toOther.Unitize())
            {
                toOther = Vector3d.Zero;
            }

            double forwardFactor = interactionHeading.Length <= 1e-6
                ? 1.0
                : RemapClamped((Vector3d.Multiply(interactionHeading, toOther) + 1.0) * 0.5, 0.0, 1.0, 0.35, 1.35);

            double anticipatedCloseness = (neighborhoodRadius - Math.Min(distance, predictedDistance)) / neighborhoodRadius;
            double weight = Math.Max(0.0, anticipatedCloseness) * forwardFactor;
            separation += away * (weight * agent.SeparationWeight * Lerp(1.0, FinalApproachSeparationFactor, finalApproach));

            Vector3d ttcAvoidance = CalculateTimeToCollisionAvoidance(agent, other);
            if (ttcAvoidance.Length > 1e-6)
            {
                collisionAvoidance += ttcAvoidance * forwardFactor;
            }
        }

        Vector3d densityAvoidance = CalculateDensityAvoidance(agent, grid, field, model, activeAgents);
        Vector3d wallRepulsion = CalculateWallRepulsion(agent, grid, model.AgentProfile.WallAvoidance);
        Vector3d noiseVector = CalculateNoiseVector(agent, direction, time, model.AgentProfile.SteeringNoise * Lerp(1.0, FinalApproachNoiseFactor, finalApproach));
        Vector3d laneBias = CalculateLaneBias(agent, direction) * Lerp(1.0, FinalApproachLaneBiasFactor, finalApproach);
        Vector3d startScatter = CalculateStartScatter(agent, direction, time);
        Vector3d wanderVector = CalculateWanderVector(agent, direction, time) * Lerp(1.0, FinalApproachWanderFactor, finalApproach);
        Vector3d finalApproachPull = CalculateFinalApproachPull(agent, targetExit) * (finalApproach * FinalApproachPullWeight);

        Vector3d combined = direction + separation + collisionAvoidance + densityAvoidance + wallRepulsion + noiseVector + laneBias + startScatter + wanderVector + finalApproachPull;
        if (!combined.Unitize())
        {
            combined = direction;
        }

        if (!combined.Unitize())
        {
            return Vector3d.Zero;
        }

        return ApplyTurnSmoothing(agent, combined, model.AgentProfile.TurnAnticipation) * Math.Min(agent.PreferredSpeed, agent.MaxSpeed);
    }

    private static CrowdAgentState CreateAgent(
        int id,
        Point3d position,
        CrowdSource source,
        CrowdGrid grid,
        IReadOnlyList<double[,]> exitFields,
        IReadOnlyList<CrowdAgentState> existingAgents,
        double spawnTime,
        CrowdAgentProfile profile,
        Random random)
    {
        double radius = SampleAround(profile.Radius, profile.VariationPercent, random, 0.05);
        double preferredSpeed = SampleAround(profile.PreferredSpeed, profile.VariationPercent, random, 0.1);
        double maxSpeed = Math.Max(preferredSpeed, SampleAround(profile.MaxSpeed, profile.VariationPercent, random, preferredSpeed));
        double separationWeight = SampleAround(profile.SeparationWeight, profile.VariationPercent, random, 0.05);
        double arrivalThreshold = SampleAround(profile.ArrivalThreshold, profile.VariationPercent * 0.5, random, 0.1);
        double noiseOffset = random.NextDouble() * Math.PI * 2.0;
        double sideBias = random.NextDouble() * 2.0 - 1.0;
        double routeCommitment = 0.75 + (random.NextDouble() * 0.35);
        double exitChoiceRandomness = SampleAround(profile.ExitChoiceRandomness, profile.VariationPercent * 0.5, random, 0.01);
        double congestionSensitivity = SampleAround(profile.CongestionSensitivity, profile.VariationPercent * 0.5, random, 0.05);
        double exitCommitment = RemapClamped(
            SampleAround(profile.ExitCommitment, profile.VariationPercent * 0.35, random, 0.05),
            0.0,
            1.0,
            0.0,
            1.0);
        double reassessmentInterval = SampleAround(profile.ReassessmentInterval, profile.VariationPercent * 0.35, random, 0.15);
        double wanderStrength = SampleBehavior(random, 0.18, 0.38, 0.72);
        double curvaturePreference = SampleBehavior(random, 0.85, 1.15, 1.45);
        double startScatterStrength = SampleBehavior(random, 0.35, 0.75, 1.2);
        double focusDelay = StartScatterDurationMin + (random.NextDouble() * (StartScatterDurationMax - StartScatterDurationMin));
        int exitIndex = ChooseExitIndex(
            source,
            position,
            grid,
            exitFields,
            existingAgents,
            noiseOffset,
            exitChoiceRandomness,
            congestionSensitivity,
            random);

        return new CrowdAgentState(
            id,
            position,
            exitIndex,
            source.ExitIndex.HasValue,
            spawnTime,
            radius,
            preferredSpeed,
            maxSpeed,
            separationWeight,
            arrivalThreshold,
            noiseOffset,
            sideBias,
            routeCommitment,
            exitChoiceRandomness,
            congestionSensitivity,
            exitCommitment,
            reassessmentInterval,
            wanderStrength,
            curvaturePreference,
            startScatterStrength,
            focusDelay);
    }

    private static void MaybeReevaluateExit(
        CrowdAgentState agent,
        CrowdModel model,
        CrowdGrid grid,
        IReadOnlyList<double[,]> exitFields,
        IReadOnlyList<CrowdAgentState> activeAgents,
        double time,
        Random random)
    {
        if (agent.HasFixedExit || time + 1e-9 < agent.NextExitDecisionTime || model.Exits.Count <= 1)
        {
            return;
        }

        int currentExit = agent.ExitIndex;
        int proposedExit = ChooseExitIndex(
            null,
            agent.Position,
            grid,
            exitFields,
            activeAgents,
            agent.NoiseOffset,
            agent.ExitChoiceRandomness,
            agent.CongestionSensitivity,
            random);

        agent.NextExitDecisionTime = time + agent.ReassessmentInterval;
        if (proposedExit == currentExit)
        {
            return;
        }

        double currentUtility = EvaluateExitUtility(agent.Position, currentExit, grid, exitFields, activeAgents, agent.NoiseOffset, agent.CongestionSensitivity);
        double proposedUtility = EvaluateExitUtility(agent.Position, proposedExit, grid, exitFields, activeAgents, agent.NoiseOffset, agent.CongestionSensitivity);
        double switchPenalty = agent.ExitCommitment * ExitSwitchThreshold;
        if (proposedUtility > currentUtility + switchPenalty)
        {
            agent.ExitIndex = proposedExit;
        }
    }

    private static int ChooseExitIndex(
        CrowdSource? source,
        Point3d position,
        CrowdGrid grid,
        IReadOnlyList<double[,]> exitFields,
        IReadOnlyList<CrowdAgentState> activeAgents,
        double noiseOffset,
        double exitChoiceRandomness,
        double congestionSensitivity,
        Random random)
    {
        if (source?.ExitIndex.HasValue == true && source.ExitIndex.Value >= 0 && source.ExitIndex.Value < exitFields.Count)
        {
            return source.ExitIndex.Value;
        }

        if (!grid.TryGetClosestWalkableCell(position, out _, out _))
        {
            return 0;
        }

        List<(int Index, double Utility)> candidates = new(exitFields.Count);
        for (int i = 0; i < exitFields.Count; i++)
        {
            double utility = EvaluateExitUtility(position, i, grid, exitFields, activeAgents, noiseOffset, congestionSensitivity);
            if (!double.IsNegativeInfinity(utility))
            {
                candidates.Add((i, utility));
            }
        }

        if (candidates.Count == 0)
        {
            return 0;
        }

        double randomness = Math.Max(0.01, exitChoiceRandomness);
        double bestUtility = candidates.Max(candidate => candidate.Utility);
        List<double> weights = new(candidates.Count);
        double totalWeight = 0.0;

        foreach (var candidate in candidates)
        {
            double scaled = (candidate.Utility - bestUtility) / Math.Max(ExitSoftmaxTemperature, randomness * 1.2);
            double weight = Math.Exp(Math.Max(-20.0, Math.Min(0.0, scaled)));
            weight = Math.Max(1e-6, weight);
            weights.Add(weight);
            totalWeight += weight;
        }

        double pick = random.NextDouble() * totalWeight;
        double cumulative = 0.0;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += weights[i];
            if (pick <= cumulative)
            {
                return candidates[i].Index;
            }
        }

        return candidates[candidates.Count - 1].Index;
    }

    private static double EvaluateExitUtility(
        Point3d position,
        int exitIndex,
        CrowdGrid grid,
        IReadOnlyList<double[,]> exitFields,
        IReadOnlyList<CrowdAgentState> activeAgents,
        double noiseOffset,
        double congestionSensitivity)
    {
        if (!grid.TryGetClosestWalkableCell(position, out int x, out int y))
        {
            return double.NegativeInfinity;
        }

        double distance = exitFields[exitIndex][x, y];
        if (double.IsInfinity(distance))
        {
            return double.NegativeInfinity;
        }

        double congestion = EstimateExitCongestion(exitIndex, position, activeAgents, distance);
        double personalBias = Math.Sin(noiseOffset + (exitIndex * 1.713)) * 0.5;
        double imperfectDistance = distance * (1.0 + (Math.Cos(noiseOffset + (exitIndex * 0.917)) * 0.12));

        return
            (-imperfectDistance * ExitDistanceWeight) +
            (-congestion * congestionSensitivity * ExitCongestionWeight) +
            personalBias;
    }

    private static double EstimateExitCongestion(
        int exitIndex,
        Point3d position,
        IReadOnlyList<CrowdAgentState> activeAgents,
        double pathDistance)
    {
        double localCompetition = 0.0;
        double queueCompetition = 0.0;

        foreach (CrowdAgentState other in activeAgents)
        {
            if (other.IsFinished || other.ExitIndex != exitIndex)
            {
                continue;
            }

            double distanceToAgent = other.Position.DistanceTo(position);
            if (distanceToAgent <= ExitAwarenessRadius)
            {
                localCompetition += 1.0 - Math.Min(1.0, distanceToAgent / ExitAwarenessRadius);
            }

            double normalizedPath = pathDistance <= 1e-6 ? 0.0 : Math.Min(1.0, distanceToAgent / Math.Max(ExitQueueRadius, pathDistance * 0.5));
            queueCompetition += 1.0 - normalizedPath;
        }

        return (localCompetition * 0.8) + (queueCompetition * 0.22);
    }

    private static Point3d CreateJitteredSpawnPoint(Point3d sourceLocation, CrowdGrid grid, CrowdAgentProfile profile, Random random)
    {
        if (profile.SpawnJitter <= 1e-6)
        {
            if (grid.TryGetClosestWalkableCell(sourceLocation, out int bx, out int by))
            {
                return grid.GetCellCenter(bx, by);
            }

            return sourceLocation;
        }

        for (int attempt = 0; attempt < 8; attempt++)
        {
            double angle = random.NextDouble() * Math.PI * 2.0;
            double distance = random.NextDouble() * profile.SpawnJitter;
            Point3d candidate = new(
                sourceLocation.X + (Math.Cos(angle) * distance),
                sourceLocation.Y + (Math.Sin(angle) * distance),
                sourceLocation.Z);

            if (grid.TryGetClosestWalkableCell(candidate, out int x, out int y))
            {
                return grid.GetCellCenter(x, y);
            }
        }

        if (grid.TryGetClosestWalkableCell(sourceLocation, out int sx, out int sy))
        {
            return grid.GetCellCenter(sx, sy);
        }

        return sourceLocation;
    }

    private static Vector3d CalculateDensityAvoidance(
        CrowdAgentState agent,
        CrowdGrid grid,
        double[,] field,
        CrowdModel model,
        IReadOnlyList<CrowdAgentState> activeAgents)
    {
        if (!grid.TryGetClosestWalkableCell(agent.Position, out int x, out int y))
        {
            return Vector3d.Zero;
        }

        double currentDensity = EstimateNeighborhoodDensity(agent.Position, activeAgents, agent.Id, agent.Radius * 4.0);
        Vector3d avoidance = Vector3d.Zero;

        int[] offsets = new[] { -1, 0, 1 };
        foreach (int ox in offsets)
        {
            foreach (int oy in offsets)
            {
                if (ox == 0 && oy == 0)
                {
                    continue;
                }

                int nx = x + ox;
                int ny = y + oy;
                if (!grid.IsWalkable(nx, ny))
                {
                    continue;
                }

                Point3d neighbor = grid.GetCellCenter(nx, ny);
                double neighborDensity = EstimateNeighborhoodDensity(neighbor, activeAgents, agent.Id, agent.Radius * 4.0);
                double gradient = currentDensity - neighborDensity;
                if (gradient <= 0.0)
                {
                    continue;
                }

                Vector3d dir = neighbor - agent.Position;
                if (!dir.Unitize())
                {
                    continue;
                }

                double fieldBias = double.IsInfinity(field[nx, ny]) ? 0.0 : 1.0 / (1.0 + field[nx, ny]);
                avoidance += dir * (gradient * (0.5 + fieldBias) * model.AgentProfile.DensityWeight);
            }
        }

        return avoidance;
    }

    private static Point3d SelectPreferredTargetPoint(
        CrowdAgentState agent,
        CrowdModel model,
        CrowdGrid grid,
        double[,] field,
        IReadOnlyList<CrowdAgentState> activeAgents,
        int x,
        int y,
        double time)
    {
        Point3d bestPoint = grid.GetCellCenter(x, y);
        double bestScore = double.NegativeInfinity;
        List<(Point3d Point, double Score)> candidates = new();
        Vector3d currentHeading = agent.Velocity;
        double routeFocus = GetRouteFocusFactor(agent, time);
        if (!currentHeading.Unitize())
        {
            currentHeading = Vector3d.Zero;
        }

        for (int ox = -1; ox <= 1; ox++)
        {
            for (int oy = -1; oy <= 1; oy++)
            {
                int nx = x + ox;
                int ny = y + oy;
                if (!grid.IsWalkable(nx, ny))
                {
                    continue;
                }

                double neighborField = field[nx, ny];
                if (double.IsInfinity(neighborField))
                {
                    continue;
                }

                Point3d candidate = grid.GetCellCenter(nx, ny);
                Vector3d candidateDirection = candidate - agent.Position;
                double candidateDistance = candidateDirection.Length;
                if (candidateDistance <= 1e-6)
                {
                    continue;
                }

                candidateDirection.Unitize();

                double fieldScore = -neighborField * CandidateFieldWeight * agent.RouteCommitment * Lerp(EntranceDiffusionFieldFactor, 1.0, routeFocus);
                double headingScore = currentHeading.Length <= 1e-6 ? 0.0 : Vector3d.Multiply(currentHeading, candidateDirection) * CandidateHeadingWeight;
                double densityScore = -EstimateNeighborhoodDensity(candidate, activeAgents, agent.Id, agent.Radius * 3.5) * CandidateDensityWeight;
                double clearanceScore = GetClearanceScore(candidate, grid, Math.Max(agent.Radius * (WallInfluenceFactor + model.AgentProfile.WallAvoidance), 1.4 + model.AgentProfile.WallAvoidance))
                    * CandidateClearanceWeight
                    * (0.8 + (model.AgentProfile.WallAvoidance * 0.5));
                Vector3d futureDirection = EstimateFieldDirection(grid, field, nx, ny);
                double turnScore = futureDirection.Length <= 1e-6
                    ? 0.0
                    : Vector3d.Multiply(candidateDirection, futureDirection) * CandidateTurnWeight * model.AgentProfile.TurnAnticipation * agent.CurvaturePreference;
                double sideScore = Vector3d.Multiply(new Vector3d(-candidateDirection.Y, candidateDirection.X, 0.0), currentHeading) * agent.SideBias * 0.12 * Lerp(EntranceDiffusionSideFactor, 1.0, routeFocus);
                double randomScore = Math.Sin((neighborField * 0.37) + agent.NoiseOffset) * CandidateRandomnessWeight * Lerp(EntranceDiffusionRandomFactor, 1.0, routeFocus);
                double curvaturePenalty = currentHeading.Length <= 1e-6
                    ? 0.0
                    : Math.Pow(Math.Max(0.0, 1.0 - Vector3d.Multiply(currentHeading, candidateDirection)), 2.0) * CurvaturePenaltyWeight * agent.CurvaturePreference * routeFocus;
                double score = fieldScore + headingScore + densityScore + clearanceScore + turnScore + sideScore + randomScore - curvaturePenalty;
                candidates.Add((candidate, score));

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = candidate;
                }
            }
        }

        if (candidates.Count == 0)
        {
            return bestPoint;
        }

        double totalWeight = 0.0;
        double sumX = 0.0;
        double sumY = 0.0;
        double sumZ = 0.0;
        foreach ((Point3d point, double score) in candidates)
        {
            double scaled = (score - bestScore) / CandidateBlendTemperature;
            double weight = Math.Exp(Math.Max(-20.0, Math.Min(0.0, scaled)));
            totalWeight += weight;
            sumX += point.X * weight;
            sumY += point.Y * weight;
            sumZ += point.Z * weight;
        }

        if (totalWeight <= 1e-9)
        {
            return bestPoint;
        }

        Point3d blendedPoint = new(sumX / totalWeight, sumY / totalWeight, sumZ / totalWeight);
        double anticipationBlend = RemapClamped(model.AgentProfile.TurnAnticipation * agent.CurvaturePreference, 0.0, 2.8, 0.24, 0.88);
        return LerpPoint(bestPoint, blendedPoint, anticipationBlend);
    }

    private static double GetClearanceScore(Point3d point, CrowdGrid grid, double influenceRadius)
    {
        Vector3d repulsion = grid.GetBoundaryRepulsion(point, influenceRadius);
        return -repulsion.Length;
    }

    private static Vector3d CalculateWallRepulsion(CrowdAgentState agent, CrowdGrid grid, double wallAvoidance)
    {
        double influenceRadius = Math.Max(agent.Radius * (WallInfluenceFactor + wallAvoidance), 1.35 + wallAvoidance);
        Vector3d currentRepulsion = grid.GetBoundaryRepulsion(agent.Position, influenceRadius);
        Vector3d velocityDirection = agent.Velocity;
        if (!velocityDirection.Unitize())
        {
            velocityDirection = Vector3d.Zero;
        }

        Point3d previewPoint = agent.Position + (velocityDirection * (influenceRadius * 0.85));
        Vector3d previewRepulsion = grid.GetBoundaryRepulsion(previewPoint, influenceRadius);
        Vector3d repulsion = currentRepulsion + (previewRepulsion * 0.9);
        if (repulsion.Length <= 1e-6)
        {
            return Vector3d.Zero;
        }

        return repulsion * WallRepulsionWeight * (0.7 + (wallAvoidance * 0.65));
    }

    private static Vector3d CalculateLaneBias(CrowdAgentState agent, Vector3d direction)
    {
        Vector3d tangent = new(-direction.Y, direction.X, 0.0);
        if (!tangent.Unitize())
        {
            return Vector3d.Zero;
        }

        return tangent * (agent.SideBias * 0.08);
    }

    private static double EstimateNeighborhoodDensity(Point3d point, IReadOnlyList<CrowdAgentState> agents, int currentAgentId, double radius)
    {
        double density = 0.0;
        foreach (CrowdAgentState other in agents)
        {
            if (other.Id == currentAgentId || other.IsFinished)
            {
                continue;
            }

            double distance = other.Position.DistanceTo(point);
            if (distance <= 1e-6 || distance > radius)
            {
                continue;
            }

            density += (radius - distance) / radius;
        }

        return density;
    }

    private static Vector3d CalculateNoiseVector(CrowdAgentState agent, Vector3d direction, double time, double steeringNoise)
    {
        if (agent.PreferredSpeed <= 1e-6 || steeringNoise <= 1e-6)
        {
            return Vector3d.Zero;
        }

        Vector3d tangent = new(-direction.Y, direction.X, 0.0);
        if (!tangent.Unitize())
        {
            return Vector3d.Zero;
        }

        double signal = Math.Sin((time * NoiseFrequency) + agent.NoiseOffset);
        double amplitude = agent.PreferredSpeed * steeringNoise * signal;
        return tangent * amplitude;
    }

    private static Vector3d CalculateStartScatter(CrowdAgentState agent, Vector3d direction, double time)
    {
        double age = GetEffectiveAge(agent, time);
        if (age <= 0.0 || age >= agent.FocusDelay)
        {
            return Vector3d.Zero;
        }

        Vector3d tangent = new(-direction.Y, direction.X, 0.0);
        if (!tangent.Unitize())
        {
            return Vector3d.Zero;
        }

        double fade = 1.0 - (age / Math.Max(0.1, agent.FocusDelay));
        double scatterSignal = Math.Sin((age * 2.2) + agent.NoiseOffset) + (Math.Cos((age * 1.1) + (agent.NoiseOffset * 0.7)) * 0.5);
        return tangent * scatterSignal * fade * agent.StartScatterStrength * 0.65;
    }

    private static Vector3d CalculateWanderVector(CrowdAgentState agent, Vector3d direction, double time)
    {
        if (agent.WanderStrength <= 1e-6)
        {
            return Vector3d.Zero;
        }

        Vector3d tangent = new(-direction.Y, direction.X, 0.0);
        if (!tangent.Unitize())
        {
            return Vector3d.Zero;
        }

        double signal =
            (Math.Sin((time * 0.75) + agent.NoiseOffset) * 0.65) +
            (Math.Sin((time * 1.63) + (agent.NoiseOffset * 1.7)) * 0.35);

        return tangent * signal * agent.WanderStrength * 0.22;
    }

    private static Vector3d CalculateFinalApproachPull(CrowdAgentState agent, CrowdExit targetExit)
    {
        Vector3d toExit = targetExit.Location - agent.Position;
        if (!toExit.Unitize())
        {
            return Vector3d.Zero;
        }

        return toExit;
    }

    private static bool TryAbsorbAtExit(CrowdAgentState agent, CrowdExit targetExit, double finishTime)
    {
        double distance = agent.Position.DistanceTo(targetExit.Location);
        double absorptionDistance = Math.Max(
            Math.Max(targetExit.Radius * ExitAbsorptionDistanceFactor, agent.ArrivalThreshold * ExitAbsorptionDistanceFactor),
            agent.Radius * 2.0);

        if (distance > absorptionDistance)
        {
            return false;
        }

        double speed = agent.Velocity.Length;
        Vector3d toExit = targetExit.Location - agent.Position;
        Vector3d heading = agent.Velocity;
        bool hasHeading = heading.Unitize();
        bool hasTarget = toExit.Unitize();
        double alignment = hasHeading && hasTarget ? Vector3d.Multiply(heading, toExit) : 1.0;
        bool isNearExit = distance <= Math.Max(targetExit.Radius * 1.15, agent.ArrivalThreshold * 1.4);
        bool isSlowEnough = speed <= ExitSnapSpeedThreshold;
        bool isOrbiting = distance <= Math.Max(targetExit.Radius * ExitSpiralDetectionDistanceFactor, agent.ArrivalThreshold * 3.0)
            && alignment < 0.15;

        if (isNearExit || isSlowEnough || isOrbiting)
        {
            agent.Position = targetExit.Location;
            agent.Velocity = Vector3d.Zero;
            agent.DesiredVelocity = Vector3d.Zero;
            agent.FinishTime = finishTime;
            return true;
        }

        return false;
    }

    private static Vector3d ApplyExitApproachDamping(CrowdAgentState agent, CrowdExit targetExit, Vector3d desiredVelocity)
    {
        if (desiredVelocity.Length <= 1e-6)
        {
            return desiredVelocity;
        }

        double approachDistance = Math.Max(
            Math.Max(targetExit.Radius * ExitAbsorptionDistanceFactor, agent.ArrivalThreshold * ExitAbsorptionDistanceFactor),
            agent.Radius * 3.0);
        double distance = agent.Position.DistanceTo(targetExit.Location);
        if (distance >= approachDistance)
        {
            return desiredVelocity;
        }

        double damping = RemapClamped(distance, 0.0, approachDistance, ExitApproachDampingFactor, 1.0);
        return desiredVelocity * damping;
    }

    private static void AppendTrajectoryPoint(Dictionary<int, List<Point3d>> trajectories, int agentId, Point3d point)
    {
        if (!trajectories.TryGetValue(agentId, out List<Point3d>? path))
        {
            path = new List<Point3d>();
            trajectories[agentId] = path;
        }

        if (path.Count == 0 || path[path.Count - 1].DistanceTo(point) > 1e-6)
        {
            path.Add(point);
        }
    }

    private static Vector3d ApplyTurnSmoothing(CrowdAgentState agent, Vector3d targetDirection, double turnAnticipation)
    {
        if (targetDirection.Length <= 1e-6)
        {
            return Vector3d.Zero;
        }

        Vector3d currentHeading = agent.Velocity;
        if (!currentHeading.Unitize())
        {
            return targetDirection;
        }

        double inertia = RemapClamped(turnAnticipation * agent.CurvaturePreference, 0.0, 2.8, 0.18, 0.68);
        Vector3d smoothed = (currentHeading * inertia) + (targetDirection * (1.0 - inertia));
        if (!smoothed.Unitize())
        {
            return targetDirection;
        }

        return smoothed;
    }

    private static Vector3d SampleContinuousFlowDirection(Point3d position, CrowdGrid grid, double[,] field)
    {
        double step = Math.Max(grid.Floor.CellSize * 0.6, 0.25);
        double fx1 = SampleFieldValue(new Point3d(position.X + step, position.Y, position.Z), grid, field);
        double fx0 = SampleFieldValue(new Point3d(position.X - step, position.Y, position.Z), grid, field);
        double fy1 = SampleFieldValue(new Point3d(position.X, position.Y + step, position.Z), grid, field);
        double fy0 = SampleFieldValue(new Point3d(position.X, position.Y - step, position.Z), grid, field);

        if (double.IsInfinity(fx1) || double.IsInfinity(fx0) || double.IsInfinity(fy1) || double.IsInfinity(fy0))
        {
            return Vector3d.Zero;
        }

        Vector3d direction = new(
            fx0 - fx1,
            fy0 - fy1,
            0.0);

        if (!direction.Unitize())
        {
            return Vector3d.Zero;
        }

        return direction;
    }

    private static double SampleFieldValue(Point3d point, CrowdGrid grid, double[,] field)
    {
        double continuousX = ((point.X - grid.MinX) / grid.Floor.CellSize) - 0.5;
        double continuousY = ((point.Y - grid.MinY) / grid.Floor.CellSize) - 0.5;

        int x0 = Math.Max(0, Math.Min(grid.Width - 1, (int)Math.Floor(continuousX)));
        int y0 = Math.Max(0, Math.Min(grid.Height - 1, (int)Math.Floor(continuousY)));
        int x1 = Math.Max(0, Math.Min(grid.Width - 1, x0 + 1));
        int y1 = Math.Max(0, Math.Min(grid.Height - 1, y0 + 1));

        double tx = Math.Max(0.0, Math.Min(1.0, continuousX - x0));
        double ty = Math.Max(0.0, Math.Min(1.0, continuousY - y0));

        double v00 = ResolveFiniteFieldValue(field, grid, x0, y0);
        double v10 = ResolveFiniteFieldValue(field, grid, x1, y0);
        double v01 = ResolveFiniteFieldValue(field, grid, x0, y1);
        double v11 = ResolveFiniteFieldValue(field, grid, x1, y1);

        double vx0 = (v00 * (1.0 - tx)) + (v10 * tx);
        double vx1 = (v01 * (1.0 - tx)) + (v11 * tx);
        return (vx0 * (1.0 - ty)) + (vx1 * ty);
    }

    private static double ResolveFiniteFieldValue(double[,] field, CrowdGrid grid, int x, int y)
    {
        if (!double.IsInfinity(field[x, y]))
        {
            return field[x, y];
        }

        Point3d center = grid.GetCellCenter(x, y);
        if (grid.TryGetClosestWalkableCell(center, out int nx, out int ny))
        {
            return field[nx, ny];
        }

        return double.PositiveInfinity;
    }

    private static Vector3d CombineDirections(Vector3d primary, Vector3d secondary, double primaryWeight)
    {
        if (primary.Length <= 1e-6)
        {
            return secondary;
        }

        if (secondary.Length <= 1e-6)
        {
            return primary;
        }

        Vector3d combined = (primary * primaryWeight) + (secondary * (1.0 - primaryWeight));
        if (!combined.Unitize())
        {
            return primary;
        }

        return combined;
    }

    private static double GetFinalApproachFactor(CrowdAgentState agent, CrowdExit targetExit)
    {
        double approachDistance = Math.Max(targetExit.Radius * FinalApproachDistanceFactor, agent.ArrivalThreshold * FinalApproachDistanceFactor);
        double distance = agent.Position.DistanceTo(targetExit.Location);
        return 1.0 - Math.Max(0.0, Math.Min(1.0, distance / Math.Max(approachDistance, 1e-6)));
    }

    private static double GetRouteFocusFactor(CrowdAgentState agent, double time)
    {
        double age = GetEffectiveAge(agent, time);
        return Math.Max(0.0, Math.Min(1.0, age / Math.Max(agent.FocusDelay, 0.1)));
    }

    private static double GetEffectiveAge(CrowdAgentState agent, double time)
    {
        return Math.Max(0.0, time - agent.SpawnTime);
    }

    private static double EstimateMaximumSimulationDuration(CrowdModel model)
    {
        BoundingBox bbox = model.Floor.Boundary.GetBoundingBox(true);
        double diagonal = bbox.Diagonal.Length;
        double minPreferredSpeed = Math.Max(0.25, model.AgentProfile.PreferredSpeed * 0.55);
        double latestSpawnEstimate = model.Sources
            .Select(source => source.StartTime + (source.TotalAgents / Math.Max(source.SpawnRate, 0.01)))
            .DefaultIfEmpty(0.0)
            .Max();

        double travelAllowance = (diagonal / minPreferredSpeed) * 4.0;
        return latestSpawnEstimate + travelAllowance + 10.0;
    }

    private static Vector3d EstimateFieldDirection(CrowdGrid grid, double[,] field, int x, int y)
    {
        double currentValue = field[x, y];
        if (double.IsInfinity(currentValue))
        {
            return Vector3d.Zero;
        }

        Vector3d direction = Vector3d.Zero;
        for (int ox = -1; ox <= 1; ox++)
        {
            for (int oy = -1; oy <= 1; oy++)
            {
                if (ox == 0 && oy == 0)
                {
                    continue;
                }

                int nx = x + ox;
                int ny = y + oy;
                if (!grid.IsWalkable(nx, ny))
                {
                    continue;
                }

                double neighborValue = field[nx, ny];
                if (double.IsInfinity(neighborValue))
                {
                    continue;
                }

                double improvement = currentValue - neighborValue;
                if (improvement <= 0.0)
                {
                    continue;
                }

                Vector3d step = grid.GetCellCenter(nx, ny) - grid.GetCellCenter(x, y);
                if (!step.Unitize())
                {
                    continue;
                }

                direction += step * improvement;
            }
        }

        if (!direction.Unitize())
        {
            return Vector3d.Zero;
        }

        return direction;
    }

    private static Point3d LerpPoint(Point3d a, Point3d b, double t)
    {
        t = Math.Max(0.0, Math.Min(1.0, t));
        return new Point3d(
            a.X + ((b.X - a.X) * t),
            a.Y + ((b.Y - a.Y) * t),
            a.Z + ((b.Z - a.Z) * t));
    }

    private static double Lerp(double a, double b, double t)
    {
        t = Math.Max(0.0, Math.Min(1.0, t));
        return a + ((b - a) * t);
    }

    private static double SampleAround(double value, double variationPercent, Random random, double minValue)
    {
        double factor = 1.0 + ((random.NextDouble() * 2.0 - 1.0) * variationPercent);
        return Math.Max(minValue, value * factor);
    }

    private static double SampleBehavior(Random random, double low, double medium, double high)
    {
        double roll = random.NextDouble();
        if (roll < 0.18)
        {
            return high;
        }

        if (roll < 0.62)
        {
            return medium;
        }

        return low;
    }

    private static bool IsOccupied(Point3d point, IEnumerable<CrowdAgentState> agents, double minDistance)
    {
        foreach (CrowdAgentState agent in agents.Where(agent => !agent.IsFinished))
        {
            if (agent.Position.DistanceTo(point) < minDistance)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOccupiedByOthers(CrowdAgentState current, Point3d point, IEnumerable<CrowdAgentState> agents, double minDistance)
    {
        foreach (CrowdAgentState agent in agents)
        {
            if (agent.Id == current.Id || agent.IsFinished)
            {
                continue;
            }

            if (agent.Position.DistanceTo(point) < minDistance)
            {
                return true;
            }
        }

        return false;
    }

    private static void RecordFrame(List<CrowdFrame> frames, IEnumerable<CrowdAgentState> agents, double time)
    {
        List<CrowdAgentState> snapshot = agents.ToList();
        List<Point3d> activePositions = snapshot
            .Where(agent => !agent.IsFinished)
            .Select(agent => agent.Position)
            .ToList();
        List<double> activeSpeeds = snapshot
            .Where(agent => !agent.IsFinished)
            .Select(agent => agent.Velocity.Length)
            .ToList();

        frames.Add(new CrowdFrame(
            time,
            activePositions,
            activeSpeeds,
            activeCount: activePositions.Count,
            finishedCount: snapshot.Count(agent => agent.IsFinished)));
    }

    private static Vector3d BlendVelocity(Vector3d currentVelocity, Vector3d desiredVelocity, double maxSpeed, double keepFactor)
    {
        Vector3d blended = (currentVelocity * keepFactor) + (desiredVelocity * (1.0 - keepFactor));
        double length = blended.Length;
        if (length <= 1e-6)
        {
            return desiredVelocity;
        }

        if (length > maxSpeed)
        {
            blended *= maxSpeed / length;
        }

        return blended;
    }

    private static Vector3d CalculateTimeToCollisionAvoidance(CrowdAgentState agent, CrowdAgentState other)
    {
        Vector3d relativePosition = other.Position - agent.Position;
        Vector3d relativeVelocity = other.Velocity - agent.Velocity;
        double relativeSpeedSquared = relativeVelocity.SquareLength;
        if (relativeSpeedSquared <= 1e-6)
        {
            return Vector3d.Zero;
        }

        double timeToClosest = -Vector3d.Multiply(relativePosition, relativeVelocity) / relativeSpeedSquared;
        if (timeToClosest <= 0.0 || timeToClosest > TimeToCollisionHorizon)
        {
            return Vector3d.Zero;
        }

        Point3d futureSelf = agent.Position + (agent.Velocity * timeToClosest);
        Point3d futureOther = other.Position + (other.Velocity * timeToClosest);
        Vector3d avoidDirection = futureSelf - futureOther;
        double combinedRadius = agent.Radius + other.Radius;
        double futureDistance = avoidDirection.Length;
        if (futureDistance >= combinedRadius * 1.5 || futureDistance <= 1e-6)
        {
            return Vector3d.Zero;
        }

        avoidDirection.Unitize();
        double urgency = 1.0 - (timeToClosest / TimeToCollisionHorizon);
        double overlapRisk = 1.0 - (futureDistance / (combinedRadius * 1.5));
        return avoidDirection * (urgency * overlapRisk * TimeToCollisionWeight);
    }

    private static double RemapClamped(double value, double fromMin, double fromMax, double toMin, double toMax)
    {
        if (fromMax - fromMin <= 1e-9)
        {
            return toMin;
        }

        double t = (value - fromMin) / (fromMax - fromMin);
        t = Math.Max(0.0, Math.Min(1.0, t));
        return toMin + ((toMax - toMin) * t);
    }
}
