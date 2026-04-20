namespace Crowd.Models;

/// <summary>
/// Collects all inputs required to produce a crowd report from the simulation outputs.
/// </summary>
public sealed class CrowdReportExportRequest
{
    public CrowdReportExportRequest(
        CrowdSimulationResult simulation,
        CrowdHeatmapResult? heatmap,
        string outputPath,
        string templatePath,
        string projectName,
        string siteName,
        string scenarioName,
        string notes,
        string imagePath)
    {
        Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        Heatmap = heatmap;
        OutputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
        TemplatePath = templatePath ?? throw new ArgumentNullException(nameof(templatePath));
        ProjectName = projectName ?? string.Empty;
        SiteName = siteName ?? string.Empty;
        ScenarioName = scenarioName ?? string.Empty;
        Notes = notes ?? string.Empty;
        ImagePath = imagePath ?? string.Empty;
    }

    public CrowdSimulationResult Simulation { get; }

    public CrowdHeatmapResult? Heatmap { get; }

    public string OutputPath { get; }

    public string TemplatePath { get; }

    public string ProjectName { get; }

    public string SiteName { get; }

    public string ScenarioName { get; }

    public string Notes { get; }

    public string ImagePath { get; }
}
