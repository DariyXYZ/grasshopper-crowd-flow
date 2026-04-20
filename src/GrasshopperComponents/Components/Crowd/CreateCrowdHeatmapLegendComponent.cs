using Crowd.Models;
using Crowd.Services;
using Grasshopper.Kernel;
using GrasshopperComponents.Utilities;
using Rhino.Geometry;

namespace GrasshopperComponents.Components.Crowd;

public sealed class CreateCrowdHeatmapLegendComponent : IndGhComponent
{
    public CreateCrowdHeatmapLegendComponent()
        : base("Create Crowd Heatmap Legend", "HeatLegend", "Builds a separate horizontal legend for a crowd heatmap.", "INDTools", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("d944e9a5-f307-4a30-a92c-dba52cfa20a8");

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdHeatmapLegend;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Heatmap", "H", "Crowd heatmap result from Create Crowd Heatmap.", GH_ParamAccess.list);
        pManager.AddNumberParameter("Scale", "S", "Legend scale factor.", GH_ParamAccess.list, 1.0);
        pManager.AddNumberParameter("Text Height", "T", "Optional text height for built-in legend labels. Use 0 for auto.", GH_ParamAccess.list, 0.0);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddMeshParameter("Legend Mesh", "M", "Horizontal color legend mesh.", GH_ParamAccess.item);
        pManager.AddGeometryParameter("Label Geometry", "G", "Built-in text geometry for legend labels without separate anchor points.", GH_ParamAccess.list);
        pManager.AddNumberParameter("Minimum Value", "Min", "Minimum value represented by the legend.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Maximum Value", "Max", "Maximum value represented by the legend.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<object> heatmapInputs = new();
        List<double> scaleInputs = new();
        List<double> textHeightInputs = new();

        if (!DA.GetDataList(0, heatmapInputs) || heatmapInputs.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "A crowd heatmap result is required.");
            return;
        }

        DA.GetDataList(1, scaleInputs);
        DA.GetDataList(2, textHeightInputs);

        if (!GhObjectExtraction.TryExtract(heatmapInputs[0], out CrowdHeatmapResult? heatmap) || heatmap == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to extract CrowdHeatmapResult from input.");
            return;
        }

        try
        {
            CrowdHeatmapLegendResult legend = CrowdHeatmapLegendService.Build(
                heatmap,
                scaleInputs.Count > 0 ? scaleInputs[0] : 1.0,
                textHeightInputs.Count > 0 ? textHeightInputs[0] : 0.0);

            DA.SetData(0, legend.Mesh);
            DA.SetDataList(1, legend.LabelGeometry);
            DA.SetData(2, legend.MinimumValue);
            DA.SetData(3, legend.MaximumValue);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }
}
