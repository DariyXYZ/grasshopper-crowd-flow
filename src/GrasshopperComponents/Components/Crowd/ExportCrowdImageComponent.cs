using Crowd.Models;
using Crowd.Services;
using Grasshopper;
using Grasshopper.Kernel;
using GrasshopperComponents.Utilities;
using Rhino.Geometry;

namespace GrasshopperComponents.Components.Crowd;

public sealed class ExportCrowdImageComponent : IndGhComponent
{
    private bool _captureLatch;
    private bool _pendingCapture;
    private DateTime _pendingCaptureAtUtc = DateTime.MinValue;
    private const int TriggerStretchMilliseconds = 1200;

    public ExportCrowdImageComponent()
        : base("Export Crowd Image", "ExportImg", "Captures a report-ready PNG from crowd geometry, model content, or heatmap output.", "INDTools", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("f6413379-31da-4234-b2dc-8d2a36af3532");

    protected override bool IsDeveloperOnly => false;

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdExportImage;

    protected override GH_Exposure DefaultExposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Content", "C", "Geometry, CrowdModel, CrowdSimulationResult, or CrowdHeatmapResult to capture.", GH_ParamAccess.list);
        pManager.AddBoxParameter("Frame", "F", "Optional explicit frame box. Leave empty to frame input content automatically.", GH_ParamAccess.list);
        pManager.AddTextParameter("File Path", "Path", "Target PNG file path.", GH_ParamAccess.list);
        pManager.AddIntegerParameter("Width", "W", "Image width in pixels.", GH_ParamAccess.list, 2400);
        pManager.AddIntegerParameter("Height", "H", "Image height in pixels.", GH_ParamAccess.list, 1600);
        pManager.AddNumberParameter("Margin", "M", "Extra framing margin as a fraction of diagonal size.", GH_ParamAccess.list, 0.05);
        pManager.AddBooleanParameter("Top View", "Top", "Use a dedicated temporary top view for stable report capture.", GH_ParamAccess.list, true);
        pManager.AddBooleanParameter("Clean View", "Clean", "Hide grid and axes and apply display mode styling.", GH_ParamAccess.list, true);
        pManager.AddTextParameter("Display Mode", "Mode", "Rhino display mode name, for example Rendered or Shaded.", GH_ParamAccess.list, "Rendered");
        pManager.AddBooleanParameter("Run", "Run", "Set to true to export the image.", GH_ParamAccess.list, false);

        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddBooleanParameter("Saved", "S", "True when the PNG was written successfully.", GH_ParamAccess.item);
        pManager.AddTextParameter("Status", "Status", "Export status message.", GH_ParamAccess.item);
        pManager.AddTextParameter("File Path", "Path", "Resolved PNG file path.", GH_ParamAccess.item);
        pManager.AddBoxParameter("Used Frame", "Frame", "Bounding box used to frame the capture.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<object> contentInputs = new();
        List<Box> frameInputs = new();
        List<string> filePathInputs = new();
        List<int> widthInputs = new();
        List<int> heightInputs = new();
        List<double> marginInputs = new();
        List<bool> topViewInputs = new();
        List<bool> cleanViewInputs = new();
        List<string> displayModeInputs = new();
        List<bool> runInputs = new();

        if (!DA.GetDataList(0, contentInputs) || contentInputs.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Capture content is required.");
            return;
        }

        DA.GetDataList(1, frameInputs);

        DA.GetDataList(2, filePathInputs);
        string filePath = filePathInputs.Count > 0 ? filePathInputs[0] : string.Empty;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Target PNG file path is required.");
            return;
        }

        DA.GetDataList(3, widthInputs);
        DA.GetDataList(4, heightInputs);
        DA.GetDataList(5, marginInputs);
        DA.GetDataList(6, topViewInputs);
        DA.GetDataList(7, cleanViewInputs);
        DA.GetDataList(8, displayModeInputs);
        DA.GetDataList(9, runInputs);

        Box frame = frameInputs.Count > 0 ? frameInputs[0] : Box.Empty;
        int width = widthInputs.Count > 0 ? widthInputs[0] : 2400;
        int height = heightInputs.Count > 0 ? heightInputs[0] : 1600;
        double margin = marginInputs.Count > 0 ? marginInputs[0] : 0.05;
        bool topView = topViewInputs.Count == 0 || topViewInputs[0];
        bool cleanView = cleanViewInputs.Count == 0 || cleanViewInputs[0];
        string displayMode = displayModeInputs.Count > 0 ? displayModeInputs[0] : "Rendered";
        bool run = runInputs.Count > 0 && runInputs[0];

        DateTime nowUtc = DateTime.UtcNow;
        if (run && !_pendingCapture && !_captureLatch)
        {
            _pendingCapture = true;
            _pendingCaptureAtUtc = nowUtc.AddMilliseconds(TriggerStretchMilliseconds);
            ScheduleSelfSolution(TriggerStretchMilliseconds);

            DA.SetData(0, false);
            DA.SetData(1, "Run armed");
            DA.SetData(2, filePath);
            return;
        }

        if (_pendingCapture)
        {
            if (nowUtc < _pendingCaptureAtUtc)
            {
                int remainingMs = Math.Max(1, (int)Math.Ceiling((_pendingCaptureAtUtc - nowUtc).TotalMilliseconds));
                ScheduleSelfSolution(remainingMs);

                DA.SetData(0, false);
                DA.SetData(1, $"Run armed ({remainingMs} ms)");
                DA.SetData(2, filePath);
                return;
            }

            run = true;
            _pendingCapture = false;
        }

        if (!run)
        {
            _captureLatch = false;
            DA.SetData(0, false);
            DA.SetData(1, "Idle");
            DA.SetData(2, filePath);
            return;
        }

        if (_captureLatch)
        {
            DA.SetData(0, false);
            DA.SetData(1, "Waiting for run reset");
            DA.SetData(2, filePath);
            return;
        }

        List<GeometryBase> geometry = CrowdExportGeometryExtraction.ExtractContent(contentInputs);
        if (geometry.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to extract capture geometry from input content.");
            return;
        }

        try
        {
            _captureLatch = true;

            CrowdViewportCaptureOptions options = new(
                geometry,
                filePath,
                frame.IsValid ? frame.BoundingBox : BoundingBox.Empty,
                width,
                height,
                margin,
                topView,
                cleanView,
                displayMode);

            CrowdViewportCaptureResult result = CrowdViewportCaptureService.Capture(options);

            DA.SetData(0, result.Saved);
            DA.SetData(1, result.Status);
            DA.SetData(2, result.FilePath);
            DA.SetData(3, new Box(Plane.WorldXY, result.UsedBounds));
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    private void ScheduleSelfSolution(int delayMilliseconds)
    {
        try
        {
            GH_Document? document = OnPingDocument();
            if (document == null)
            {
                return;
            }

            document.ScheduleSolution(
                Math.Max(1, delayMilliseconds),
                _ => ExpireSolution(false));
        }
        catch
        {
            // Best-effort scheduling to stretch a short run pulse.
        }
    }
}
