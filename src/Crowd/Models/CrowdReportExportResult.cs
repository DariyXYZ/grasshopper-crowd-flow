namespace Crowd.Models;

/// <summary>
/// Describes the generated report artifact paths and export status.
/// </summary>
public sealed class CrowdReportExportResult
{
    public CrowdReportExportResult(string docxPath, string pdfPath, bool pdfCreated, string status)
    {
        DocxPath = docxPath ?? throw new ArgumentNullException(nameof(docxPath));
        PdfPath = pdfPath ?? throw new ArgumentNullException(nameof(pdfPath));
        PdfCreated = pdfCreated;
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    public string DocxPath { get; }

    public string PdfPath { get; }

    public bool PdfCreated { get; }

    public string Status { get; }
}
