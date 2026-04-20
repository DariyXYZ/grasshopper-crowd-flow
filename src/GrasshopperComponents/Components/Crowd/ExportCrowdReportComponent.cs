using Crowd.Models;
using Crowd.Services;
using Grasshopper.Kernel;
using GrasshopperComponents.Utilities;

namespace GrasshopperComponents.Components.Crowd;

public sealed class ExportCrowdReportComponent : IndGhComponent
{
    public ExportCrowdReportComponent()
        : base("Export Crowd Report", "ExportPdf", "Builds a DOCX/PDF crowd report from the simulation result, heatmap, and exported image.", "INDTools", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("516b4380-1c3a-4a88-9268-1934fb9f1bd6");

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdExportReport;

    protected override GH_Exposure DefaultExposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Result", "R", "CrowdSimulationResult from Run Crowd Simulation.", GH_ParamAccess.item);
        pManager.AddGenericParameter("Heatmap", "H", "Optional CrowdHeatmapResult for report legend labels and value range.", GH_ParamAccess.item);
        pManager.AddTextParameter("Image Path", "Img", "Optional PNG path exported by Export Crowd Image.", GH_ParamAccess.item, string.Empty);
        pManager.AddTextParameter("Output Path", "Out", "Target report path. You can pass a base path, .docx, or .pdf path.", GH_ParamAccess.item);
        pManager.AddTextParameter("Template Path", "Tpl", "DOCX template path for the report layout.", GH_ParamAccess.item);
        pManager.AddTextParameter("Project Name", "Project", "Report project title.", GH_ParamAccess.item, string.Empty);
        pManager.AddTextParameter("Site Name", "Site", "Project site or area name.", GH_ParamAccess.item, string.Empty);
        pManager.AddTextParameter("Scenario Name", "Scenario", "Scenario or option name shown in the report.", GH_ParamAccess.item, "Base scenario");
        pManager.AddTextParameter("Notes", "Notes", "Optional architect-facing note block.", GH_ParamAccess.item, string.Empty);
        pManager.AddBooleanParameter("Run", "Run", "Set to true to export the report.", GH_ParamAccess.item, false);

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
        object? resultInput = null;
        object? heatmapInput = null;
        string imagePath = string.Empty;
        string outputPath = string.Empty;
        string templatePath = string.Empty;
        string projectName = string.Empty;
        string siteName = string.Empty;
        string scenarioName = "Base scenario";
        string notes = string.Empty;
        bool run = false;

        if (!DA.GetData(0, ref resultInput) || resultInput == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Crowd simulation result is required.");
            return;
        }

        DA.GetData(1, ref heatmapInput);
        DA.GetData(2, ref imagePath);

        if (!DA.GetData(3, ref outputPath) || string.IsNullOrWhiteSpace(outputPath))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Target report path is required.");
            return;
        }

        if (!DA.GetData(4, ref templatePath) || string.IsNullOrWhiteSpace(templatePath))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "DOCX template path is required.");
            return;
        }

        DA.GetData(5, ref projectName);
        DA.GetData(6, ref siteName);
        DA.GetData(7, ref scenarioName);
        DA.GetData(8, ref notes);
        DA.GetData(9, ref run);

        if (!run)
        {
            DA.SetData(2, false);
            DA.SetData(3, "Idle");
            return;
        }

        if (!GhObjectExtraction.TryExtract(resultInput, out CrowdSimulationResult? simulation) || simulation == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to extract CrowdSimulationResult from input.");
            return;
        }

        CrowdHeatmapResult? heatmap = null;
        if (heatmapInput != null)
        {
            GhObjectExtraction.TryExtract(heatmapInput, out heatmap);
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
