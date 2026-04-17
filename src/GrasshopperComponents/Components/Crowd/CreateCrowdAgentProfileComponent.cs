using Crowd.Models;
using Crowd.Services;
using Grasshopper.Kernel;

namespace GrasshopperComponents.Components.Crowd;

public sealed class CreateCrowdAgentProfileComponent : IndGhComponent
{
    public CreateCrowdAgentProfileComponent()
        : base("Create Crowd Agent Profile", "Agent", "Creates reusable agent motion settings for the crowd solver.", "GhCrowdFlow", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("44908e5f-4962-46c2-bfb1-764759c81066");

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdAgent;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Radius", "R", "Approximate personal body radius in model units.", GH_ParamAccess.list, 0.35);
        pManager.AddNumberParameter("Preferred Speed", "S", "Preferred walking speed in units per second.", GH_ParamAccess.list, 1.35);
        pManager.AddNumberParameter("Max Speed", "M", "Maximum allowed speed in units per second.", GH_ParamAccess.list, 1.8);
        pManager.AddNumberParameter("Separation Weight", "W", "Strength of local avoidance when agents get close.", GH_ParamAccess.list, 1.4);
        pManager.AddNumberParameter("Arrival Threshold", "A", "Distance at which an agent counts as having reached an exit.", GH_ParamAccess.list, 0.5);
        pManager.AddNumberParameter("Variation Percent", "Var", "Per-agent variation applied around the supplied values. Use 0.1 for +/-10%.", GH_ParamAccess.list, 0.14);
        pManager.AddNumberParameter("Steering Noise", "N", "Small directional noise to avoid over-deterministic movement. Range 0..1.", GH_ParamAccess.list, 0.12);
        pManager.AddNumberParameter("Density Weight", "D", "How strongly agents avoid locally crowded areas.", GH_ParamAccess.list, 1.05);
        pManager.AddNumberParameter("Spawn Jitter", "J", "Randomized spawn offset in model units.", GH_ParamAccess.list, 0.95);
        pManager.AddNumberParameter("Exit Choice Randomness", "E", "How imperfectly agents estimate the best exit. Higher values make exit choice less deterministic. Range 0..1.", GH_ParamAccess.list, 0.28);
        pManager.AddNumberParameter("Congestion Sensitivity", "C", "How strongly agents prefer less crowded exits and avoid queues.", GH_ParamAccess.list, 1.2);
        pManager.AddNumberParameter("Exit Commitment", "K", "How reluctant agents are to switch exits after making a choice. Range 0..1.", GH_ParamAccess.list, 0.64);
        pManager.AddNumberParameter("Reassessment Interval", "T", "Seconds between route reconsideration checks.", GH_ParamAccess.list, 1.35);
        pManager.AddNumberParameter("Wall Avoidance", "Wa", "Comfort buffer from walls and obstacles. Higher values keep people further from edges and columns.", GH_ParamAccess.list, 1.25);
        pManager.AddNumberParameter("Turn Anticipation", "Ta", "How early agents start bending into turns instead of making angular corrections.", GH_ParamAccess.list, 1.45);
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
        List<double> separationWeights = new();
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
        List<double> turnAnticipationValues = new();

        DA.GetDataList(0, radii);
        DA.GetDataList(1, preferredSpeeds);
        DA.GetDataList(2, maxSpeeds);
        DA.GetDataList(3, separationWeights);
        DA.GetDataList(4, arrivalThresholds);
        DA.GetDataList(5, variationPercents);
        DA.GetDataList(6, steeringNoiseValues);
        DA.GetDataList(7, densityWeights);
        DA.GetDataList(8, spawnJitters);
        DA.GetDataList(9, exitChoiceRandomnessValues);
        DA.GetDataList(10, congestionSensitivityValues);
        DA.GetDataList(11, exitCommitmentValues);
        DA.GetDataList(12, reassessmentIntervals);
        DA.GetDataList(13, wallAvoidanceValues);
        DA.GetDataList(14, turnAnticipationValues);

        try
        {
            CrowdAgentProfile profile = CrowdModelService.CreateAgentProfile(
                radii.Count > 0 ? radii[0] : 0.35,
                preferredSpeeds.Count > 0 ? preferredSpeeds[0] : 1.35,
                maxSpeeds.Count > 0 ? maxSpeeds[0] : 1.8,
                separationWeights.Count > 0 ? separationWeights[0] : 1.4,
                arrivalThresholds.Count > 0 ? arrivalThresholds[0] : 0.5,
                variationPercents.Count > 0 ? variationPercents[0] : 0.14,
                steeringNoiseValues.Count > 0 ? steeringNoiseValues[0] : 0.12,
                densityWeights.Count > 0 ? densityWeights[0] : 1.05,
                spawnJitters.Count > 0 ? spawnJitters[0] : 0.95,
                exitChoiceRandomnessValues.Count > 0 ? exitChoiceRandomnessValues[0] : 0.28,
                congestionSensitivityValues.Count > 0 ? congestionSensitivityValues[0] : 1.2,
                exitCommitmentValues.Count > 0 ? exitCommitmentValues[0] : 0.64,
                reassessmentIntervals.Count > 0 ? reassessmentIntervals[0] : 1.35,
                wallAvoidanceValues.Count > 0 ? wallAvoidanceValues[0] : 1.25,
                turnAnticipationValues.Count > 0 ? turnAnticipationValues[0] : 1.45);

            DA.SetData(0, profile);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }
}
