using Crowd.Models;
using Crowd.Services;
using Grasshopper.Kernel;

namespace GrasshopperComponents.Components.Crowd;

public sealed class CreateCrowdAgentProfileComponent : IndGhComponent
{
    public CreateCrowdAgentProfileComponent()
        : base("Create Crowd Agent Profile", "Agent", "Creates reusable agent motion settings for the crowd solver.", "INDTools", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("44908e5f-4962-46c2-bfb1-764759c81066");

    protected override bool IsDeveloperOnly => false;

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdAgent;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Radius", "R", "Approximate personal body radius in model units.", GH_ParamAccess.list, 0.35);
        pManager.AddNumberParameter("Preferred Speed", "S", "Preferred walking speed in units per second.", GH_ParamAccess.list, 1.35);
        pManager.AddNumberParameter("Max Speed", "M", "Maximum allowed speed in units per second.", GH_ParamAccess.list, 1.8);
        pManager.AddNumberParameter("Time Gap", "Gap", "Desired temporal spacing to the pedestrian ahead. Higher values encourage safer following distances.", GH_ParamAccess.list, 1.15);
        pManager.AddNumberParameter("Reaction Time", "Rt", "How quickly agents turn and respond to changing local conflicts.", GH_ParamAccess.list, 0.42);
        pManager.AddNumberParameter("Anticipation Time", "At", "Look-ahead horizon for predicting near-future conflicts.", GH_ParamAccess.list, 1.0);
        pManager.AddNumberParameter("Separation Weight", "W", "Strength of local avoidance when agents get close.", GH_ParamAccess.list, 1.35);
        pManager.AddNumberParameter("Neighbor Repulsion Strength", "Ns", "How strongly nearby pedestrians push route choice and steering away from them.", GH_ParamAccess.list, 1.05);
        pManager.AddNumberParameter("Neighbor Repulsion Range", "Nr", "Distance over which neighboring pedestrians influence steering.", GH_ParamAccess.list, 1.2);
        pManager.AddNumberParameter("Comfort Distance", "Cd", "Preferred interpersonal buffer around each pedestrian.", GH_ParamAccess.list, 0.55);
        pManager.AddNumberParameter("Arrival Threshold", "A", "Distance at which an agent counts as having reached an exit.", GH_ParamAccess.list, 0.5);
        pManager.AddNumberParameter("Variation Percent", "Var", "Per-agent variation applied around the supplied values. Use 0.1 for +/-10%.", GH_ParamAccess.list, 0.16);
        pManager.AddNumberParameter("Steering Noise", "N", "Small directional noise to avoid over-deterministic movement. Range 0..1.", GH_ParamAccess.list, 0.08);
        pManager.AddNumberParameter("Density Weight", "D", "How strongly agents avoid locally crowded areas.", GH_ParamAccess.list, 0.95);
        pManager.AddNumberParameter("Spawn Jitter", "J", "Randomized spawn offset in model units.", GH_ParamAccess.list, 0.8);
        pManager.AddNumberParameter("Exit Choice Randomness", "E", "How imperfectly agents estimate the best exit. Higher values make exit choice less deterministic. Range 0..1.", GH_ParamAccess.list, 0.24);
        pManager.AddNumberParameter("Congestion Sensitivity", "C", "How strongly agents prefer less crowded exits and avoid queues.", GH_ParamAccess.list, 1.1);
        pManager.AddNumberParameter("Exit Commitment", "K", "How reluctant agents are to switch exits after making a choice. Range 0..1.", GH_ParamAccess.list, 0.66);
        pManager.AddNumberParameter("Reassessment Interval", "T", "Seconds between route reconsideration checks.", GH_ParamAccess.list, 1.35);
        pManager.AddNumberParameter("Wall Avoidance", "Wa", "Comfort buffer from walls and obstacles. Higher values keep people further from edges and columns.", GH_ParamAccess.list, 1.1);
        pManager.AddNumberParameter("Wall Buffer Distance", "Wb", "Preferred gliding offset from walls before repulsion sharply increases.", GH_ParamAccess.list, 0.22);
        pManager.AddNumberParameter("Turn Anticipation", "Ta", "How early agents start bending into turns instead of making angular corrections.", GH_ParamAccess.list, 1.45);
        pManager.AddNumberParameter("Preferred Side Bias", "Sb", "Bias toward spontaneous left/right lane preference in counterflow. Range 0..1.", GH_ParamAccess.list, 0.16);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Agent Profile", "P", "Crowd agent profile object.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<double> radii = new();
        List<double> preferredSpeeds = new();
        List<double> maxSpeeds = new();
        List<double> timeGaps = new();
        List<double> reactionTimes = new();
        List<double> anticipationTimes = new();
        List<double> separationWeights = new();
        List<double> neighborRepulsionStrengths = new();
        List<double> neighborRepulsionRanges = new();
        List<double> comfortDistances = new();
        List<double> arrivalThresholds = new();
        List<double> variationPercents = new();
        List<double> steeringNoiseValues = new();
        List<double> densityWeights = new();
        List<double> spawnJitters = new();
        List<double> exitChoiceRandomnessValues = new();
        List<double> congestionSensitivityValues = new();
        List<double> exitCommitmentValues = new();
        List<double> reassessmentIntervals = new();
        List<double> wallAvoidanceValues = new();
        List<double> wallBufferDistanceValues = new();
        List<double> turnAnticipationValues = new();
        List<double> preferredSideBiasValues = new();

        DA.GetDataList(0, radii);
        DA.GetDataList(1, preferredSpeeds);
        DA.GetDataList(2, maxSpeeds);
        DA.GetDataList(3, timeGaps);
        DA.GetDataList(4, reactionTimes);
        DA.GetDataList(5, anticipationTimes);
        DA.GetDataList(6, separationWeights);
        DA.GetDataList(7, neighborRepulsionStrengths);
        DA.GetDataList(8, neighborRepulsionRanges);
        DA.GetDataList(9, comfortDistances);
        DA.GetDataList(10, arrivalThresholds);
        DA.GetDataList(11, variationPercents);
        DA.GetDataList(12, steeringNoiseValues);
        DA.GetDataList(13, densityWeights);
        DA.GetDataList(14, spawnJitters);
        DA.GetDataList(15, exitChoiceRandomnessValues);
        DA.GetDataList(16, congestionSensitivityValues);
        DA.GetDataList(17, exitCommitmentValues);
        DA.GetDataList(18, reassessmentIntervals);
        DA.GetDataList(19, wallAvoidanceValues);
        DA.GetDataList(20, wallBufferDistanceValues);
        DA.GetDataList(21, turnAnticipationValues);
        DA.GetDataList(22, preferredSideBiasValues);

        try
        {
            CrowdAgentProfile profile = CrowdModelService.CreateAgentProfile(
                radii.Count > 0 ? radii[0] : 0.35,
                preferredSpeeds.Count > 0 ? preferredSpeeds[0] : 1.35,
                maxSpeeds.Count > 0 ? maxSpeeds[0] : 1.8,
                timeGaps.Count > 0 ? timeGaps[0] : 1.15,
                reactionTimes.Count > 0 ? reactionTimes[0] : 0.42,
                anticipationTimes.Count > 0 ? anticipationTimes[0] : 1.0,
                separationWeights.Count > 0 ? separationWeights[0] : 1.35,
                neighborRepulsionStrengths.Count > 0 ? neighborRepulsionStrengths[0] : 1.05,
                neighborRepulsionRanges.Count > 0 ? neighborRepulsionRanges[0] : 1.2,
                comfortDistances.Count > 0 ? comfortDistances[0] : 0.55,
                arrivalThresholds.Count > 0 ? arrivalThresholds[0] : 0.5,
                variationPercents.Count > 0 ? variationPercents[0] : 0.16,
                steeringNoiseValues.Count > 0 ? steeringNoiseValues[0] : 0.08,
                densityWeights.Count > 0 ? densityWeights[0] : 0.95,
                spawnJitters.Count > 0 ? spawnJitters[0] : 0.8,
                exitChoiceRandomnessValues.Count > 0 ? exitChoiceRandomnessValues[0] : 0.24,
                congestionSensitivityValues.Count > 0 ? congestionSensitivityValues[0] : 1.1,
                exitCommitmentValues.Count > 0 ? exitCommitmentValues[0] : 0.66,
                reassessmentIntervals.Count > 0 ? reassessmentIntervals[0] : 1.35,
                wallAvoidanceValues.Count > 0 ? wallAvoidanceValues[0] : 1.1,
                wallBufferDistanceValues.Count > 0 ? wallBufferDistanceValues[0] : 0.22,
                turnAnticipationValues.Count > 0 ? turnAnticipationValues[0] : 1.45,
                preferredSideBiasValues.Count > 0 ? preferredSideBiasValues[0] : 0.16);

            DA.SetData(0, profile);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }
}
