using Crowd.Models;
using Crowd.Services;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GrasshopperComponents.Components.Crowd;

public sealed class CreateCrowdExitComponent : IndGhComponent
{
    public CreateCrowdExitComponent()
        : base("Create Crowd Exits", "Exit", "Creates destination exits for the crowd solver.", "INDTools", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("d547666c-d67d-42e0-b3ab-ae5bd7876e6d");

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdExit;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddPointParameter("Locations", "L", "Exit locations.", GH_ParamAccess.list);
        pManager.AddNumberParameter("Radius", "R", "Capture radius for finishing an agent route.", GH_ParamAccess.list, 1.0);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Exits", "E", "Crowd exit objects.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<Point3d> locations = new();
        List<double> radii = new();
        if (!DA.GetDataList(0, locations) || locations.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "At least one exit location is required.");
            return;
        }

        DA.GetDataList(1, radii);
        List<CrowdExit> exits = new();
        for (int i = 0; i < locations.Count; i++)
        {
            double radius = radii.Count == 0 ? 1.0 : radii[Math.Min(i, radii.Count - 1)];
            try
            {
                exits.Add(CrowdModelService.CreateExit(locations[i], radius));
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Exit [{i}]: {ex.Message}");
            }
        }

        DA.SetDataList(0, exits);
    }
}
