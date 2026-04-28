using Crowd.Models;
using Crowd.Utilities;
using Rhino.Geometry;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Crowd.Services;

public static class CrowdSimulationService
{
    // --- Integration ---
    // MaxInternalTimeStep: sub-step cap keeps numeric integration stable at high speeds
    // StalledMoveFactor: stalled agents move at 50% speed to keep them from full stop
    // VelocityBlendFactor/DesiredVelocityBlendFactor: exponential smoothing — higher = more inertia
    private const double MaxInternalTimeStep = 0.2;
    private const double StalledMoveFactor = 0.5;
    private const double VelocityBlendFactor = 0.88;
    private const double DesiredVelocityBlendFactor = 0.94;

    // --- Steering noise ---
    // NoiseFrequency: spatial frequency of per-agent Perlin-like noise in path field space
    // StartScatterDuration: agents use high randomness for 1.8–4.2s after spawn to disperse from source
    private const double NoiseFrequency = 0.65;
    private const double StartScatterDurationMin = 1.8;
    private const double StartScatterDurationMax = 4.2;

    // --- Local candidate scoring weights ---
    // Relative weights of each scoring term in SelectPreferredTargetPoint.
    // Higher weight = that term dominates candidate selection.
    // FieldWeight drives global pathfinding; HeadingWeight preserves momentum;
    // DensityWeight spreads agents; ClearanceWeight keeps agents from walls in constrained zones.
    private const double CandidateFieldWeight = 1.9;
    private const double CandidateHeadingWeight = 1.1;
    private const double CandidateDensityWeight = 1.5;
    private const double CandidateClearanceWeight = 1.3;
    // Tuned 2026-04-28: raised from 1.45 — must outweigh fieldScore+headingScore combined
    // when agent is already moving along a wall; lower values cause persistent wall hugging
    private const double CandidateWidthWeight = 1.85;
    // FlowSpacingWeight: rewards candidates that maintain gap to leading agent (CSM headway)
    // BottleneckWeight/ApexPenaltyWeight/PocketTrapWeight: penalise candidates that enter traps
    // LaneCommitmentWeight: low — only mild bias toward continuing current lane
    private const double CandidateFlowSpacingWeight = 1.65;
    private const double CandidateBottleneckWeight = 1.7;
    private const double CandidateApexPenaltyWeight = 1.6;
    private const double CandidatePocketTrapWeight = 1.65;
    private const double CandidateLaneCommitmentWeight = 0.55;
    // Tuned 2026-04-28: raised from 0.11 — per-agent NoiseOffset creates route-family
    // diversity; too low collapses all agents from one source onto one dominant path
    private const double CandidateRandomnessWeight = 0.20;
    // ForwardClearanceWeight: look-ahead space in travel direction — prevents stepping into dead-ends
    // StreamPenaltyWeight: discourages joining aligned streams of agents (reduces channelisation)
    // ProgressWeight: rewards candidates that reduce field value (move toward exit)
    // TargetAlignmentWeight: active only in final approach / target zone — pulls toward exit directly
    // LowProgressPenaltyWeight: penalises lateral or backward steps
    // RecirculationPenaltyWeight: suppresses re-entry into already-visited constrained zones
    // TurnWeight: rewards candidates consistent with upcoming path field curvature
    private const double CandidateForwardClearanceWeight = 1.35;
    private const double CandidateStreamPenaltyWeight = 1.45;
    private const double CandidateProgressWeight = 1.15;
    private const double CandidateTargetAlignmentWeight = 1.4;
    private const double CandidateLowProgressPenaltyWeight = 0.95;
    private const double CandidateRecirculationPenaltyWeight = 1.35;
    private const double CandidateTurnWeight = 1.15;
    // Tuned 2026-04-28: raised from 0.38 — controls softmax spread around best candidate;
    // lower values collapse route families to single narrow channel in open space;
    // further reduced in constrained zones by BottleneckBlendTemperatureFactor / TargetZoneBlendTemperatureFactor
    private const double CandidateBlendTemperature = 0.48;
    // CurvaturePenaltyWeight: penalises sharp turns relative to current heading
    private const double CurvaturePenaltyWeight = 1.1;

    // --- Wall / avoidance ---
    // WallInfluenceFactor: multiplier on agent radius to define wall repulsion activation radius
    // WallRepulsionWeight: strength of boundary repulsion force applied each step
    // WallFollowWeight: intentionally low (0.08) — only mild wall-tangent alignment, not sticky following
    // WallClearancePreviewFactor: look-ahead distance for clearance scoring, in multiples of desiredClearance
    // CorridorVisibility*: weights for the corridor-visibility scoring sub-function
    // BottleneckPreviewStep*: 2-step lookahead at 1.1× cell size for bottleneck penalty
    private const double WallInfluenceFactor = 2.4;
    private const double WallRepulsionWeight = 2.05;
    private const double WallFollowWeight = 0.08;
    private const double WallClearancePreviewFactor = 1.25;
    private const double CorridorVisibilityTurnPenalty = 0.32;
    private const double CorridorVisibilityClearanceWeight = 0.22;
    private const double CorridorVisibilityProgressWeight = 1.0;
    private const double BottleneckPreviewStepFactor = 1.1;
    private const int BottleneckPreviewSteps = 2;

    // --- Neighbor separation / TTC ---
    // TimeToCollisionWeight: strength of TTC-based avoidance force toward other agents
    // AlignedNeighborSpreadWeight: mild lateral push to spread agents walking in the same direction
    private const double TimeToCollisionWeight = 1.8;
    private const double AlignedNeighborSpreadWeight = 0.42;

    // --- Behavioral mode: final approach ---
    // Activated when agent is within DistanceFactor×radius of exit. Reduces noise/wander,
    // adds direct pull toward exit, damps separation to allow tight queuing.
    private const double FinalApproachDistanceFactor = 4.5;
    private const double FinalApproachSeparationFactor = 0.4;
    private const double FinalApproachNoiseFactor = 0.15;
    private const double FinalApproachWanderFactor = 0.14;
    private const double FinalApproachLaneBiasFactor = 0.2;
    private const double FinalApproachPullWeight = 1.35;

    // --- Behavioral mode: target zone ---
    // Very low noise/wander — agent is inside exit radius, absorbing. High wall follow
    // factor keeps agent from bouncing; BlendTemperatureFactor reduces candidate spread.
    private const double TargetZoneNoiseFactor = 0.05;
    private const double TargetZoneWanderFactor = 0.07;
    private const double TargetZoneLaneBiasFactor = 0.1;
    private const double TargetZoneFlowFollowFactor = 0.38;
    private const double TargetZoneWallFollowFactor = 0.52;
    private const double TargetZoneBlendTemperatureFactor = 0.58;
    private const double TargetZoneCommitFactor = 0.22;
    private const double TargetZoneStabilityDistanceFactor = 6.0;

    // --- Behavioral mode: high-clarity open flow ---
    // Open space with clear gradient — higher noise/wander to spread route families
    private const double HighClarityNoiseFactor = 0.5;
    private const double HighClarityWanderFactor = 0.38;

    // --- Behavioral mode: bottleneck ---
    // Constrained passage — higher flow follow / commit, reduced randomness, slight
    // separation boost to prevent deadlock compression
    private const double BottleneckNoiseFactor = 0.42;
    private const double BottleneckWanderFactor = 0.28;
    private const double BottleneckSeparationBoost = 1.28;
    private const double BottleneckFlowFollowFactor = 0.7;
    private const double BottleneckBlendTemperatureFactor = 0.82;
    private const double BottleneckCommitFactor = 0.42;
    private const double BottleneckStabilizationWeight = 0.82;

    // --- Speed and conflict ---
    // CollisionFreeSpeedBlend: mix between desired and conflict-resolved speed
    // FlowFollowWeight: mild flow-matching force (keeps agents from fighting stream direction)
    // EscapeWeight: force applied when agent is in collision state
    // ConflictDamping*: noise/wander injection during active conflict to break symmetry
    private const double CollisionFreeSpeedBlend = 0.58;
    private const double FlowFollowWeight = 0.08;
    private const double EscapeWeight = 0.28;
    private const double ConflictDampingNoiseFactor = 0.16;
    private const double ConflictDampingWanderFactor = 0.1;

    // --- Exit absorption ---
    // AbsorptionDistanceFactor: exit captures agent when within Factor×exitRadius
    // SnapSpeedThreshold: below this speed agent snaps straight to exit center
    // ApproachDampingFactor: speed multiplier during final absorption glide
    // SpiralDetectionDistanceFactor: radius for detecting agents orbiting exit
    // ClosingSpeedFactor: speed correction for agents approaching exit tangentially
    private const double ExitAbsorptionDistanceFactor = 2.8;
    private const double ExitSnapSpeedThreshold = 0.35;
    private const double ExitApproachDampingFactor = 0.32;
    private const double ExitSpiralDetectionDistanceFactor = 4.0;
    private const double ExitClosingSpeedFactor = 0.42;

    // --- Spawn diffusion ---
    // Agents near source use reduced FieldFactor and increased random/side bias to
    // disperse from spawn point before committing to a route
    private const double EntranceDiffusionFieldFactor = 0.52;
    private const double EntranceDiffusionRandomFactor = 2.4;
    private const double EntranceDiffusionSideFactor = 1.9;

    // --- Stuck / deadlock ---
    // StuckProgressTolerance: displacement below this per step counts as stuck
    // StuckActivationTime: seconds before stuck recovery kicks in
    // FieldRegression*: recovery teleport target — moves agent back along field gradient
    // ConstrainedWinnerCollapseGap: score gap above which winner-collapse softmax is applied
    private const double StuckProgressTolerance = 0.02;
    private const double StuckActivationTime = 1.35;
    private const double FieldRegressionCellFactor = 0.35;
    private const double FieldRegressionStepFactor = 0.45;
    private const double ConstrainedWinnerCollapseGap = 0.24;

    // --- Exit choice utility ---
    // Softmax over exits scored by distance, congestion, queue length, and current progress.
    // SwitchThreshold: minimum utility gain required to switch away from current exit
    // AwarenessRadius/QueueRadius: spatial query radii for congestion and queue estimation
    private const double ExitDistanceWeight = 0.68;
    private const double ExitCongestionWeight = 1.45;
    private const double ExitQueueWeight = 1.2;
    private const double ExitProgressWeight = 0.32;
    private const double ExitSwitchThreshold = 0.05;
    private const double ExitAwarenessRadius = 4.5;
    private const double ExitQueueRadius = 6.0;
    private const double ExitSoftmaxTemperature = 0.42;

    // --- Simulation termination ---
    // Grace/MinimumStuck: seconds after last completion before stall detection triggers
    // AverageSpeedThreshold: m/s below which all remaining agents are considered stuck
    // MaxActiveShare: if fewer than this fraction remain active, declare stalled tail
    // LongRunActive/DurationShare: fallback cutoff — fire if tiny tail exceeds 65% of max duration
    private const double StalledTailCompletionGraceSeconds = 90.0;
    private const double StalledTailMinimumStuckSeconds = 12.0;
    private const double StalledTailAverageSpeedThreshold = 0.05;
    private const double StalledTailMaxActiveShare = 0.75;
    private const double StalledTailLongRunActiveShare = 0.5;
    private const double StalledTailLongRunDurationShare = 0.65;

    /// <summary>
    /// Simulates pedestrians on a 2D walkable floor using a grid-derived distance field and lightweight local avoidance.
    /// The solver is intentionally compact: it targets architectural flow studies rather than high-fidelity behavioral research.
    /// </summary>
    /// <param name="model">Crowd model assembled from floor, obstacles, sources, exits, and agent profile.</param>
    /// <returns>Recorded frames, agent paths, and summary counts for downstream Grasshopper visualization.</returns>
    public static CrowdSimulationResult Run(CrowdModel model)
    {
        Stopwatch totalStopwatch = Stopwatch.StartNew();
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        Stopwatch stageStopwatch = Stopwatch.StartNew();
        CrowdGrid grid = new(model.Floor, model.Obstacles);
        double gridBuildMilliseconds = stageStopwatch.Elapsed.TotalMilliseconds;

        stageStopwatch.Restart();
        List<double[,]> exitFields = BuildExitFields(grid, model.Exits);
        double pathFieldBuildMilliseconds = stageStopwatch.Elapsed.TotalMilliseconds;

        List<CrowdAgentState> agents = new();
        Dictionary<int, List<Point3d>> trajectories = new();
        List<CrowdFrame> frames = new();
        double[] sourceRemainders = new double[model.Sources.Count];
        int[] sourceSpawned = new int[model.Sources.Count];
        int nextAgentId = 1;
        double time = 0.0;
        Random random = new(12345);
        int totalExpectedAgents = model.Sources.Sum(source => source.TotalAgents);
        int totalSpawned = 0;
        int totalCompleted = 0;
        int activeAgentCount = 0;
        double lastCompletionTime = 0.0;
        string terminationReason = "maximum simulation duration reached";
        double maxSimulationDuration = EstimateMaximumSimulationDuration(model);

        RecordFrame(frames, agents, time, out activeAgentCount, out totalCompleted);

        stageStopwatch.Restart();
        while (time < maxSimulationDuration)
        {
            totalSpawned += SpawnAgents(model, grid, exitFields, agents, trajectories, sourceRemainders, sourceSpawned, ref nextAgentId, time, random);
            AdvanceAgents(model, grid, exitFields, agents, trajectories, time, random);

            time += model.TimeStep;
            int previousCompleted = totalCompleted;
            RecordFrame(frames, agents, time, out activeAgentCount, out totalCompleted);
            if (totalCompleted > previousCompleted)
            {
                lastCompletionTime = time;
            }

            if (totalSpawned >= totalExpectedAgents && activeAgentCount == 0)
            {
                terminationReason = "all agents completed";
                break;
            }

            if (totalSpawned >= totalExpectedAgents
                && activeAgentCount > 0
                && time - lastCompletionTime >= StalledTailCompletionGraceSeconds
                && IsStalledTail(agents, totalSpawned, totalCompleted, activeAgentCount))
            {
                terminationReason = "stalled active tail";
                break;
            }

            if (totalSpawned >= totalExpectedAgents
                && activeAgentCount > 0
                && totalCompleted > 0
                && activeAgentCount / Math.Max(1.0, totalSpawned) <= StalledTailLongRunActiveShare
                && time >= maxSimulationDuration * StalledTailLongRunDurationShare)
            {
                terminationReason = "long-running active tail";
                break;
            }
        }
        double simulationLoopMilliseconds = stageStopwatch.Elapsed.TotalMilliseconds;

        stageStopwatch.Restart();
        List<CrowdAgentPath> paths = BuildAgentPaths(agents, trajectories);

        CrowdSimulationCoreMetrics coreMetrics = CrowdSimulationMetricsService.BuildCoreMetrics(model, paths, time);
        double resultBuildMilliseconds = stageStopwatch.Elapsed.TotalMilliseconds;
        totalStopwatch.Stop();
        CrowdSimulationProfile profile = new(
            gridBuildMilliseconds,
            pathFieldBuildMilliseconds,
            simulationLoopMilliseconds,
            resultBuildMilliseconds,
            totalStopwatch.Elapsed.TotalMilliseconds,
            grid.Width,
            grid.Height,
            model.Exits.Count,
            frames.Count,
            totalSpawned,
            totalCompleted,
            activeAgentCount,
            terminationReason,
            BuildActiveTailSummary(agents, grid, exitFields),
            Math.Max(0.0, time - lastCompletionTime),
            maxSimulationDuration,
            time);

        return new CrowdSimulationResult(
            model,
            frames,
            paths,
            coreMetrics,
            profile,
            totalSpawned: totalSpawned,
            totalFinished: totalCompleted,
            simulatedDuration: time,
            completedAllAgents: totalSpawned == totalExpectedAgents && activeAgentCount == 0);
    }

