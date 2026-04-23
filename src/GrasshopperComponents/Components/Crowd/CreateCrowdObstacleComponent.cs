using Crowd.Models;
using Crowd.Services;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GrasshopperComponents.Components.Crowd;

public sealed class CreateCrowdObstacleComponent : IndGhComponent
{
    public CreateCrowdObstacleComponent()
        : base("Create Crowd Obstacles", "Obs", "Creates obstacle regions that agents must navigate around.", "INDTools", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("97748331-7f0b-41dd-a25f-0b3d68e7b00c");

    protected override bool IsDeveloperOnly => false;

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdObstacle;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter("Boundaries", "B", "Closed obstacle boundaries.", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Obstacles", "O", "Crowd obstacle objects.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<Curve> boundaries = new();
        if (!DA.GetDataList(0, boundaries) || boundaries.Count == 0)
        {
            DA.SetDataList(0, Array.Empty<CrowdObstacle>());
            return;
        }

        List<CrowdObstacle> obstacles = new();
        for (int i = 0; i < boundaries.Count; i++)
        {
            try
            {
                obstacles.Add(CrowdModelService.CreateObstacle(boundaries[i]));
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Obstacle [{i}]: {ex.Message}");
            }
        }

        DA.SetDataList(0, obstacles);
    }
}
