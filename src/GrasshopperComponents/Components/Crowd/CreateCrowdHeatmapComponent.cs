using Crowd.Models;
using Crowd.Services;
using Grasshopper.Kernel;
using GrasshopperComponents.Utilities;

namespace GrasshopperComponents.Components.Crowd;

public sealed class CreateCrowdHeatmapComponent : IndGhComponent
{
    public CreateCrowdHeatmapComponent()
        : base("Create Crowd Heatmap", "Heat", "Builds a colored metric heatmap from simulated movement for architectural analysis and reporting.", "INDTools", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("71663952-a28d-4514-bfa8-cf91d70f6fcb");

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdHeatmap;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Result", "R", "Crowd simulation result from Run Crowd Simulation.", GH_ParamAccess.list);
        pManager.AddTextParameter("Mode", "Mode", "Heatmap mode: Occupancy, Density, Throughput, Speed, or Congestion.", GH_ParamAccess.list, "Occupancy");
        pManager.AddIntegerParameter("Smoothing", "S", "Number of smoothing passes on the heat field.", GH_ParamAccess.list, 2);
        pManager.AddNumberParameter("Height Scale", "H", "Optional height exaggeration for the heat mesh. Use 0 for a flat map.", GH_ParamAccess.list, 0.0);
        pManager.AddBooleanParameter("Normalize", "N", "Normalize values by frame count for easier scenario comparison.", GH_ParamAccess.list, true);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Heatmap", "H", "Crowd heatmap result for downstream legend or reporting nodes.", GH_ParamAccess.item);
        pManager.AddMeshParameter("Heatmap Mesh", "M", "Colored mesh heatmap of the resolved metric.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Values", "V", "Metric value per output cell.", GH_ParamAccess.list);
        pManager.AddNumberParameter("Minimum Value", "Min", "Minimum heat value in the field.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Peak Value", "P", "Maximum heat value in the field.", GH_ParamAccess.item);
        pManager.AddTextParameter("Resolved Mode", "Mode", "Resolved heatmap mode used for calculation.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<object> resultInputs = new();
        List<string> modeInputs = new();
        List<int> smoothingInputs = new();
        List<double> heightInputs = new();
        List<bool> normalizeInputs = new();

        if (!DA.GetDataList(0, resultInputs) || resultInputs.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "A crowd simulation result is required.");
            return;
        }

        DA.GetDataList(1, modeInputs);
        DA.GetDataList(2, smoothingInputs);
        DA.GetDataList(3, heightInputs);
        DA.GetDataList(4, normalizeInputs);

        if (!GhObjectExtraction.TryExtract(resultInputs[0], out CrowdSimulationResult? result) || result == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to extract CrowdSimulationResult from input.");
            return;
        }

        try
        {
            CrowdHeatmapResult heatmap = CrowdHeatmapService.Build(
                result,
                modeInputs.Count > 0 ? modeInputs[0] : "Occupancy",
                smoothingInputs.Count > 0 ? smoothingInputs[0] : 2,
                heightInputs.Count > 0 ? heightInputs[0] : 0.0,
                normalizeInputs.Count > 0 ? normalizeInputs[0] : true);

            DA.SetData(0, heatmap);
            DA.SetData(1, heatmap.Mesh);
            DA.SetDataList(2, heatmap.Values);
            DA.SetData(3, heatmap.MinimumValue);
            DA.SetData(4, heatmap.PeakValue);
            DA.SetData(5, heatmap.Mode);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }
}
