using Crowd.Models;
using Crowd.Services;
using Grasshopper.Kernel;
using GrasshopperComponents.Utilities;

namespace GrasshopperComponents.Components.Crowd;

public sealed class ExportCrowdReportComponent : IndGhComponent
{
    public ExportCrowdReportComponent()
        : base("Export Crowd Report", "ExportPdf", "Builds a DOCX/PDF crowd report from the simulation result, heatmap, and exported image.", "Crowd Flow", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("516b4380-1c3a-4a88-9268-1934fb9f1bd6");

    protected override bool IsDeveloperOnly => false;

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdExportReport;

    protected override GH_Exposure DefaultExposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Result", "R", "CrowdSimulationResult from Run Crowd Simulation.", GH_ParamAccess.list);
        pManager.AddGenericParameter("Heatmap", "H", "Optional CrowdHeatmapResult for report legend labels and value range.", GH_ParamAccess.list);
        pManager.AddTextParameter("Image Path", "Img", "Optional PNG path exported by Export Crowd Image.", GH_ParamAccess.list, string.Empty);
        pManager.AddTextParameter("Output Path", "Out", "Target report path. You can pass a base path, .docx, or .pdf path.", GH_ParamAccess.list);
        pManager.AddTextParameter("Template Path", "Tpl", "DOCX template path for the report layout.", GH_ParamAccess.list);
        pManager.AddTextParameter("Project Name", "Project", "Report project title.", GH_ParamAccess.list, string.Empty);
        pManager.AddTextParameter("Site Name", "Site", "Project site or area name.", GH_ParamAccess.list, string.Empty);
        pManager.AddTextParameter("Scenario Name", "Scenario", "Scenario or option name shown in the report.", GH_ParamAccess.list, "Base scenario");
        pManager.AddTextParameter("Notes", "Notes", "Optional architect-facing note block.", GH_ParamAccess.list, string.Empty);
        pManager.AddBooleanParameter("Run", "Run", "Set to true to export the report.", GH_ParamAccess.list, false);

        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("DOCX Path", "DOCX", "Generated DOCX report path.", GH_ParamAccess.item);
        pManager.AddTextParameter("PDF Path", "PDF", "Generated PDF report path.", GH_ParamAccess.item);
        pManager.AddBooleanParameter("PDF Created", "PDF?", "True when the PDF was exported successfully.", GH_ParamAccess.item);
        pManager.AddTextParameter("Status", "Status", "Report export status message.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<object> resultInputs = new();
        List<object> heatmapInputs = new();
        List<string> imagePathInputs = new();
        List<string> outputPathInputs = new();
        List<string> templatePathInputs = new();
        List<string> projectNameInputs = new();
        List<string> siteNameInputs = new();
        List<string> scenarioNameInputs = new();
        List<string> notesInputs = new();
        List<bool> runInputs = new();

        if (!DA.GetDataList(0, resultInputs) || resultInputs.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Crowd simulation result is required.");
            return;
        }

        DA.GetDataList(1, heatmapInputs);
        DA.GetDataList(2, imagePathInputs);
        DA.GetDataList(3, outputPathInputs);
        DA.GetDataList(4, templatePathInputs);
        DA.GetDataList(5, projectNameInputs);
        DA.GetDataList(6, siteNameInputs);
        DA.GetDataList(7, scenarioNameInputs);
        DA.GetDataList(8, notesInputs);
        DA.GetDataList(9, runInputs);

        string imagePath = imagePathInputs.Count > 0 ? imagePathInputs[0] : string.Empty;
        string outputPath = outputPathInputs.Count > 0 ? outputPathInputs[0] : string.Empty;
        string templatePath = templatePathInputs.Count > 0 ? templatePathInputs[0] : string.Empty;
        string projectName = projectNameInputs.Count > 0 ? projectNameInputs[0] : string.Empty;
        string siteName = siteNameInputs.Count > 0 ? siteNameInputs[0] : string.Empty;
        string scenarioName = scenarioNameInputs.Count > 0 ? scenarioNameInputs[0] : "Base scenario";
        string notes = notesInputs.Count > 0 ? notesInputs[0] : string.Empty;
        bool run = runInputs.Count > 0 && runInputs[0];

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Target report path is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(templatePath))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "DOCX template path is required.");
            return;
        }

        if (!run)
        {
            DA.SetData(2, false);
            DA.SetData(3, "Idle");
            return;
        }

        if (!GhObjectExtraction.TryExtract(resultInputs[0], out CrowdSimulationResult? simulation) || simulation == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to extract CrowdSimulationResult from input.");
            return;
        }

        CrowdHeatmapResult? heatmap = null;
        if (heatmapInputs.Count > 0)
        {
            GhObjectExtraction.TryExtract(heatmapInputs[0], out heatmap);
        }

        try
        {
            CrowdReportExportRequest request = new(
                simulation,
                heatmap,
                outputPath,
                templatePath,
                projectName,
                siteName,
                scenarioName,
                notes,
                imagePath);

            CrowdReportExportResult result = CrowdReportExportService.Export(request);

            DA.SetData(0, result.DocxPath);
            DA.SetData(1, result.PdfPath);
            DA.SetData(2, result.PdfCreated);
            DA.SetData(3, result.Status);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }
}
