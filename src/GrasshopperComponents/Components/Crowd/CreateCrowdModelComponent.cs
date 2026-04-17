using Crowd.Models;
using Crowd.Services;
using Grasshopper.Kernel;
using GrasshopperComponents.Utilities;

namespace GrasshopperComponents.Components.Crowd;

public sealed class CreateCrowdModelComponent : IndGhComponent
{
    public CreateCrowdModelComponent()
        : base("Create Crowd Model", "Model", "Combines floor, sources, obstacles, exits, and agent settings into a crowd model.", "GhCrowdFlow", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("54d2cc44-b66f-411a-a036-f923d6f2f993");

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdModel;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Floor", "F", "Crowd floor object.", GH_ParamAccess.list);
        pManager.AddGenericParameter("Obstacles", "O", "Optional crowd obstacle objects.", GH_ParamAccess.list);
        pManager.AddGenericParameter("Sources", "S", "Crowd source objects.", GH_ParamAccess.list);
        pManager.AddGenericParameter("Exits", "E", "Crowd exit objects.", GH_ParamAccess.list);
        pManager.AddGenericParameter("Agent Profile", "P", "Optional crowd agent profile object.", GH_ParamAccess.list);
        pManager.AddNumberParameter("Time Step", "T", "Simulation step in seconds.", GH_ParamAccess.list, 0.2);

        pManager[1].Optional = true;
        pManager[4].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Crowd Model", "M", "Crowd model ready for simulation.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<object> floorInputs = new();
        List<object> obstacleInputs = new();
        List<object> sourceInputs = new();
        List<object> exitInputs = new();
        List<object> profileInputs = new();
        List<double> timeSteps = new();

        if (!DA.GetDataList(0, floorInputs) || floorInputs.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "A crowd floor is required.");
            return;
        }

        DA.GetDataList(1, obstacleInputs);
        if (!DA.GetDataList(2, sourceInputs) || sourceInputs.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "At least one source is required.");
            return;
        }

        if (!DA.GetDataList(3, exitInputs) || exitInputs.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "At least one exit is required.");
            return;
        }

        DA.GetDataList(4, profileInputs);
        DA.GetDataList(5, timeSteps);

        if (!GhObjectExtraction.TryExtract(floorInputs[0], out CrowdFloor? floor) || floor == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to extract CrowdFloor from input.");
            return;
        }

        List<CrowdObstacle> obstacles = ExtractMany<CrowdObstacle>(obstacleInputs);
        List<CrowdSource> sources = ExtractMany<CrowdSource>(sourceInputs);
        List<CrowdExit> exits = ExtractMany<CrowdExit>(exitInputs);
        CrowdAgentProfile? profile = profileInputs.Count > 0 && GhObjectExtraction.TryExtract(profileInputs[0], out CrowdAgentProfile? extractedProfile)
            ? extractedProfile
            : null;

        try
        {
            CrowdModel model = CrowdModelService.CreateModel(
                floor,
                obstacles,
                sources,
                exits,
                profile,
                timeSteps.Count > 0 ? timeSteps[0] : 0.2);

            DA.SetData(0, model);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    private static List<T> ExtractMany<T>(IEnumerable<object> inputs)
        where T : class
    {
        List<T> items = new();
        foreach (object input in inputs)
        {
            if (GhObjectExtraction.TryExtract(input, out T? value) && value != null)
            {
                items.Add(value);
            }
        }

        return items;
    }
}
