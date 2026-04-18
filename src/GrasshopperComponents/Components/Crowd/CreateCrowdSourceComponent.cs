using Crowd.Models;
using Crowd.Services;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GrasshopperComponents.Components.Crowd;

public sealed class CreateCrowdSourceComponent : IndGhComponent
{
    public CreateCrowdSourceComponent()
        : base("Create Crowd Sources", "Src", "Creates source points that emit agents over time.", "INDTools", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("a5a7695a-c3d9-4ed2-95f0-f878d78d9794");

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdSource;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddPointParameter("Locations", "L", "Source locations.", GH_ParamAccess.list);
        pManager.AddIntegerParameter("Total Agents", "N", "Total number of agents emitted per source. Single value is broadcast.", GH_ParamAccess.list, 50);
        pManager.AddNumberParameter("Spawn Rate", "R", "Agents spawned per second. Single value is broadcast.", GH_ParamAccess.list, 4.0);
        pManager.AddIntegerParameter("Exit Index", "E", "Optional target exit index. Use -1 for nearest-exit routing.", GH_ParamAccess.list, -1);
        pManager.AddNumberParameter("Start Time", "T", "Optional source start time in seconds.", GH_ParamAccess.list, 0.0);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Sources", "S", "Crowd source objects.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<Point3d> locations = new();
        List<int> totals = new();
        List<double> rates = new();
        List<int> exitIndices = new();
        List<double> startTimes = new();

        if (!DA.GetDataList(0, locations) || locations.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "At least one source location is required.");
            return;
        }

        DA.GetDataList(1, totals);
        DA.GetDataList(2, rates);
        DA.GetDataList(3, exitIndices);
        DA.GetDataList(4, startTimes);

        List<CrowdSource> sources = new();
        for (int i = 0; i < locations.Count; i++)
        {
            int total = GetValue(totals, i, 50);
            double rate = GetValue(rates, i, 4.0);
            int exitIndex = GetValue(exitIndices, i, -1);
            double startTime = GetValue(startTimes, i, 0.0);

            try
            {
                sources.Add(CrowdModelService.CreateSource(
                    locations[i],
                    total,
                    rate,
                    exitIndex >= 0 ? exitIndex : null,
                    startTime));
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Source [{i}]: {ex.Message}");
            }
        }

        DA.SetDataList(0, sources);
    }

    private static int GetValue(IReadOnlyList<int> values, int index, int fallback)
    {
        if (values.Count == 0)
        {
            return fallback;
        }

        return values[Math.Min(index, values.Count - 1)];
    }

    private static double GetValue(IReadOnlyList<double> values, int index, double fallback)
    {
        if (values.Count == 0)
        {
            return fallback;
        }

        return values[Math.Min(index, values.Count - 1)];
    }
}