    private static int SpawnAgents(
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
        int spawnedCount = 0;
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

            int spawnedFromSource = 0;
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

                CrowdAgentState agent = CreateAgent(nextAgentId++, spawnPoint, source, grid, model.Exits, exitFields, agents, time, model.AgentProfile, random);
                agents.Add(agent);
                trajectories[agent.Id] = new List<Point3d> { spawnPoint };
                sourceSpawned[sourceIndex]++;
                spawnedCount++;
                spawnedFromSource++;
            }

            sourceRemainders[sourceIndex] -= spawnedFromSource;
        }

        return spawnedCount;
    }

    private static void AdvanceAgents(
        CrowdModel model,
        CrowdGrid grid,
        IReadOnlyList<double[,]> exitFields,
        List<CrowdAgentState> agents,
        Dictionary<int, List<Point3d>> trajectories,
        double time,
        Random random)
    {
        int substeps = Math.Max(1, (int)Math.Ceiling(model.TimeStep / MaxInternalTimeStep));
        double substepTime = model.TimeStep / substeps;
        for (int stepIndex = 0; stepIndex < substeps; stepIndex++)
        {
            double stepTime = time + (substepTime * stepIndex);
            UpdateAgents(model, grid, exitFields, agents, trajectories, stepTime, substepTime, random);
        }
    }

    private static void UpdateAgents(
        CrowdModel model,
        CrowdGrid grid,
        IReadOnlyList<double[,]> exitFields,
        List<CrowdAgentState> agents,
        Dictionary<int, List<Point3d>> trajectories,
        double time,
        double timeStep,
        Random random)
    {
        List<CrowdAgentState> activeAgents = agents.Where(agent => !agent.IsFinished).ToList();
        if (activeAgents.Count == 0)
        {
            return;
        }

        AgentSpatialIndex spatialIndex = new(grid, activeAgents);
        List<PendingAgentUpdate> pendingUpdates = PreparePendingAgentUpdates(
            model,
            grid,
            exitFields,
            activeAgents,
            spatialIndex,
            trajectories,
            time,
            random);
        if (pendingUpdates.Count == 0)
        {
            return;
        }

        AgentMotionPlan[] motionPlans = BuildAgentMotionPlans(model, grid, activeAgents, spatialIndex, pendingUpdates, time, timeStep);
        ApplyAgentMotionPlans(grid, activeAgents, spatialIndex, trajectories, motionPlans, time, timeStep);
    }

    private static List<PendingAgentUpdate> PreparePendingAgentUpdates(
        CrowdModel model,
        CrowdGrid grid,
        IReadOnlyList<double[,]> exitFields,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex,
        Dictionary<int, List<Point3d>> trajectories,
        double time,
        Random random)
    {
        List<PendingAgentUpdate> pendingUpdates = new(activeAgents.Count);
        foreach (CrowdAgentState agent in activeAgents)
        {
            MaybeReevaluateExit(agent, model, grid, exitFields, activeAgents, spatialIndex, time, random);
            CrowdExit targetExit = model.Exits[agent.ExitIndex];
            if (TryAbsorbAtExit(agent, targetExit, time))
            {
                AppendTrajectoryPoint(trajectories, agent.Id, agent.Position);
                continue;
            }

            pendingUpdates.Add(new PendingAgentUpdate(agent, targetExit, exitFields[agent.ExitIndex]));
        }

        return pendingUpdates;
    }

    private static AgentMotionPlan[] BuildAgentMotionPlans(
        CrowdModel model,
        CrowdGrid grid,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex,
        IReadOnlyList<PendingAgentUpdate> pendingUpdates,
        double time,
        double timeStep)
    {
        AgentMotionPlan[] motionPlans = new AgentMotionPlan[pendingUpdates.Count];
        Parallel.For(0, pendingUpdates.Count, updateIndex =>
        {
            PendingAgentUpdate pendingUpdate = pendingUpdates[updateIndex];
            CrowdAgentState agent = pendingUpdate.Agent;
            Vector3d desiredVelocity = CalculateDesiredVelocity(
                agent,
                model,
                grid,
                pendingUpdate.TargetExit,
                pendingUpdate.Field,
                activeAgents,
                spatialIndex,
                time);
            desiredVelocity = ApplyExitApproachDamping(agent, pendingUpdate.TargetExit, desiredVelocity);
            Vector3d blendedDesiredVelocity = BlendVelocity(agent.DesiredVelocity, desiredVelocity, agent.MaxSpeed, DesiredVelocityBlendFactor);
            Vector3d motionVelocity = ComputeMotionVelocity(agent, grid, pendingUpdate.Field, blendedDesiredVelocity, timeStep);
            motionPlans[updateIndex] = new AgentMotionPlan(agent, pendingUpdate.TargetExit, pendingUpdate.Field, blendedDesiredVelocity, motionVelocity);
        });

        return motionPlans;
    }

    private static void ApplyAgentMotionPlans(
        CrowdGrid grid,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex,
        Dictionary<int, List<Point3d>> trajectories,
        IReadOnlyList<AgentMotionPlan> motionPlans,
        double time,
        double timeStep)
    {
        foreach (AgentMotionPlan motionPlan in motionPlans)
        {
            CrowdAgentState agent = motionPlan.Agent;
            agent.DesiredVelocity = motionPlan.DesiredVelocity;

            Point3d proposed = agent.Position + (motionPlan.MotionVelocity * timeStep);
            if (!grid.IsWalkable(proposed) || IsOccupiedByOthers(agent, proposed, activeAgents, spatialIndex, agent.Radius * 1.5))
            {
                proposed = agent.Position + (motionPlan.MotionVelocity * (timeStep * StalledMoveFactor));
                if (!grid.IsWalkable(proposed) || IsOccupiedByOthers(agent, proposed, activeAgents, spatialIndex, agent.Radius * 1.35))
                {
                    proposed = agent.Position;
                }
            }

            proposed = StabilizeProposedMove(agent, timeStep, grid, motionPlan.Field, activeAgents, spatialIndex, proposed);
            if (agent.StuckDuration >= StuckActivationTime && proposed.DistanceTo(agent.Position) <= 1e-6)
            {
                proposed = CreateDeadlockReleaseMove(agent, timeStep, grid, motionPlan.Field, activeAgents, spatialIndex, motionPlan.MotionVelocity);
            }

            agent.Velocity = (proposed - agent.Position) / Math.Max(timeStep, 1e-6);
            agent.Position = proposed;
            UpdateAgentProgressState(agent, grid, motionPlan.Field, timeStep);

            AppendTrajectoryPoint(trajectories, agent.Id, proposed);

            if (TryAbsorbAtExit(agent, motionPlan.TargetExit, time + timeStep))
            {
                AppendTrajectoryPoint(trajectories, agent.Id, agent.Position);
            }
        }
    }

    private static List<double[,]> BuildExitFields(CrowdGrid grid, IReadOnlyList<CrowdExit> exits)
    {
        double[][,] exitFields = new double[exits.Count][,];
        Parallel.For(0, exits.Count, exitIndex =>
        {
            exitFields[exitIndex] = CrowdPathFieldBuilder.Build(grid, exits[exitIndex]);
        });

        return exitFields.ToList();
    }

    private static List<CrowdAgentPath> BuildAgentPaths(
        IReadOnlyList<CrowdAgentState> agents,
        IReadOnlyDictionary<int, List<Point3d>> trajectories)
    {
        CrowdAgentState[] orderedAgents = agents.OrderBy(agent => agent.Id).ToArray();
        CrowdAgentPath[] paths = new CrowdAgentPath[orderedAgents.Length];
        Parallel.For(0, orderedAgents.Length, agentIndex =>
        {
            CrowdAgentState agent = orderedAgents[agentIndex];
            IEnumerable<Point3d> points = trajectories.TryGetValue(agent.Id, out List<Point3d>? pathPoints)
                ? pathPoints
                : Enumerable.Empty<Point3d>();
            paths[agentIndex] = new CrowdAgentPath(
                agent.Id,
                agent.ExitIndex,
                new Polyline(points),
                agent.IsFinished,
                agent.SpawnTime,
                agent.FinishTime);
        });

        return paths.ToList();
    }

    private static Vector3d CalculateDesiredVelocity(
        CrowdAgentState agent,
        CrowdModel model,
        CrowdGrid grid,
        CrowdExit targetExit,
        double[,] field,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex,
        double time)
    {
        if (!grid.TryGetClosestWalkableCell(agent.Position, out int x, out int y))
        {
            return Vector3d.Zero;
        }

        Vector3d fieldDirection = SampleContinuousFlowDirection(agent.Position, grid, field);
        Vector3d routeDirection = BuildRouteDirection(agent.Position, grid, field, x, y);
        Point3d preferredTargetPoint = SelectPreferredTargetPoint(agent, model, grid, targetExit, field, activeAgents, spatialIndex, x, y, time);
        Vector3d localDirection = preferredTargetPoint - agent.Position;
        if (!localDirection.Unitize())
        {
            localDirection = routeDirection;
        }

        double routeFocus = GetRouteFocusFactor(agent, time);
        double routeClarity = GetRouteClarity(agent.Position, grid, field, routeDirection);
        double localDensity = EstimateNeighborhoodDensity(agent.Position, activeAgents, spatialIndex, agent.Id, agent.Radius * 4.0);
        double densityFactor = RemapClamped(localDensity, 0.0, 2.5, 0.0, 1.0);
        double routeBlend = RemapClamped(routeClarity, 0.0, 1.0, 0.38, 0.82);
        routeBlend = Lerp(routeBlend, 0.56, densityFactor * 0.45);
        routeBlend = Lerp(routeBlend, 0.74, routeFocus * 0.35);

        Vector3d navigationalDirection = CombineDirections(routeDirection, localDirection, routeBlend);
        double fieldBlend = fieldDirection.Length > 1e-6 ? 0.48 : 0.68;
        Vector3d direction = CombineDirections(navigationalDirection, fieldDirection, fieldBlend);
        if (!direction.Unitize())
        {
            direction = navigationalDirection;
        }

        double finalApproach = GetFinalApproachFactor(agent, targetExit);
        double targetZoneFactor = GetTargetZoneStabilityFactor(agent, targetExit);

        Vector3d separation = Vector3d.Zero;
        Vector3d collisionAvoidance = Vector3d.Zero;
        double neighborhoodRadius = Math.Max(agent.Radius * 3.0, agent.NeighborRepulsionRange + agent.ComfortDistance + (agent.Radius * 2.5));
        Vector3d interactionHeading = agent.Velocity;
        if (!interactionHeading.Unitize())
        {
            interactionHeading = direction;
        }

        Vector3d interactionTangent = new(-interactionHeading.Y, interactionHeading.X, 0.0);
        bool hasInteractionTangent = interactionTangent.Unitize();

        foreach (CrowdAgentState other in spatialIndex.Query(agent.Position, neighborhoodRadius + agent.NeighborRepulsionRange + agent.ComfortDistance))
        {
            if (other.Id == agent.Id)
            {
                continue;
            }

            Point3d predictedSelf = agent.Position + (agent.Velocity * agent.AnticipationTime);
            Point3d predictedOther = other.Position + (other.Velocity * agent.AnticipationTime);

            Vector3d away = agent.Position - other.Position;
            double distance = away.Length;
            double predictedDistance = predictedSelf.DistanceTo(predictedOther);
            double effectiveRange = neighborhoodRadius + agent.NeighborRepulsionRange + agent.ComfortDistance;
            if (distance <= 1e-6 || distance > effectiveRange)
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

            double personalRange = Math.Max(agent.ComfortDistance + agent.Radius + other.Radius, 1e-6);
            double anticipatedCloseness = (effectiveRange - Math.Min(distance, predictedDistance)) / effectiveRange;
            double comfortPenalty = Math.Max(0.0, (personalRange - predictedDistance) / personalRange);
            double weight = Math.Max(0.0, anticipatedCloseness) * forwardFactor;
            separation += away * (weight * (agent.SeparationWeight + (comfortPenalty * agent.NeighborRepulsionStrength)) * Lerp(1.0, FinalApproachSeparationFactor, finalApproach));

            if (hasInteractionTangent)
            {
                Vector3d offset = other.Position - agent.Position;
                double longitudinal = Vector3d.Multiply(offset, interactionHeading);
                double lateral = Vector3d.Multiply(offset, interactionTangent);
                double lateralDistance = Math.Abs(lateral);
                double alignedSpacingRange = Math.Max(agent.ComfortDistance + agent.Radius + other.Radius, agent.Radius * 2.4);
                double longitudinalRange = Math.Max(agent.NeighborRepulsionRange + agent.ComfortDistance, alignedSpacingRange * 1.8);
                Vector3d otherDirection = other.Velocity;
                double alignment = 0.0;
                if (otherDirection.Unitize())
                {
                    alignment = Math.Max(0.0, Vector3d.Multiply(otherDirection, interactionHeading));
                }

                if (alignment > 0.25 && Math.Abs(longitudinal) <= longitudinalRange && lateralDistance < alignedSpacingRange)
                {
                    double spreadSign = lateral >= 0.0 ? -1.0 : 1.0;
                    if (lateralDistance <= 1e-4)
                    {
                        spreadSign = agent.SideBias >= 0.0 ? 1.0 : -1.0;
                    }

                    double lateralPressure = 1.0 - Math.Min(1.0, lateralDistance / Math.Max(alignedSpacingRange, 1e-6));
                    double queuePressure = 1.0 - Math.Min(1.0, Math.Abs(longitudinal) / Math.Max(longitudinalRange, 1e-6));
                    separation += interactionTangent * (spreadSign * lateralPressure * queuePressure * alignment * AlignedNeighborSpreadWeight);
                }
            }

            Vector3d ttcAvoidance = CalculateTimeToCollisionAvoidance(agent, other);
            if (ttcAvoidance.Length > 1e-6)
            {
                collisionAvoidance += ttcAvoidance * (forwardFactor * (0.8 + (agent.NeighborRepulsionStrength * 0.35)));
            }
        }

        Vector3d densityAvoidance = CalculateDensityAvoidance(agent, grid, field, model, activeAgents, spatialIndex);
        Vector3d wallRepulsion = CalculateWallRepulsion(agent, grid, model.AgentProfile.WallAvoidance);
        Vector3d finalApproachPull = CalculateFinalApproachPull(agent, targetExit) * (Math.Max(finalApproach, targetZoneFactor) * FinalApproachPullWeight);
        double desiredClearance = agent.Radius + agent.WallBufferDistance + (agent.ComfortDistance * 0.5);
        double bottleneckFactor = GetBottleneckRegimeFactor(agent, grid, direction, desiredClearance);
        double flowFollowFactor = GetFlowFollowRegimeFactor(agent, direction, activeAgents, spatialIndex);
        double conflictFactor = GetConflictFactor(separation, collisionAvoidance, densityAvoidance);
        double stabilityFactor = Math.Max(finalApproach, targetZoneFactor);
        double clarityAmbiguity = 1.0 - routeClarity;
        double commitFactor = Math.Max(
            targetZoneFactor,
            Math.Max(
                bottleneckFactor * 0.85,
                Math.Max(finalApproach * 0.7, conflictFactor * 0.65)));
        Vector3d wallFollow = CalculateWallFollowing(agent, grid, direction, model.AgentProfile.WallAvoidance)
            * (bottleneckFactor * BottleneckStabilizationWeight)
            * Lerp(1.0, TargetZoneWallFollowFactor, targetZoneFactor);
        flowFollowFactor *= Lerp(1.0, BottleneckFlowFollowFactor, bottleneckFactor);
        Vector3d flowFollow = CalculateFlowFollow(agent, direction, activeAgents, spatialIndex)
            * (flowFollowFactor * (FlowFollowWeight * 1.8))
            * Lerp(1.0, TargetZoneFlowFollowFactor, targetZoneFactor);
        Vector3d laneBias = CalculateLaneBias(agent, direction)
            * (1.0 - (conflictFactor * 0.55))
            * Lerp(1.0, BottleneckCommitFactor, commitFactor)
            * Lerp(1.0, FinalApproachLaneBiasFactor, finalApproach)
            * Lerp(1.0, TargetZoneLaneBiasFactor, targetZoneFactor);
        Vector3d noiseVector = CalculateNoiseVector(
            agent,
            direction,
            time,
            model.AgentProfile.SteeringNoise
            * (1.0 - (conflictFactor * ConflictDampingNoiseFactor))
            * Lerp(HighClarityNoiseFactor, 1.0, clarityAmbiguity)
            * Lerp(1.0, BottleneckCommitFactor, commitFactor)
            * Lerp(1.0, BottleneckNoiseFactor, bottleneckFactor)
            * Lerp(1.0, FinalApproachNoiseFactor, finalApproach)
            * Lerp(1.0, TargetZoneNoiseFactor, targetZoneFactor));
        Vector3d startScatter = CalculateStartScatter(agent, direction, time) * (1.0 - conflictFactor);
        Vector3d wanderVector = CalculateWanderVector(agent, direction, time)
            * (1.0 - (conflictFactor * ConflictDampingWanderFactor))
            * Lerp(HighClarityWanderFactor, 1.0, clarityAmbiguity)
            * Lerp(1.0, BottleneckCommitFactor, commitFactor)
            * Lerp(1.0, BottleneckWanderFactor, bottleneckFactor)
            * Lerp(1.0, FinalApproachWanderFactor, finalApproach)
            * Lerp(1.0, TargetZoneWanderFactor, targetZoneFactor);
        Vector3d stuckEscape = CalculateStuckEscape(agent, grid, field, activeAgents, spatialIndex);
        Vector3d combined =
            (direction * 1.28) +
            (separation * (0.65 * Lerp(1.0, BottleneckSeparationBoost, Math.Max(bottleneckFactor, stabilityFactor * 0.6)))) +
            (collisionAvoidance * 0.9) +
            (densityAvoidance * 0.28) +
            (wallRepulsion * 0.42) +
            wallFollow +
            flowFollow +
            laneBias +
            noiseVector +
            startScatter +
            wanderVector +
            stuckEscape +
            finalApproachPull;
        if (!combined.Unitize())
        {
            combined = direction;
        }

        combined = CombineDirections(direction, combined, Lerp(0.72, 0.9, commitFactor));

        if (!combined.Unitize())
        {
            return Vector3d.Zero;
        }

        Vector3d smoothedDirection = ApplyTurnSmoothing(agent, combined, model.AgentProfile.TurnAnticipation);
        smoothedDirection = ApplyDirectionalDamping(agent, direction, smoothedDirection);
        double collisionFreeSpeed = CalculateCollisionFreeSpeed(agent, grid, smoothedDirection, activeAgents, spatialIndex);
        double wallLimitedSpeed = CalculateWallLimitedSpeed(agent, grid, smoothedDirection);
        double turningLimitedSpeed = CalculateTurningLimitedSpeed(agent, direction, smoothedDirection);
        double targetSpeed = Math.Min(agent.PreferredSpeed, agent.MaxSpeed);
        targetSpeed = Lerp(targetSpeed, Math.Min(targetSpeed, collisionFreeSpeed), CollisionFreeSpeedBlend);
        targetSpeed = Math.Min(targetSpeed, wallLimitedSpeed);
        targetSpeed = Math.Min(targetSpeed, turningLimitedSpeed);
        return smoothedDirection * targetSpeed;
    }

    private static CrowdAgentState CreateAgent(
        int id,
        Point3d position,
        CrowdSource source,
        CrowdGrid grid,
        IReadOnlyList<CrowdExit> exits,
        IReadOnlyList<double[,]> exitFields,
        IReadOnlyList<CrowdAgentState> existingAgents,
        double spawnTime,
        CrowdAgentProfile profile,
        Random random)
    {
        double radius = SampleAround(profile.Radius, profile.VariationPercent, random, 0.05);
        double preferredSpeed = SampleAround(profile.PreferredSpeed, profile.VariationPercent, random, 0.1);
        double maxSpeed = Math.Max(preferredSpeed, SampleAround(profile.MaxSpeed, profile.VariationPercent, random, preferredSpeed));
        double timeGap = SampleAround(profile.TimeGap, profile.VariationPercent * 0.25, random, 0.2);
        double reactionTime = SampleAround(profile.ReactionTime, profile.VariationPercent * 0.25, random, 0.08);
        double anticipationTime = SampleAround(profile.AnticipationTime, profile.VariationPercent * 0.3, random, 0.2);
        double separationWeight = SampleAround(profile.SeparationWeight, profile.VariationPercent, random, 0.05);
        double neighborRepulsionStrength = SampleAround(profile.NeighborRepulsionStrength, profile.VariationPercent * 0.35, random, 0.05);
        double neighborRepulsionRange = SampleAround(profile.NeighborRepulsionRange, profile.VariationPercent * 0.3, random, 0.2);
        double comfortDistance = SampleAround(profile.ComfortDistance, profile.VariationPercent * 0.2, random, 0.05);
        double arrivalThreshold = SampleAround(profile.ArrivalThreshold, profile.VariationPercent * 0.5, random, 0.1);
        double noiseOffset = random.NextDouble() * Math.PI * 2.0;
        double sideBias = (random.NextDouble() * 2.0 - 1.0) * (0.45 + (profile.PreferredSideBias * 1.1));
        double routeCommitment = 0.56 + (random.NextDouble() * 0.58);
        double exitChoiceRandomness = SampleAround(profile.ExitChoiceRandomness, profile.VariationPercent * 0.5, random, 0.01);
        double congestionSensitivity = SampleAround(profile.CongestionSensitivity, profile.VariationPercent * 0.5, random, 0.05);
        double exitCommitment = RemapClamped(
            SampleAround(profile.ExitCommitment, profile.VariationPercent * 0.35, random, 0.05),
            0.0,
            1.0,
            0.0,
            1.0);
        double reassessmentInterval = SampleAround(profile.ReassessmentInterval, profile.VariationPercent * 0.35, random, 0.15);
        double wanderStrength = SampleBehavior(random, 0.22, 0.52, 0.96);
        double curvaturePreference = SampleBehavior(random, 0.78, 1.12, 1.58);
        double startScatterStrength = SampleBehavior(random, 0.45, 0.92, 1.45);
        double focusDelay = StartScatterDurationMin + (random.NextDouble() * (StartScatterDurationMax - StartScatterDurationMin));
        int exitIndex = ChooseExitIndex(
            source,
            position,
            grid,
            exits,
            exitFields,
            existingAgents,
            spatialIndex: null,
            noiseOffset,
            exitChoiceRandomness,
            congestionSensitivity,
            random);
        double initialFieldDistance = GetFieldDistanceAtPoint(position, grid, exitFields[exitIndex]);
        double laneCommitmentBias = sideBias >= 0.0 ? 1.0 : -1.0;

        return new CrowdAgentState(
            id,
            position,
            exitIndex,
            source.ExitIndex.HasValue,
            spawnTime,
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
            noiseOffset,
            sideBias,
            routeCommitment,
            exitChoiceRandomness,
            congestionSensitivity,
            exitCommitment,
            reassessmentInterval,
            profile.WallBufferDistance,
            wanderStrength,
            curvaturePreference,
            startScatterStrength,
            focusDelay,
            initialFieldDistance,
            laneCommitmentBias);
    }

    private static void MaybeReevaluateExit(
        CrowdAgentState agent,
        CrowdModel model,
        CrowdGrid grid,
        IReadOnlyList<double[,]> exitFields,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex,
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
            model.Exits,
            exitFields,
            activeAgents,
            spatialIndex,
            agent.NoiseOffset,
            agent.ExitChoiceRandomness,
            agent.CongestionSensitivity,
            random);

        agent.NextExitDecisionTime = time + agent.ReassessmentInterval;
        if (proposedExit == currentExit)
        {
            return;
        }

        double currentUtility = EvaluateExitUtility(agent, currentExit, grid, model.Exits, exitFields, activeAgents, spatialIndex);
        double proposedUtility = EvaluateExitUtility(agent, proposedExit, grid, model.Exits, exitFields, activeAgents, spatialIndex);
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
        IReadOnlyList<CrowdExit> exits,
        IReadOnlyList<double[,]> exitFields,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex? spatialIndex,
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
            double utility = EvaluateExitUtility(position, i, grid, exits, exitFields, activeAgents, spatialIndex, noiseOffset, congestionSensitivity, 1.2, 1.0);
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
        CrowdAgentState agent,
        int exitIndex,
        CrowdGrid grid,
        IReadOnlyList<CrowdExit> exits,
        IReadOnlyList<double[,]> exitFields,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex)
    {
        return EvaluateExitUtility(
            agent.Position,
            exitIndex,
            grid,
            exits,
            exitFields,
            activeAgents,
            spatialIndex,
            agent.NoiseOffset,
            agent.CongestionSensitivity,
            agent.PreferredSpeed,
            agent.TimeGap);
    }

    private static double EvaluateExitUtility(
        Point3d position,
        int exitIndex,
        CrowdGrid grid,
        IReadOnlyList<CrowdExit> exits,
        IReadOnlyList<double[,]> exitFields,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex? spatialIndex,
        double noiseOffset,
        double congestionSensitivity,
        double preferredSpeed,
        double timeGap)
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

        double travelDistance = distance * grid.Floor.CellSize;
        double travelTime = travelDistance / Math.Max(0.2, preferredSpeed);
        (double congestion, double queuePressure, double progressPenalty) = EstimateExitCongestion(exitIndex, exits[exitIndex].Location, position, activeAgents, spatialIndex, travelDistance, timeGap);
        double personalBias = Math.Sin(noiseOffset + (exitIndex * 1.713)) * 0.9;
        double imperfectTravelTime = Math.Sqrt(Math.Max(0.05, travelTime)) * (1.0 + (Math.Cos(noiseOffset + (exitIndex * 0.917)) * 0.05));

        return
            (-imperfectTravelTime * ExitDistanceWeight) +
            (-congestion * congestionSensitivity * ExitCongestionWeight) +
            (-queuePressure * congestionSensitivity * ExitQueueWeight) +
            (-progressPenalty * ExitProgressWeight) +
            personalBias;
    }

    private static (double Congestion, double QueuePressure, double ProgressPenalty) EstimateExitCongestion(
        int exitIndex,
        Point3d exitLocation,
        Point3d position,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex? spatialIndex,
        double pathDistance,
        double timeGap)
    {
        double localCompetition = 0.0;
        double queueCompetition = 0.0;
        double progressPenalty = 0.0;
        double routeAwarenessRadius = Math.Max(ExitAwarenessRadius * 1.4, Math.Min(pathDistance * 0.35, ExitQueueRadius * 1.8));
        double currentDistanceToExit = position.DistanceTo(exitLocation);

        if (spatialIndex != null)
        {
            // Fast path: query only nearby agents for local competition and progress penalty.
            double localQueryRadius = Math.Max(routeAwarenessRadius, Math.Max(ExitQueueRadius, pathDistance * 0.3));
            foreach (CrowdAgentState other in spatialIndex.Query(position, localQueryRadius))
            {
                if (other.IsFinished || other.ExitIndex != exitIndex)
                {
                    continue;
                }

                double distanceToAgent = other.Position.DistanceTo(position);
                if (distanceToAgent <= routeAwarenessRadius)
                {
                    localCompetition += 1.0 - Math.Min(1.0, distanceToAgent / Math.Max(routeAwarenessRadius, 1e-6));
                }

                double distanceToExit = other.Position.DistanceTo(exitLocation);
                if (distanceToExit < currentDistanceToExit && distanceToAgent <= Math.Max(ExitQueueRadius, pathDistance * 0.3))
                {
                    double followingGap = Math.Max(0.25, timeGap * other.PreferredSpeed);
                    progressPenalty += Math.Max(0.0, followingGap - distanceToAgent) / followingGap;
                }
            }

            // Queue pressure: agents physically near the exit (different query center).
            foreach (CrowdAgentState other in spatialIndex.Query(exitLocation, ExitQueueRadius))
            {
                if (other.IsFinished || other.ExitIndex != exitIndex)
                {
                    continue;
                }

                double distanceToExit = other.Position.DistanceTo(exitLocation);
                queueCompetition += 1.0 - Math.Min(1.0, distanceToExit / ExitQueueRadius);
            }
        }
        else
        {
            // Fallback path used at spawn time when no spatial index is available yet.
            foreach (CrowdAgentState other in activeAgents)
            {
                if (other.IsFinished || other.ExitIndex != exitIndex)
                {
                    continue;
                }

                double distanceToAgent = other.Position.DistanceTo(position);
                if (distanceToAgent <= routeAwarenessRadius)
                {
                    localCompetition += 1.0 - Math.Min(1.0, distanceToAgent / Math.Max(routeAwarenessRadius, 1e-6));
                }

                double distanceToExit = other.Position.DistanceTo(exitLocation);
                if (distanceToExit <= ExitQueueRadius)
                {
                    queueCompetition += 1.0 - Math.Min(1.0, distanceToExit / ExitQueueRadius);
                }

                if (distanceToExit < currentDistanceToExit && distanceToAgent <= Math.Max(ExitQueueRadius, pathDistance * 0.3))
                {
                    double followingGap = Math.Max(0.25, timeGap * other.PreferredSpeed);
                    progressPenalty += Math.Max(0.0, followingGap - distanceToAgent) / followingGap;
                }
            }
        }

        return (
            (localCompetition * 0.45) + (queueCompetition * 0.95),
            queueCompetition,
            progressPenalty);
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
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex)
    {
        if (!grid.TryGetClosestWalkableCell(agent.Position, out int x, out int y))
        {
            return Vector3d.Zero;
        }

        double currentDensity = EstimateNeighborhoodDensity(agent.Position, activeAgents, spatialIndex, agent.Id, agent.Radius * 4.0);
        if (currentDensity < 0.08)
        {
            return Vector3d.Zero;
        }

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
                double neighborDensity = EstimateNeighborhoodDensity(neighbor, activeAgents, spatialIndex, agent.Id, agent.Radius * 4.0);
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
        CrowdExit targetExit,
        double[,] field,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex,
        int x,
        int y,
        double time)
    {
        Point3d bestPoint = grid.GetCellCenter(x, y);
        double bestScore = double.NegativeInfinity;
        double secondBestScore = double.NegativeInfinity;
        List<(Point3d Point, double Score, double WinnerSafety)> candidates = new();
        Vector3d currentHeading = agent.Velocity;
        double routeFocus = GetRouteFocusFactor(agent, time);
        double desiredClearance = agent.Radius + agent.WallBufferDistance + (agent.ComfortDistance * 0.5);
        double currentBoundaryDistance = grid.GetBoundaryDistance(agent.Position);
        double currentFieldDistance = GetFieldDistanceAtPoint(agent.Position, grid, field);
        double openSpaceThreshold = desiredClearance * 2.8;
        bool routeNearConstraint = currentBoundaryDistance < openSpaceThreshold;
        double targetZoneFactor = GetTargetZoneStabilityFactor(agent, targetExit);
        double finalApproachFactor = GetFinalApproachFactor(agent, targetExit);
        if (!currentHeading.Unitize())
        {
            currentHeading = Vector3d.Zero;
        }

        double bestWinnerSafety = 0.0;
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
                double candidateBoundaryDistance = grid.GetBoundaryDistance(candidate);
                bool evaluateConstraintDetail = routeNearConstraint || candidateBoundaryDistance < openSpaceThreshold;
                double rawProgress = double.IsInfinity(currentFieldDistance)
                    ? 0.0
                    : currentFieldDistance - neighborField;
                double progressScore = double.IsInfinity(currentFieldDistance)
                    ? 0.0
                    : Math.Max(0.0, rawProgress) * CandidateProgressWeight;

                double fieldScore = -neighborField * CandidateFieldWeight * agent.RouteCommitment * Lerp(EntranceDiffusionFieldFactor, 1.0, routeFocus);
                double headingScore = currentHeading.Length <= 1e-6 ? 0.0 : Vector3d.Multiply(currentHeading, candidateDirection) * CandidateHeadingWeight;
                double densityScore = -EstimateNeighborhoodDensity(candidate, activeAgents, spatialIndex, agent.Id, agent.Radius * 3.5) * CandidateDensityWeight;
                double clearanceScore = evaluateConstraintDetail
                    ? GetClearanceScore(candidate, grid, Math.Max(agent.Radius * (WallInfluenceFactor + model.AgentProfile.WallAvoidance), 1.4 + model.AgentProfile.WallAvoidance))
                        * CandidateClearanceWeight
                        * (0.8 + (model.AgentProfile.WallAvoidance * 0.5))
                    : 0.0;
                Vector3d futureDirection = EstimateFieldDirection(grid, field, nx, ny);
                double turnScore = futureDirection.Length <= 1e-6
                    ? 0.0
                    : Vector3d.Multiply(candidateDirection, futureDirection) * CandidateTurnWeight * model.AgentProfile.TurnAnticipation * agent.CurvaturePreference;
                double widthScore = GetPassageWidthScore(candidateBoundaryDistance, currentBoundaryDistance, desiredClearance);
                double forwardClearanceScore = evaluateConstraintDetail
                    ? GetForwardClearanceScore(candidate, futureDirection.Length > 1e-6 ? futureDirection : candidateDirection, grid, desiredClearance)
                    : 0.0;
                double flowSpacingScore = GetFlowSpacingScore(agent, candidate, candidateDirection, activeAgents, spatialIndex);
                double streamPenalty = GetAlignedStreamPenalty(agent, candidate, candidateDirection, activeAgents, spatialIndex);
                double bottleneckPenalty = evaluateConstraintDetail
                    ? GetBottleneckPenalty(agent, candidate, futureDirection.Length > 1e-6 ? futureDirection : candidateDirection, grid, desiredClearance)
                    : 0.0;
                double apexPenalty = evaluateConstraintDetail
                    ? GetApexPenalty(agent, candidate, candidateDirection, futureDirection, grid, desiredClearance)
                    : 0.0;
                double pocketTrapPenalty = evaluateConstraintDetail
                    ? GetPocketTrapPenalty(agent, candidate, futureDirection.Length > 1e-6 ? futureDirection : candidateDirection, grid, field, desiredClearance)
                    : 0.0;
                double laneCommitmentScore = GetLaneCommitmentScore(agent, candidateDirection, futureDirection.Length > 1e-6 ? futureDirection : currentHeading);
                double sideScore = Vector3d.Multiply(new Vector3d(-candidateDirection.Y, candidateDirection.X, 0.0), currentHeading) * agent.SideBias * 0.12 * Lerp(EntranceDiffusionSideFactor, 1.0, routeFocus);
                double randomScore = Math.Sin((neighborField * 0.37) + agent.NoiseOffset) * CandidateRandomnessWeight * Lerp(EntranceDiffusionRandomFactor, 1.0, routeFocus);
                Vector3d toExit = targetExit.Location - candidate;
                double targetAlignmentScore = toExit.Unitize()
                    ? Math.Max(0.0, Vector3d.Multiply(candidateDirection, toExit)) * CandidateTargetAlignmentWeight * Math.Max(finalApproachFactor, targetZoneFactor)
                    : 0.0;
                double localConstraintFactor = Math.Max(
                    targetZoneFactor,
                    Math.Max(
                        Math.Max(bottleneckPenalty, apexPenalty),
                        pocketTrapPenalty));
                double lowProgressPenalty = rawProgress <= 1e-6
                    ? CandidateLowProgressPenaltyWeight * (0.65 + (localConstraintFactor * 0.7))
                    : RemapClamped(rawProgress, 0.0, Math.Max(grid.Floor.CellSize * 0.45, 0.08), CandidateLowProgressPenaltyWeight, 0.0)
                        * (0.3 + localConstraintFactor);
                double recirculationPenalty = evaluateConstraintDetail
                    ? GetRecirculationPenalty(
                        agent,
                        candidateDistance,
                        rawProgress,
                        candidateDirection,
                        futureDirection.Length > 1e-6 ? futureDirection : currentHeading,
                        forwardClearanceScore,
                        widthScore,
                        candidateBoundaryDistance,
                        desiredClearance,
                        bottleneckPenalty,
                        apexPenalty,
                        pocketTrapPenalty,
                        localConstraintFactor,
                        grid)
                    : 0.0;
                double winnerSafety = evaluateConstraintDetail
                    ? GetWinnerSafetyFactor(
                        rawProgress,
                        forwardClearanceScore,
                        widthScore,
                        candidateBoundaryDistance,
                        desiredClearance,
                        bottleneckPenalty,
                        apexPenalty,
                        pocketTrapPenalty)
                    : 1.0;
                double curvaturePenalty = currentHeading.Length <= 1e-6
                    ? 0.0
                    : Math.Pow(Math.Max(0.0, 1.0 - Vector3d.Multiply(currentHeading, candidateDirection)), 2.0) * CurvaturePenaltyWeight * agent.CurvaturePreference * routeFocus;
                double score =
                    fieldScore +
                    progressScore +
                    headingScore +
                    densityScore +
                    clearanceScore +
                    turnScore +
                    (widthScore * CandidateWidthWeight) +
                    (forwardClearanceScore * CandidateForwardClearanceWeight) +
                    (flowSpacingScore * CandidateFlowSpacingWeight) +
                    (laneCommitmentScore * CandidateLaneCommitmentWeight * Lerp(1.0, TargetZoneCommitFactor, localConstraintFactor)) +
                    (sideScore * Lerp(1.0, 0.14, localConstraintFactor)) +
                    (randomScore * Lerp(1.0, 0.1, Math.Max(localConstraintFactor, finalApproachFactor))) +
                    targetAlignmentScore -
                    lowProgressPenalty -
                    (recirculationPenalty * CandidateRecirculationPenaltyWeight) -
                    curvaturePenalty -
                    (streamPenalty * CandidateStreamPenaltyWeight) -
                    (bottleneckPenalty * CandidateBottleneckWeight) -
                    (apexPenalty * CandidateApexPenaltyWeight) -
                    (pocketTrapPenalty * CandidatePocketTrapWeight);
                candidates.Add((candidate, score, winnerSafety));

                if (score > bestScore)
                {
                    secondBestScore = bestScore;
                    bestScore = score;
                    bestPoint = candidate;
                    bestWinnerSafety = winnerSafety;
                }
                else if (score > secondBestScore)
                {
                    secondBestScore = score;
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
        double winnerGap = double.IsNegativeInfinity(secondBestScore) ? bestScore : bestScore - secondBestScore;
        double winnerCollapse =
            RemapClamped(winnerGap, 0.0, ConstrainedWinnerCollapseGap, 0.0, 1.0) *
            bestWinnerSafety *
            Math.Max(targetZoneFactor, routeNearConstraint ? 0.7 : 0.0);
        foreach ((Point3d point, double score, double winnerSafety) in candidates)
        {
            double blendTemperature =
                CandidateBlendTemperature *
                Lerp(1.0, BottleneckBlendTemperatureFactor, routeNearConstraint ? 1.0 : 0.0) *
                Lerp(1.0, TargetZoneBlendTemperatureFactor, targetZoneFactor) *
                Lerp(1.0, 0.72, Math.Max(targetZoneFactor, routeNearConstraint ? 0.5 : 0.0)) *
                Lerp(1.0, 0.6, winnerCollapse * Lerp(0.85, 1.0, winnerSafety));
            double scaled = (score - bestScore) / Math.Max(0.05, blendTemperature);
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
        anticipationBlend *= Lerp(1.0, 0.6, targetZoneFactor);
        return LerpPoint(bestPoint, blendedPoint, anticipationBlend);
    }

    private static double GetClearanceScore(Point3d point, CrowdGrid grid, double influenceRadius)
    {
        Vector3d repulsion = grid.GetBoundaryRepulsion(point, influenceRadius);
        return -repulsion.Length;
    }

    private static double GetPassageWidthScore(double candidateBoundaryDistance, double currentBoundaryDistance, double desiredClearance)
    {
        if (double.IsInfinity(candidateBoundaryDistance) || desiredClearance <= 1e-6)
        {
            return 0.0;
        }

        double normalized = candidateBoundaryDistance / desiredClearance;
        double sufficiency = RemapClamped(normalized, 0.55, 2.2, -1.15, 0.9);
        double wideningBias = currentBoundaryDistance <= 1e-6
            ? 0.0
            : RemapClamped(candidateBoundaryDistance - currentBoundaryDistance, -desiredClearance, desiredClearance, -0.35, 0.35);
        return sufficiency + wideningBias;
    }

    private static double GetForwardClearanceScore(
        Point3d candidate,
        Vector3d previewDirection,
        CrowdGrid grid,
        double desiredClearance)
    {
        if (!previewDirection.Unitize() || desiredClearance <= 1e-6)
        {
            return 0.0;
        }

        double step = Math.Max(grid.Floor.CellSize * BottleneckPreviewStepFactor, desiredClearance * 0.45);
        double clearanceSum = grid.GetBoundaryDistance(candidate);
        int samples = 1;

        for (int i = 1; i <= BottleneckPreviewSteps; i++)
        {
            Point3d probe = candidate + (previewDirection * (step * i));
            if (!grid.IsWalkable(probe))
            {
                break;
            }

            clearanceSum += grid.GetBoundaryDistance(probe);
            samples++;
        }

        double averageClearance = clearanceSum / Math.Max(samples, 1);
        return RemapClamped(averageClearance / desiredClearance, 0.8, 2.8, -0.7, 1.05);
    }

    private static double GetFlowSpacingScore(
        CrowdAgentState agent,
        Point3d candidate,
        Vector3d direction,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex)
    {
        if (!direction.Unitize())
        {
            return 0.0;
        }

        Vector3d tangent = new(-direction.Y, direction.X, 0.0);
        if (!tangent.Unitize())
        {
            return 0.0;
        }

        double desiredLateralSpacing = Math.Max(agent.ComfortDistance + agent.Radius, 0.2);
        double lookAhead = Math.Max(agent.PreferredSpeed * agent.TimeGap * 1.5, desiredLateralSpacing * 2.0);
        double penalty = 0.0;

        foreach (CrowdAgentState other in spatialIndex.Query(candidate, lookAhead))
        {
            if (other.Id == agent.Id || other.IsFinished)
            {
                continue;
            }

            Vector3d offset = other.Position - candidate;
            double longitudinal = Vector3d.Multiply(offset, direction);
            if (longitudinal < -agent.Radius || longitudinal > lookAhead)
            {
                continue;
            }

            double lateral = Math.Abs(Vector3d.Multiply(offset, tangent));
            double alignment = 0.5;
            if (other.Velocity.Length > 1e-6)
            {
                Vector3d otherDirection = other.Velocity;
                if (otherDirection.Unitize())
                {
                    alignment = RemapClamped((Vector3d.Multiply(otherDirection, direction) + 1.0) * 0.5, 0.0, 1.0, 0.25, 1.0);
                }
            }

            double lateralPenalty = Math.Max(0.0, (desiredLateralSpacing - lateral) / desiredLateralSpacing);
            double longitudinalWeight = 1.0 - Math.Min(1.0, longitudinal / Math.Max(lookAhead, 1e-6));
            penalty += lateralPenalty * longitudinalWeight * alignment;
        }

        return -penalty;
    }

    private static double GetAlignedStreamPenalty(
        CrowdAgentState agent,
        Point3d candidate,
        Vector3d direction,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex)
    {
        if (!direction.Unitize())
        {
            return 0.0;
        }

        Vector3d tangent = new(-direction.Y, direction.X, 0.0);
        if (!tangent.Unitize())
        {
            return 0.0;
        }

        double desiredLateralSpacing = Math.Max(agent.ComfortDistance + agent.Radius, 0.25);
        double radius = Math.Max(agent.PreferredSpeed * agent.TimeGap * 1.25, desiredLateralSpacing * 2.4);
        double penalty = 0.0;

        foreach (CrowdAgentState other in spatialIndex.Query(candidate, radius))
        {
            if (other.Id == agent.Id || other.IsFinished)
            {
                continue;
            }

            Vector3d offset = other.Position - candidate;
            double longitudinal = Math.Abs(Vector3d.Multiply(offset, direction));
            if (longitudinal > radius)
            {
                continue;
            }

            double lateral = Math.Abs(Vector3d.Multiply(offset, tangent));
            if (lateral >= desiredLateralSpacing)
            {
                continue;
            }

            Vector3d otherDirection = other.Velocity;
            double alignment = 0.55;
            if (otherDirection.Unitize())
            {
                alignment = RemapClamped(Vector3d.Multiply(otherDirection, direction), -0.2, 1.0, 0.0, 1.0);
            }

            double sameLanePressure = 1.0 - Math.Min(1.0, lateral / Math.Max(desiredLateralSpacing, 1e-6));
            double queuePressure = 1.0 - Math.Min(1.0, longitudinal / Math.Max(radius, 1e-6));
            penalty += sameLanePressure * queuePressure * alignment;
        }

        return penalty;
    }

    private static double GetBottleneckPenalty(
        CrowdAgentState agent,
        Point3d candidate,
        Vector3d previewDirection,
        CrowdGrid grid,
        double desiredClearance)
    {
        if (!previewDirection.Unitize())
        {
            return 0.0;
        }

        double currentClearance = grid.GetBoundaryDistance(candidate);
        if (double.IsInfinity(currentClearance))
        {
            return 0.0;
        }

        double minForwardClearance = currentClearance;
        double step = Math.Max(grid.Floor.CellSize * BottleneckPreviewStepFactor, agent.Radius * 0.8);
        for (int i = 1; i <= BottleneckPreviewSteps; i++)
        {
            Point3d probe = candidate + (previewDirection * (step * i));
            if (!grid.IsWalkable(probe))
            {
                break;
            }

            minForwardClearance = Math.Min(minForwardClearance, grid.GetBoundaryDistance(probe));
        }

        return Math.Max(0.0, (desiredClearance - minForwardClearance) / Math.Max(desiredClearance, 1e-6));
    }

    private static double GetApexPenalty(
        CrowdAgentState agent,
        Point3d candidate,
        Vector3d candidateDirection,
        Vector3d futureDirection,
        CrowdGrid grid,
        double desiredClearance)
    {
        if (!candidateDirection.Unitize())
        {
            return 0.0;
        }

        double currentClearance = grid.GetBoundaryDistance(candidate);
        if (double.IsInfinity(currentClearance))
        {
            return 0.0;
        }

        Vector3d probeDirection = futureDirection;
        if (!probeDirection.Unitize())
        {
            probeDirection = candidateDirection;
        }

        double step = Math.Max(grid.Floor.CellSize * BottleneckPreviewStepFactor, agent.Radius * 0.8);
        double forwardClearanceSum = 0.0;
        int forwardSamples = 0;
        for (int i = 1; i <= BottleneckPreviewSteps; i++)
        {
            Point3d probe = candidate + (probeDirection * (step * i));
            if (!grid.IsWalkable(probe))
            {
                break;
            }

            forwardClearanceSum += grid.GetBoundaryDistance(probe);
            forwardSamples++;
        }

        if (forwardSamples == 0)
        {
            return 0.0;
        }

        double averageForwardClearance = forwardClearanceSum / forwardSamples;
        double turnAmount = futureDirection.Length <= 1e-6
            ? 0.0
            : Math.Max(0.0, 1.0 - Vector3d.Multiply(candidateDirection, futureDirection));
        double apexNarrowness = Math.Max(0.0, (averageForwardClearance - currentClearance) / Math.Max(desiredClearance, 1e-6));
        double localPinch = Math.Max(0.0, (desiredClearance - currentClearance) / Math.Max(desiredClearance, 1e-6));
        return apexNarrowness * (0.55 + turnAmount) * (0.4 + localPinch);
    }

    private static double GetPocketTrapPenalty(
        CrowdAgentState agent,
        Point3d candidate,
        Vector3d previewDirection,
        CrowdGrid grid,
        double[,] field,
        double desiredClearance)
    {
        if (!previewDirection.Unitize())
        {
            return 0.0;
        }

        double startField = GetFieldDistanceAtPoint(candidate, grid, field);
        if (double.IsInfinity(startField))
        {
            return 0.0;
        }

        double step = Math.Max(grid.Floor.CellSize * BottleneckPreviewStepFactor, agent.Radius * 0.8);
        double bestProgress = 0.0;
        double lowestClearance = double.PositiveInfinity;
        int samples = 0;

        for (int i = 1; i <= BottleneckPreviewSteps; i++)
        {
            Point3d probe = candidate + (previewDirection * (step * i));
            if (!grid.IsWalkable(probe))
            {
                break;
            }

            double probeField = GetFieldDistanceAtPoint(probe, grid, field);
            if (double.IsInfinity(probeField))
            {
                break;
            }

            bestProgress = Math.Max(bestProgress, startField - probeField);
            lowestClearance = Math.Min(lowestClearance, grid.GetBoundaryDistance(probe));
            samples++;
        }

        if (samples == 0 || double.IsInfinity(lowestClearance))
        {
            return 0.0;
        }

        double expectedProgress = samples * 0.75;
        double poorProgress = Math.Max(0.0, (expectedProgress - bestProgress) / Math.Max(expectedProgress, 1e-6));
        double pinchPenalty = Math.Max(0.0, (desiredClearance - lowestClearance) / Math.Max(desiredClearance, 1e-6));
        return poorProgress * (0.45 + pinchPenalty);
    }

    private static double GetRecirculationPenalty(
        CrowdAgentState agent,
        double candidateDistance,
        double rawProgress,
        Vector3d candidateDirection,
        Vector3d referenceDirection,
        double forwardClearanceScore,
        double widthScore,
        double candidateBoundaryDistance,
        double desiredClearance,
        double bottleneckPenalty,
        double apexPenalty,
        double pocketTrapPenalty,
        double localConstraintFactor,
        CrowdGrid grid)
    {
        if (localConstraintFactor <= 1e-6 || !candidateDirection.Unitize())
        {
            return 0.0;
        }

        double shortMoveThreshold = Math.Max(grid.Floor.CellSize * 1.4, Math.Max(agent.ComfortDistance * 0.9, agent.Radius * 2.4));
        double shortMoveFactor = 1.0 - Math.Min(1.0, candidateDistance / Math.Max(shortMoveThreshold, 1e-6));
        double lowProgressFactor = rawProgress <= 1e-6
            ? 1.0
            : 1.0 - Math.Min(1.0, rawProgress / Math.Max(grid.Floor.CellSize * 0.55, 0.08));

        double turnAwayFactor = 0.0;
        if (referenceDirection.Unitize())
        {
            turnAwayFactor = Math.Max(0.0, 1.0 - Vector3d.Multiply(candidateDirection, referenceDirection));
        }

        double clearanceSupport = desiredClearance <= 1e-6
            ? 0.0
            : Math.Min(1.0, Math.Max(0.0, candidateBoundaryDistance / desiredClearance));
        double escapeEvidence =
            (Math.Max(0.0, forwardClearanceScore) * 0.55) +
            (Math.Max(0.0, widthScore) * 0.2) +
            (clearanceSupport * 0.25);
        double trapPressure = Math.Max(bottleneckPenalty, Math.Max(apexPenalty, pocketTrapPenalty));
        double reliefFactor = Math.Max(0.15, 1.0 - Math.Min(1.0, escapeEvidence * (0.75 + (0.45 * (1.0 - trapPressure)))));

        return lowProgressFactor * (0.35 + shortMoveFactor) * (0.25 + turnAwayFactor) * localConstraintFactor * reliefFactor;
    }

    private static double GetWinnerSafetyFactor(
        double rawProgress,
        double forwardClearanceScore,
        double widthScore,
        double candidateBoundaryDistance,
        double desiredClearance,
        double bottleneckPenalty,
        double apexPenalty,
        double pocketTrapPenalty)
    {
        double progressFactor = rawProgress <= 0.0
            ? 0.0
            : Math.Min(1.0, rawProgress / Math.Max(0.16, desiredClearance * 0.35));
        double forwardFactor = Math.Max(0.0, Math.Min(1.0, forwardClearanceScore));
        double widthFactor = RemapClamped(widthScore, -0.25, 0.45, 0.0, 1.0);
        double clearanceFactor = desiredClearance <= 1e-6
            ? 0.0
            : Math.Min(1.0, Math.Max(0.0, candidateBoundaryDistance / desiredClearance));
        double trapPenalty = Math.Max(bottleneckPenalty, Math.Max(apexPenalty, pocketTrapPenalty));
        double baseSafety =
            (progressFactor * 0.34) +
            (forwardFactor * 0.34) +
            (widthFactor * 0.16) +
            (clearanceFactor * 0.16);

        return Math.Max(0.0, Math.Min(1.0, baseSafety * (1.0 - Math.Min(1.0, trapPenalty * 0.8))));
    }

    private static double GetLaneCommitmentScore(CrowdAgentState agent, Vector3d candidateDirection, Vector3d referenceDirection)
    {
        if (!candidateDirection.Unitize())
        {
            return 0.0;
        }

        if (!referenceDirection.Unitize())
        {
            return 0.0;
        }

        double preferredSign = Math.Sign(agent.LaneCommitmentBias);
        if (preferredSign == 0.0)
        {
            return 0.0;
        }

        double signedTurn = (referenceDirection.X * candidateDirection.Y) - (referenceDirection.Y * candidateDirection.X);
        double magnitude = Math.Abs(signedTurn);
        if (magnitude <= 1e-4)
        {
            return 0.08;
        }

        double sign = Math.Sign(signedTurn);
        return sign == preferredSign ? (0.18 + (magnitude * 0.2)) : (-0.12 - (magnitude * 0.15));
    }

    private static Vector3d CalculateWallRepulsion(CrowdAgentState agent, CrowdGrid grid, double wallAvoidance)
    {
        double influenceRadius = Math.Max(agent.Radius * (WallInfluenceFactor + wallAvoidance), 1.35 + wallAvoidance + agent.WallBufferDistance);
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

        double boundaryDistance = grid.GetBoundaryDistance(agent.Position);
        double wallUrgency = boundaryDistance <= 1e-6
            ? 1.0
            : RemapClamped(boundaryDistance, 0.0, Math.Max(influenceRadius, 1e-6), 1.0, 0.15);
        return repulsion * WallRepulsionWeight * wallUrgency * (0.7 + (wallAvoidance * 0.65));
    }

    private static Vector3d CalculateWallFollowing(CrowdAgentState agent, CrowdGrid grid, Vector3d desiredDirection, double wallAvoidance)
    {
        if (!desiredDirection.Unitize())
        {
            return Vector3d.Zero;
        }

        double influenceRadius = Math.Max(agent.Radius * (WallInfluenceFactor + wallAvoidance), 1.1 + wallAvoidance + agent.WallBufferDistance);
        Vector3d repulsion = grid.GetBoundaryRepulsion(agent.Position, influenceRadius);
        if (repulsion.Length <= 1e-6)
        {
            return Vector3d.Zero;
        }

        Vector3d tangentA = new(-repulsion.Y, repulsion.X, 0.0);
        Vector3d tangentB = new(repulsion.Y, -repulsion.X, 0.0);
        if (!tangentA.Unitize() || !tangentB.Unitize())
        {
            return Vector3d.Zero;
        }

        Vector3d tangent = Vector3d.Multiply(tangentA, desiredDirection) >= Vector3d.Multiply(tangentB, desiredDirection)
            ? tangentA
            : tangentB;
        double alignment = Math.Max(0.0, Vector3d.Multiply(tangent, desiredDirection));
        if (alignment <= 1e-6)
        {
            return Vector3d.Zero;
        }

        double boundaryDistance = grid.GetBoundaryDistance(agent.Position);
        double followFactor = RemapClamped(boundaryDistance, 0.0, Math.Max(influenceRadius, 1e-6), 1.0, 0.0);
        return tangent * (alignment * followFactor * WallFollowWeight);
    }

    private static double GetBottleneckRegimeFactor(CrowdAgentState agent, CrowdGrid grid, Vector3d direction, double desiredClearance)
    {
        if (!direction.Unitize())
        {
            return 0.0;
        }

        double boundaryDistance = grid.GetBoundaryDistance(agent.Position);
        double localPinch = Math.Max(0.0, (desiredClearance - boundaryDistance) / Math.Max(desiredClearance, 1e-6));
        double forwardPinch = GetBottleneckPenalty(agent, agent.Position, direction, grid, desiredClearance);
        return Math.Max(localPinch, forwardPinch);
    }

    private static double GetFlowFollowRegimeFactor(CrowdAgentState agent, Vector3d direction, IReadOnlyList<CrowdAgentState> activeAgents, AgentSpatialIndex spatialIndex)
    {
        if (!direction.Unitize())
        {
            return 0.0;
        }

        double alignmentSum = 0.0;
        double weightSum = 0.0;
        double radius = Math.Max(agent.PreferredSpeed * agent.TimeGap * 1.8, agent.Radius * 6.0);
        foreach (CrowdAgentState other in spatialIndex.Query(agent.Position, radius))
        {
            if (other.Id == agent.Id || other.IsFinished || other.Velocity.Length <= 1e-6)
            {
                continue;
            }

            double distance = other.Position.DistanceTo(agent.Position);
            if (distance <= 1e-6 || distance > radius)
            {
                continue;
            }

            Vector3d otherDirection = other.Velocity;
            if (!otherDirection.Unitize())
            {
                continue;
            }

            double distanceWeight = 1.0 - Math.Min(1.0, distance / radius);
            double alignment = Math.Max(0.0, Vector3d.Multiply(direction, otherDirection));
            alignmentSum += alignment * distanceWeight;
            weightSum += distanceWeight;
        }

        if (weightSum <= 1e-6)
        {
            return 0.0;
        }

        return Math.Max(0.0, Math.Min(1.0, alignmentSum / weightSum));
    }

    private static double GetConflictFactor(Vector3d separation, Vector3d collisionAvoidance, Vector3d densityAvoidance)
    {
        double intensity = separation.Length + collisionAvoidance.Length + (densityAvoidance.Length * 0.65);
        return RemapClamped(intensity, 0.0, 2.5, 0.0, 1.0);
    }

    private static Vector3d CalculateFlowFollow(CrowdAgentState agent, Vector3d direction, IReadOnlyList<CrowdAgentState> activeAgents, AgentSpatialIndex spatialIndex)
    {
        if (!direction.Unitize())
        {
            return Vector3d.Zero;
        }

        Vector3d blended = Vector3d.Zero;
        double weightSum = 0.0;
        double radius = Math.Max(agent.PreferredSpeed * agent.TimeGap * 2.0, agent.Radius * 7.0);
        foreach (CrowdAgentState other in spatialIndex.Query(agent.Position, radius))
        {
            if (other.Id == agent.Id || other.IsFinished || other.Velocity.Length <= 1e-6)
            {
                continue;
            }

            double distance = other.Position.DistanceTo(agent.Position);
            if (distance <= 1e-6 || distance > radius)
            {
                continue;
            }

            Vector3d otherDirection = other.Velocity;
            if (!otherDirection.Unitize())
            {
                continue;
            }

            double weight = 1.0 - Math.Min(1.0, distance / radius);
            blended += otherDirection * weight;
            weightSum += weight;
        }

        if (weightSum <= 1e-6 || !blended.Unitize())
        {
            return Vector3d.Zero;
        }

        return blended;
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

    private static Point3d StabilizeProposedMove(
        CrowdAgentState agent,
        double timeStep,
        CrowdGrid grid,
        double[,] field,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex,
        Point3d proposed)
    {
        double currentFieldDistance = GetFieldDistanceAtPoint(agent.Position, grid, field);
        if (double.IsInfinity(currentFieldDistance))
        {
            return proposed;
        }

        double proposedFieldDistance = GetFieldDistanceAtPoint(proposed, grid, field);
        if (double.IsInfinity(proposedFieldDistance))
        {
            return CreateFieldRecoveryMove(agent, timeStep, grid, field, activeAgents, spatialIndex);
        }

        double moveDistance = proposed.DistanceTo(agent.Position);
        double regressionAllowance = Math.Max(
            grid.Floor.CellSize * FieldRegressionCellFactor,
            agent.PreferredSpeed * timeStep * FieldRegressionStepFactor);
        if (agent.StuckDuration >= StuckActivationTime)
        {
            regressionAllowance = Math.Max(
                regressionAllowance,
                Math.Min(grid.Floor.CellSize * 0.85, Math.Max(moveDistance * 0.75, agent.Radius * 0.9)));
        }

        if (proposedFieldDistance <= currentFieldDistance + regressionAllowance)
        {
            return proposed;
        }

        if (agent.StuckDuration >= StuckActivationTime * 0.65 && moveDistance > 1e-6)
        {
            double escapeAllowance = Math.Max(grid.Floor.CellSize * 1.1, moveDistance * 1.2);
            if (proposedFieldDistance <= currentFieldDistance + escapeAllowance)
            {
                return proposed;
            }
        }

        Point3d recovered = CreateFieldRecoveryMove(agent, timeStep, grid, field, activeAgents, spatialIndex);
        double recoveredFieldDistance = GetFieldDistanceAtPoint(recovered, grid, field);
        if (!double.IsInfinity(recoveredFieldDistance) && recoveredFieldDistance <= currentFieldDistance + regressionAllowance)
        {
            return recovered;
        }

        return agent.Position;
    }

    private static Point3d CreateFieldRecoveryMove(
        CrowdAgentState agent,
        double timeStep,
        CrowdGrid grid,
        double[,] field,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex)
    {
        if (!grid.TryGetClosestWalkableCell(agent.Position, out int x, out int y))
        {
            return agent.Position;
        }

        Vector3d recoveryDirection = SampleContinuousFlowDirection(agent.Position, grid, field);
        if (!recoveryDirection.Unitize())
        {
            recoveryDirection = EstimateFieldDirection(grid, field, x, y);
        }

        if (!recoveryDirection.Unitize())
        {
            (int bestX, int bestY) = CrowdPathFieldBuilder.GetBestNeighbor(grid, field, x, y);
            recoveryDirection = grid.GetCellCenter(bestX, bestY) - agent.Position;
        }

        if (!recoveryDirection.Unitize())
        {
            return agent.Position;
        }

        double baseStep = Math.Max(grid.Floor.CellSize * 0.35, Math.Min(agent.PreferredSpeed * timeStep, grid.Floor.CellSize * 0.9));
        double[] stepFactors = new[] { 1.0, 0.65, 0.35 };
        foreach (double factor in stepFactors)
        {
            Point3d candidate = agent.Position + (recoveryDirection * (baseStep * factor));
            if (!grid.IsWalkable(candidate) || IsOccupiedByOthers(agent, candidate, activeAgents, spatialIndex, agent.Radius * 1.2))
            {
                continue;
            }

            return candidate;
        }

        return agent.Position;
    }

    private static Point3d CreateDeadlockReleaseMove(
        CrowdAgentState agent,
        double timeStep,
        CrowdGrid grid,
        double[,] field,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex,
        Vector3d preferredVelocity)
    {
        Vector3d baseDirection = preferredVelocity;
        if (!baseDirection.Unitize())
        {
            baseDirection = SampleContinuousFlowDirection(agent.Position, grid, field);
        }

        if (!baseDirection.Unitize() && grid.TryGetClosestWalkableCell(agent.Position, out int x, out int y))
        {
            baseDirection = EstimateFieldDirection(grid, field, x, y);
        }

        if (!baseDirection.Unitize())
        {
            return agent.Position;
        }

        double currentFieldDistance = GetFieldDistanceAtPoint(agent.Position, grid, field);
        if (double.IsInfinity(currentFieldDistance))
        {
            currentFieldDistance = double.PositiveInfinity;
        }

        double baseStep = Math.Max(agent.PreferredSpeed * timeStep * 0.9, Math.Min(grid.Floor.CellSize * 0.75, agent.Radius * 2.0));
        double allowedRegression = Math.Max(grid.Floor.CellSize * 1.35, agent.Radius * 2.4);
        Point3d bestPoint = agent.Position;
        double bestScore = double.NegativeInfinity;

        double baseAngle = Math.Atan2(baseDirection.Y, baseDirection.X);
        double[] angleOffsets =
        {
            0.0,
            Math.PI / 9.0,
            -Math.PI / 9.0,
            Math.PI / 4.0,
            -Math.PI / 4.0,
            Math.PI / 2.0,
            -Math.PI / 2.0,
            Math.PI
        };
        double[] stepFactors = { 1.0, 0.7, 0.45 };
        double occupancyLimit = agent.StuckDuration >= StuckActivationTime * 2.0
            ? agent.Radius * 0.35
            : agent.Radius * 0.65;

        foreach (double stepFactor in stepFactors)
        {
            foreach (double angleOffset in angleOffsets)
            {
                double angle = baseAngle + angleOffset;
                Vector3d direction = new(Math.Cos(angle), Math.Sin(angle), 0.0);
                Point3d candidate = agent.Position + (direction * (baseStep * stepFactor));
                if (!grid.IsWalkable(candidate) || IsOccupiedByOthers(agent, candidate, activeAgents, spatialIndex, occupancyLimit))
                {
                    continue;
                }

                double candidateFieldDistance = GetFieldDistanceAtPoint(candidate, grid, field);
                if (!double.IsInfinity(candidateFieldDistance) && !double.IsInfinity(currentFieldDistance)
                    && candidateFieldDistance > currentFieldDistance + allowedRegression)
                {
                    continue;
                }

                double progressScore = double.IsInfinity(candidateFieldDistance) || double.IsInfinity(currentFieldDistance)
                    ? 0.0
                    : currentFieldDistance - candidateFieldDistance;
                double alignmentScore = Vector3d.Multiply(direction, baseDirection);
                double clearanceScore = GetClearanceScore(candidate, grid, Math.Max(agent.Radius * 3.0, agent.ComfortDistance + agent.Radius));
                double score = (progressScore * 1.8) + (alignmentScore * 0.45) + (clearanceScore * 0.25) + (stepFactor * 0.15);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = candidate;
                }
            }
        }

        return bestScore > double.NegativeInfinity ? bestPoint : agent.Position;
    }

    private static double GetRouteClarity(Point3d position, CrowdGrid grid, double[,] field, Vector3d direction)
    {
        if (!direction.Unitize())
        {
            return 0.0;
        }

        double currentFieldDistance = GetFieldDistanceAtPoint(position, grid, field);
        if (double.IsInfinity(currentFieldDistance))
        {
            return 0.0;
        }

        Point3d forwardProbe = position + (direction * Math.Max(grid.Floor.CellSize * 0.55, 0.2));
        double forwardFieldDistance = GetFieldDistanceAtPoint(forwardProbe, grid, field);
        if (double.IsInfinity(forwardFieldDistance))
        {
            return 0.0;
        }

        double improvement = currentFieldDistance - forwardFieldDistance;
        return RemapClamped(improvement, 0.0, Math.Max(grid.Floor.CellSize * 0.9, 0.5), 0.0, 1.0);
    }

    private static Vector3d BuildRouteDirection(Point3d position, CrowdGrid grid, double[,] field, int startX, int startY)
    {
        Point3d lookAheadPoint = BuildRouteLookAheadPoint(position, grid, field, startX, startY);
        Vector3d direction = lookAheadPoint - position;
        if (!direction.Unitize())
        {
            direction = EstimateFieldDirection(grid, field, startX, startY);
        }

        if (!direction.Unitize())
        {
            (int bestX, int bestY) = CrowdPathFieldBuilder.GetBestNeighbor(grid, field, startX, startY);
            direction = grid.GetCellCenter(bestX, bestY) - position;
        }

        return direction.Unitize() ? direction : Vector3d.Zero;
    }

    private static Point3d BuildRouteLookAheadPoint(Point3d position, CrowdGrid grid, double[,] field, int startX, int startY)
    {
        int currentX = startX;
        int currentY = startY;
        Point3d lastPoint = position;
        Point3d farthestVisiblePoint = position;
        double startField = GetFieldDistanceAtPoint(position, grid, field);
        double bestVisibleScore = double.NegativeInfinity;
        Vector3d currentHeading = SampleContinuousFlowDirection(position, grid, field);
        int maxSteps = 6;

        for (int step = 0; step < maxSteps; step++)
        {
            (int nextX, int nextY) = CrowdPathFieldBuilder.GetBestNeighbor(grid, field, currentX, currentY);
            if ((nextX == currentX && nextY == currentY) || double.IsInfinity(field[nextX, nextY]))
            {
                break;
            }

            if (field[nextX, nextY] >= field[currentX, currentY] - 1e-6)
            {
                break;
            }

            currentX = nextX;
            currentY = nextY;
            lastPoint = grid.GetCellCenter(currentX, currentY);

            if (HasDirectWalkableSight(position, lastPoint, grid))
            {
                double progress = double.IsInfinity(startField) ? 0.0 : Math.Max(0.0, startField - field[currentX, currentY]);
                double clearance = grid.GetBoundaryDistance(lastPoint);
                Vector3d segmentDirection = lastPoint - position;
                double turnPenalty = 0.0;
                if (segmentDirection.Unitize() && currentHeading.Unitize())
                {
                    turnPenalty = Math.Max(0.0, 1.0 - Vector3d.Multiply(currentHeading, segmentDirection));
                }

                double score =
                    (progress * CorridorVisibilityProgressWeight) +
                    (Math.Min(clearance, grid.Floor.CellSize * 3.0) * CorridorVisibilityClearanceWeight) -
                    (turnPenalty * CorridorVisibilityTurnPenalty);

                if (score >= bestVisibleScore)
                {
                    bestVisibleScore = score;
                    farthestVisiblePoint = lastPoint;
                }
            }
        }

        return farthestVisiblePoint.DistanceTo(position) > 1e-6 ? farthestVisiblePoint : lastPoint;
    }

    private static bool HasDirectWalkableSight(Point3d from, Point3d to, CrowdGrid grid)
    {
        double distance = from.DistanceTo(to);
        if (distance <= 1e-6)
        {
            return true;
        }

        int samples = Math.Max(3, (int)Math.Ceiling(distance / Math.Max(grid.Floor.CellSize * 0.35, 0.1)));
        double minClearance = Math.Max(grid.Floor.CellSize * 0.08, 0.04);
        for (int i = 1; i <= samples; i++)
        {
            double t = (double)i / samples;
            Point3d probe = LerpPoint(from, to, t);
            if (!grid.IsWalkable(probe))
            {
                return false;
            }

            double clearance = grid.GetBoundaryDistance(probe);
            if (!double.IsInfinity(clearance) && clearance < minClearance)
            {
                return false;
            }
        }

        return true;
    }

    private static Vector3d ComputeMotionVelocity(CrowdAgentState agent, CrowdGrid grid, double[,] field, Vector3d desiredVelocity, double timeStep)
    {
        double desiredSpeed = Math.Min(agent.MaxSpeed, desiredVelocity.Length);
        Vector3d desiredDirection = desiredVelocity;
        if (!desiredDirection.Unitize())
        {
            desiredDirection = Vector3d.Zero;
        }

        Vector3d currentDirection = agent.Velocity;
        double currentSpeed = currentDirection.Length;
        if (!currentDirection.Unitize())
        {
            currentDirection = desiredDirection;
        }

        Vector3d steeredDirection = RotateTowards(currentDirection, desiredDirection, GetMaxTurnRateRadians(agent) * Math.Max(timeStep, 1e-6));
        if (!steeredDirection.Unitize())
        {
            steeredDirection = desiredDirection;
        }

        double acceleratedSpeed = MoveTowardsScalar(currentSpeed, desiredSpeed, GetMaxSpeedDelta(agent, desiredSpeed >= currentSpeed, timeStep));
        double routeSpeed = CalculateRouteLimitedSpeed(agent, grid, field, steeredDirection);
        acceleratedSpeed = Math.Min(acceleratedSpeed, routeSpeed);
        return steeredDirection * acceleratedSpeed;
    }

    private static double CalculateWallLimitedSpeed(CrowdAgentState agent, CrowdGrid grid, Vector3d direction)
    {
        if (!direction.Unitize())
        {
            return agent.PreferredSpeed;
        }

        double clearance = grid.GetBoundaryDistance(agent.Position);
        Point3d previewPoint = agent.Position + (direction * Math.Max(grid.Floor.CellSize * WallClearancePreviewFactor, agent.Radius * 1.8));
        double previewClearance = grid.IsWalkable(previewPoint)
            ? grid.GetBoundaryDistance(previewPoint)
            : 0.0;
        double effectiveClearance = Math.Min(clearance, previewClearance);
        double desiredClearance = agent.Radius + agent.WallBufferDistance + (agent.ComfortDistance * 0.55);
        double slowdownFactor = RemapClamped(effectiveClearance, desiredClearance * 0.85, desiredClearance * 2.6, 0.22, 1.0);
        return Math.Max(0.18, agent.PreferredSpeed * slowdownFactor);
    }

    private static double CalculateTurningLimitedSpeed(CrowdAgentState agent, Vector3d routeDirection, Vector3d steeredDirection)
    {
        if (!routeDirection.Unitize() || !steeredDirection.Unitize())
        {
            return agent.PreferredSpeed;
        }

        double alignment = Math.Max(-1.0, Math.Min(1.0, Vector3d.Multiply(routeDirection, steeredDirection)));
        double speedFactor = RemapClamped(alignment, -0.15, 1.0, 0.3, 1.0);
        return Math.Max(0.18, agent.PreferredSpeed * speedFactor);
    }

    private static double CalculateRouteLimitedSpeed(CrowdAgentState agent, CrowdGrid grid, double[,] field, Vector3d direction)
    {
        if (!direction.Unitize())
        {
            return agent.PreferredSpeed;
        }

        double currentFieldDistance = GetFieldDistanceAtPoint(agent.Position, grid, field);
        if (double.IsInfinity(currentFieldDistance))
        {
            return agent.PreferredSpeed;
        }

        Point3d nearProbe = agent.Position + (direction * Math.Max(grid.Floor.CellSize * 0.45, agent.Radius));
        double nearFieldDistance = GetFieldDistanceAtPoint(nearProbe, grid, field);
        if (double.IsInfinity(nearFieldDistance))
        {
            return Math.Max(0.16, agent.PreferredSpeed * 0.35);
        }

        double improvement = currentFieldDistance - nearFieldDistance;
        double speedFactor = RemapClamped(improvement, -grid.Floor.CellSize * 0.3, grid.Floor.CellSize * 0.6, 0.42, 1.0);
        if (agent.StuckDuration >= StuckActivationTime)
        {
            speedFactor = Math.Max(speedFactor, 0.62);
        }

        return Math.Max(0.28, agent.PreferredSpeed * speedFactor);
    }

    private static Vector3d RotateTowards(Vector3d currentDirection, Vector3d targetDirection, double maxAngle)
    {
        bool hasCurrent = currentDirection.Unitize();
        bool hasTarget = targetDirection.Unitize();
        if (!hasTarget)
        {
            return Vector3d.Zero;
        }

        if (!hasCurrent || maxAngle <= 1e-6)
        {
            return targetDirection;
        }

        double currentAngle = Math.Atan2(currentDirection.Y, currentDirection.X);
        double targetAngle = Math.Atan2(targetDirection.Y, targetDirection.X);
        double delta = NormalizeAngleRadians(targetAngle - currentAngle);
        double clampedDelta = Math.Max(-maxAngle, Math.Min(maxAngle, delta));
        double resultAngle = currentAngle + clampedDelta;
        return new Vector3d(Math.Cos(resultAngle), Math.Sin(resultAngle), 0.0);
    }

    private static double NormalizeAngleRadians(double angle)
    {
        while (angle > Math.PI)
        {
            angle -= Math.PI * 2.0;
        }

        while (angle < -Math.PI)
        {
            angle += Math.PI * 2.0;
        }

        return angle;
    }

    private static double MoveTowardsScalar(double current, double target, double maxDelta)
    {
        if (Math.Abs(target - current) <= maxDelta)
        {
            return target;
        }

        return current + (Math.Sign(target - current) * maxDelta);
    }

    private static double GetMaxSpeedDelta(CrowdAgentState agent, bool accelerating, double timeStep)
    {
        double baseAcceleration = RemapClamped(agent.PreferredSpeed / Math.Max(0.25, agent.ReactionTime), 0.0, 6.0, 0.7, 1.5);
        double maxRate = accelerating ? baseAcceleration : baseAcceleration * 1.5;
        return maxRate * Math.Max(timeStep, 1e-6);
    }

    private static double GetMaxTurnRateRadians(CrowdAgentState agent)
    {
        double degreesPerSecond = RemapClamped((1.0 / Math.Max(0.2, agent.ReactionTime)) * agent.CurvaturePreference, 1.0, 6.0, 55.0, 110.0);
        return degreesPerSecond * (Math.PI / 180.0);
    }

    private static Vector3d CalculateStuckEscape(
        CrowdAgentState agent,
        CrowdGrid grid,
        double[,] field,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex)
    {
        if (agent.StuckDuration < StuckActivationTime)
        {
            return Vector3d.Zero;
        }

        if (!grid.TryGetClosestWalkableCell(agent.Position, out int x, out int y))
        {
            return Vector3d.Zero;
        }

        double bestScore = double.NegativeInfinity;
        Vector3d bestDirection = Vector3d.Zero;
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
                if (!grid.IsWalkable(nx, ny) || double.IsInfinity(field[nx, ny]))
                {
                    continue;
                }

                Point3d candidate = grid.GetCellCenter(nx, ny);
                Vector3d dir = candidate - agent.Position;
                if (!dir.Unitize())
                {
                    continue;
                }

                double fieldProgress = GetFieldDistanceAtPoint(agent.Position, grid, field) - field[nx, ny];
                double density = EstimateNeighborhoodDensity(candidate, activeAgents, spatialIndex, agent.Id, agent.Radius * 3.5);
                double clearance = grid.GetBoundaryDistance(candidate);
                double score = (fieldProgress * 1.2) - (density * 0.85) + Math.Min(clearance, agent.ComfortDistance * 2.0);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = dir;
                }
            }
        }

        return bestDirection.Length <= 1e-6 ? Vector3d.Zero : bestDirection * EscapeWeight * RemapClamped(agent.StuckDuration, StuckActivationTime, 2.4, 0.35, 1.0);
    }

    private static double CalculateCollisionFreeSpeed(
        CrowdAgentState agent,
        CrowdGrid grid,
        Vector3d direction,
        IReadOnlyList<CrowdAgentState> activeAgents,
        AgentSpatialIndex spatialIndex)
    {
        if (!direction.Unitize())
        {
            return agent.PreferredSpeed;
        }

        double headway = double.PositiveInfinity;
        double forwardHorizon = Math.Max(agent.PreferredSpeed * (agent.TimeGap + agent.ReactionTime), agent.Radius * 3.0);

        foreach (CrowdAgentState other in spatialIndex.Query(agent.Position, forwardHorizon))
        {
            if (other.Id == agent.Id || other.IsFinished)
            {
                continue;
            }

            Vector3d offset = other.Position - agent.Position;
            double longitudinal = Vector3d.Multiply(offset, direction);
            if (longitudinal <= 0.0 || longitudinal > forwardHorizon)
            {
                continue;
            }

            Vector3d lateralOffset = offset - (direction * longitudinal);
            double lateralDistance = lateralOffset.Length;
            double lateralThreshold = agent.Radius + other.Radius + agent.ComfortDistance;
            if (lateralDistance > lateralThreshold)
            {
                continue;
            }

            double freeGap = Math.Max(0.0, longitudinal - lateralThreshold);
            headway = Math.Min(headway, freeGap);
        }

        double wallHeadway = EstimateWallHeadway(agent, grid, direction, forwardHorizon);
        headway = Math.Min(headway, wallHeadway);

        if (double.IsInfinity(headway))
        {
            return agent.PreferredSpeed;
        }

        double safeTime = Math.Max(0.2, agent.TimeGap + (agent.ReactionTime * 0.8));
        double collisionFreeSpeed = Math.Max(0.12, headway / safeTime);
        return Math.Min(agent.PreferredSpeed, collisionFreeSpeed);
    }

    private static double EstimateWallHeadway(CrowdAgentState agent, CrowdGrid grid, Vector3d direction, double horizon)
    {
        double step = Math.Max(grid.Floor.CellSize * 0.55, agent.Radius * 0.65);
        for (double distance = step; distance <= horizon; distance += step)
        {
            Point3d probe = agent.Position + (direction * distance);
            if (!grid.IsWalkable(probe))
            {
                return Math.Max(0.0, distance - step);
            }

            double clearance = grid.GetBoundaryDistance(probe);
            if (!double.IsInfinity(clearance) && clearance < agent.Radius + (agent.WallBufferDistance * 0.6))
            {
                return Math.Max(0.0, distance - step);
            }
        }

        return double.PositiveInfinity;
    }

    private static double EstimateNeighborhoodDensity(Point3d point, IReadOnlyList<CrowdAgentState> agents, AgentSpatialIndex spatialIndex, int currentAgentId, double radius)
    {
        double density = 0.0;
        foreach (CrowdAgentState other in spatialIndex.Query(point, radius))
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

        double signal = Math.Sin((time * (NoiseFrequency / Math.Max(0.15, agent.ReactionTime))) + agent.NoiseOffset);
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
        double closingSpeed = hasTarget ? Math.Max(0.0, Vector3d.Multiply(agent.Velocity, toExit)) : speed;
        bool isNearExit = distance <= Math.Max(targetExit.Radius * 1.15, agent.ArrivalThreshold * 1.4);
        bool isSlowEnough = speed <= ExitSnapSpeedThreshold;
        bool isOrbiting = distance <= Math.Max(targetExit.Radius * ExitSpiralDetectionDistanceFactor, agent.ArrivalThreshold * 3.0)
            && alignment < 0.15;
        bool isClosingCleanly = distance <= Math.Max(targetExit.Radius * 1.8, agent.ArrivalThreshold * 2.0)
            && alignment > 0.72
            && closingSpeed >= ExitClosingSpeedFactor;

        if (isNearExit || isSlowEnough || isOrbiting || isClosingCleanly)
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

        double inertia = RemapClamped((turnAnticipation * agent.CurvaturePreference) / Math.Max(0.1, agent.ReactionTime), 0.0, 8.0, 0.18, 0.72);
        Vector3d smoothed = (currentHeading * inertia) + (targetDirection * (1.0 - inertia));
        if (!smoothed.Unitize())
        {
            return targetDirection;
        }

        return smoothed;
    }

    private static Vector3d ApplyDirectionalDamping(CrowdAgentState agent, Vector3d routeDirection, Vector3d targetDirection)
    {
        if (!targetDirection.Unitize())
        {
            return Vector3d.Zero;
        }

        Vector3d baseDirection = routeDirection;
        if (!baseDirection.Unitize())
        {
            baseDirection = agent.DesiredVelocity;
            if (!baseDirection.Unitize())
            {
                return targetDirection;
            }
        }

        double alignment = Math.Max(-1.0, Math.Min(1.0, Vector3d.Multiply(baseDirection, targetDirection)));
        double damping = RemapClamped(alignment, -0.2, 1.0, 0.22, 0.72);
        Vector3d blended = (baseDirection * damping) + (targetDirection * (1.0 - damping));
        if (!blended.Unitize())
        {
            return targetDirection;
        }

        return blended;
    }

    private static Vector3d SampleContinuousFlowDirection(Point3d position, CrowdGrid grid, double[,] field)
    {
        double step = Math.Max(grid.Floor.CellSize * 0.6, 0.25);
        double center = GetFieldDistanceAtPoint(position, grid, field);
        if (double.IsInfinity(center))
        {
            return Vector3d.Zero;
        }

        double fx1 = SampleFieldValue(new Point3d(position.X + step, position.Y, position.Z), grid, field);
        double fx0 = SampleFieldValue(new Point3d(position.X - step, position.Y, position.Z), grid, field);
        double fy1 = SampleFieldValue(new Point3d(position.X, position.Y + step, position.Z), grid, field);
        double fy0 = SampleFieldValue(new Point3d(position.X, position.Y - step, position.Z), grid, field);

        fx1 = double.IsInfinity(fx1) ? center : fx1;
        fx0 = double.IsInfinity(fx0) ? center : fx0;
        fy1 = double.IsInfinity(fy1) ? center : fy1;
        fy0 = double.IsInfinity(fy0) ? center : fy0;

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

    private static double GetFieldDistanceAtPoint(Point3d point, CrowdGrid grid, double[,] field)
    {
        double value = SampleFieldValue(point, grid, field);
        return double.IsInfinity(value) ? double.PositiveInfinity : value;
    }

    private static string BuildActiveTailSummary(
        IReadOnlyList<CrowdAgentState> agents,
        CrowdGrid grid,
        IReadOnlyList<double[,]> exitFields)
    {
        List<CrowdAgentState> activeAgents = agents.Where(agent => !agent.IsFinished).ToList();
        if (activeAgents.Count == 0)
        {
            return "none";
        }

        double finiteDistanceSum = 0.0;
        double minimumDistance = double.PositiveInfinity;
        double maximumDistance = 0.0;
        double speedSum = 0.0;
        int finiteDistanceCount = 0;
        Dictionary<int, int> byExit = new();

        foreach (CrowdAgentState agent in activeAgents)
        {
            speedSum += agent.Velocity.Length;

            byExit.TryGetValue(agent.ExitIndex, out int exitCount);
            byExit[agent.ExitIndex] = exitCount + 1;

            if (agent.ExitIndex < 0 || agent.ExitIndex >= exitFields.Count)
            {
                continue;
            }

            double distance = GetFieldDistanceAtPoint(agent.Position, grid, exitFields[agent.ExitIndex]);
            if (double.IsInfinity(distance))
            {
                continue;
            }

            finiteDistanceSum += distance;
            minimumDistance = Math.Min(minimumDistance, distance);
            maximumDistance = Math.Max(maximumDistance, distance);
            finiteDistanceCount++;
        }

        string distanceSummary = finiteDistanceCount == 0
            ? "field unreachable"
            : $"field min/avg/max {minimumDistance:0.##}/{finiteDistanceSum / finiteDistanceCount:0.##}/{maximumDistance:0.##} m";
        string exitSummary = string.Join(
            ", ",
            byExit
                .OrderBy(pair => pair.Key)
                .Select(pair => $"exit {pair.Key + 1}: {pair.Value}"));

        double stuckSum = activeAgents.Sum(agent => agent.StuckDuration);
        return $"{activeAgents.Count} active, avg speed {speedSum / activeAgents.Count:0.###} m/s, avg stuck {stuckSum / activeAgents.Count:0.#} s, {distanceSummary}, {exitSummary}";
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

    private static double GetTargetZoneStabilityFactor(CrowdAgentState agent, CrowdExit targetExit)
    {
        double stabilityDistance = Math.Max(
            Math.Max(targetExit.Radius * TargetZoneStabilityDistanceFactor, agent.ArrivalThreshold * TargetZoneStabilityDistanceFactor),
            Math.Max(agent.ComfortDistance * 4.0, agent.Radius * 8.0));
        double distance = agent.Position.DistanceTo(targetExit.Location);
        return 1.0 - Math.Max(0.0, Math.Min(1.0, distance / Math.Max(stabilityDistance, 1e-6)));
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

    private static void UpdateAgentProgressState(CrowdAgentState agent, CrowdGrid grid, double[,] field, double timeStep)
    {
        double currentFieldDistance = GetFieldDistanceAtPoint(agent.Position, grid, field);
        if (double.IsInfinity(currentFieldDistance))
        {
            return;
        }

        double progress = agent.LastFieldDistance - currentFieldDistance;
        if (progress <= StuckProgressTolerance)
        {
            agent.StuckDuration += timeStep;
        }
        else
        {
            agent.StuckDuration = Math.Max(0.0, agent.StuckDuration - (timeStep * 1.5));
        }

        agent.LastFieldDistance = currentFieldDistance;
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
        foreach (CrowdAgentState agent in agents)
        {
            if (agent.IsFinished)
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

    private static bool IsOccupiedByOthers(CrowdAgentState current, Point3d point, IEnumerable<CrowdAgentState> agents, AgentSpatialIndex spatialIndex, double minDistance)
    {
        foreach (CrowdAgentState agent in spatialIndex.Query(point, minDistance))
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

    private static void RecordFrame(
        List<CrowdFrame> frames,
        IReadOnlyList<CrowdAgentState> agents,
        double time,
        out int activeCount,
        out int finishedCount)
    {
        List<Point3d> activePositions = new(agents.Count);
        List<double> activeSpeeds = new(agents.Count);
        finishedCount = 0;

        foreach (CrowdAgentState agent in agents)
        {
            if (agent.IsFinished)
            {
                finishedCount++;
                continue;
            }

            activePositions.Add(agent.Position);
            activeSpeeds.Add(agent.Velocity.Length);
        }

        activeCount = activePositions.Count;

        frames.Add(new CrowdFrame(
            time,
            activePositions,
            activeSpeeds,
            activeCount,
            finishedCount));
    }

    private static bool IsStalledTail(
        IReadOnlyList<CrowdAgentState> agents,
        int totalSpawned,
        int completedCount,
        int activeCount)
    {
        if (activeCount <= 0 || completedCount <= 0 || totalSpawned <= 0)
        {
            return false;
        }

        double activeShare = activeCount / Math.Max(1.0, totalSpawned);
        if (activeShare <= StalledTailMaxActiveShare)
        {
            return true;
        }

        double speedSum = 0.0;
        int stalledCount = 0;
        foreach (CrowdAgentState agent in agents)
        {
            if (agent.IsFinished)
            {
                continue;
            }

            speedSum += agent.Velocity.Length;
            if (agent.StuckDuration >= StalledTailMinimumStuckSeconds)
            {
                stalledCount++;
            }
        }

        double averageSpeed = speedSum / activeCount;
        return stalledCount == activeCount && averageSpeed <= StalledTailAverageSpeedThreshold;
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

        double horizon = Math.Max(0.35, agent.AnticipationTime + (agent.ReactionTime * 0.5));
        double timeToClosest = -Vector3d.Multiply(relativePosition, relativeVelocity) / relativeSpeedSquared;
        if (timeToClosest <= 0.0 || timeToClosest > horizon)
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
        double urgency = 1.0 - (timeToClosest / horizon);
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

    private sealed class AgentSpatialIndex
    {
        private readonly CrowdGrid _grid;
        private readonly Dictionary<(int X, int Y), List<CrowdAgentState>> _buckets = new();

        public AgentSpatialIndex(CrowdGrid grid, IReadOnlyList<CrowdAgentState> agents)
        {
            _grid = grid;
            foreach (CrowdAgentState agent in agents)
            {
                if (agent.IsFinished)
                {
                    continue;
                }

                (int x, int y) = _grid.ToCell(agent.Position);
                if (!_buckets.TryGetValue((x, y), out List<CrowdAgentState>? bucket))
                {
                    bucket = new List<CrowdAgentState>();
                    _buckets[(x, y)] = bucket;
                }

                bucket.Add(agent);
            }
        }

        public IEnumerable<CrowdAgentState> Query(Point3d point, double radius)
        {
            (int x, int y) = _grid.ToCell(point);
            int cellRadius = Math.Max(1, (int)Math.Ceiling(radius / Math.Max(_grid.Floor.CellSize, 1e-6)));

            for (int ix = x - cellRadius; ix <= x + cellRadius; ix++)
            {
                for (int iy = y - cellRadius; iy <= y + cellRadius; iy++)
                {
                    if (_buckets.TryGetValue((ix, iy), out List<CrowdAgentState>? agents))
                    {
                        foreach (CrowdAgentState agent in agents)
                        {
                            yield return agent;
                        }
                    }
                }
            }
        }
    }

    private sealed class PendingAgentUpdate
    {
        public PendingAgentUpdate(CrowdAgentState agent, CrowdExit targetExit, double[,] field)
        {
            Agent = agent;
            TargetExit = targetExit;
            Field = field;
        }

        public CrowdAgentState Agent { get; }

        public CrowdExit TargetExit { get; }

        public double[,] Field { get; }
    }

    private sealed class AgentMotionPlan
    {
        public AgentMotionPlan(CrowdAgentState agent, CrowdExit targetExit, double[,] field, Vector3d desiredVelocity, Vector3d motionVelocity)
        {
            Agent = agent;
            TargetExit = targetExit;
            Field = field;
            DesiredVelocity = desiredVelocity;
            MotionVelocity = motionVelocity;
        }

        public CrowdAgentState Agent { get; }

        public CrowdExit TargetExit { get; }

        public double[,] Field { get; }

        public Vector3d DesiredVelocity { get; }

        public Vector3d MotionVelocity { get; }
    }
}
