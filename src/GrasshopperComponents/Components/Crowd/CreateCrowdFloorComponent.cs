using Crowd.Models;
using Crowd.Services;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GrasshopperComponents.Components.Crowd;

public sealed class CreateCrowdFloorComponent : IndGhComponent
{
    public CreateCrowdFloorComponent()
        : base("Create Crowd Floor", "Floor", "Creates a walkable floor for crowd simulation from a closed planar boundary.", "Crowd Flow", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("2b2a7a76-c861-4d2f-84a7-30cabd5db717");

    protected override bool IsDeveloperOnly => false;

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdFloor;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter("Boundary", "B", "Closed planar floor boundary.", GH_ParamAccess.list);
        pManager.AddNumberParameter("Cell Size", "C", "Grid resolution for routing and occupancy checks.", GH_ParamAccess.list, 0.75);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Floor", "F", "Crowd floor object.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<Curve> boundaries = new();
        List<double> cellSizes = new();

        if (!DA.GetDataList(0, boundaries) || boundaries.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "At least one floor boundary is required.");
            return;
        }

        DA.GetDataList(1, cellSizes);
        double cellSize = cellSizes.Count > 0 ? cellSizes[0] : 0.75;

        try
        {
            CrowdFloor floor = CrowdModelService.CreateFloor(boundaries[0], cellSize);
            DA.SetData(0, floor);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }
}
