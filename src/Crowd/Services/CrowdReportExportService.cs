using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.IO.Compression;
using System.Xml.Linq;
using Crowd.Models;

namespace Crowd.Services;

/// <summary>
/// Produces DOCX and PDF crowd reports from the reporting template and simulation outputs.
/// </summary>
public static class CrowdReportExportService
{
    private const int WdExportFormatPdf = 17;
    private const int WdFindContinue = 1;
    private const int WdReplaceAll = 2;
    private const int WdCollapseEnd = 0;
    private const int WdTrue = -1;
    private struct PeakMetricDescriptor
    {
        public PeakMetricDescriptor(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }

        public string Value { get; }
    }

    /// <summary>
    /// Exports a crowd report using the prepared report template and current simulation metrics.
    /// </summary>
    /// <param name="request">Report export inputs, metadata, and artifact paths.</param>
    /// <returns>Generated report paths and status text.</returns>
    public static CrowdReportExportResult Export(CrowdReportExportRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        string templatePath = NormalizeFileReference(request.TemplatePath);
        string imagePath = NormalizeFileReference(request.ImagePath);

        if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
        {
            throw new InvalidOperationException("Valid DOCX template path is required.");
        }

        string basePath = NormalizeBasePath(request.OutputPath, request.ScenarioName, request.ProjectName);
        string docxPath = $"{basePath}.docx";
        string pdfPath = $"{basePath}.pdf";
        string? directory = Path.GetDirectoryName(docxPath);

        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        bool docxCreated = BuildReportFromWordTemplate(request, templatePath, imagePath, docxPath, pdfPath);
        bool pdfCreated = File.Exists(pdfPath);
        string status = pdfCreated
            ? $"Report exported: {docxPath} and {pdfPath}"
            : docxCreated
                ? $"DOCX exported: {docxPath}. PDF export failed."
                : "Report export failed.";

        return new CrowdReportExportResult(docxPath, pdfPath, pdfCreated, status);
    }

    private static bool BuildReportFromWordTemplate(
        CrowdReportExportRequest request,
        string templatePath,
        string imagePath,
        string outputDocxPath,
        string outputPdfPath)
    {
        Type? wordType = GetWordApplicationType();
        if (wordType == null)
        {
            throw new InvalidOperationException("Microsoft Word is not installed or COM is unavailable.");
        }

        object? wordApplication = null;
        object? document = null;

        try
        {
            PrepareOutputDocxFromTemplate(templatePath, outputDocxPath);
            NormalizeOutputDocxTemplate(outputDocxPath, request);

            wordApplication = Activator.CreateInstance(wordType);
            if (wordApplication == null)
            {
                throw new InvalidOperationException("Failed to start Microsoft Word.");
            }

            SetProperty(wordApplication, "Visible", false);
            SetProperty(wordApplication, "DisplayAlerts", 0);

            object documents = GetProperty(wordApplication, "Documents");
            object missing = Type.Missing;

            document = InvokeMethod(
                documents,
                "Open",
                outputDocxPath,
                missing,
                false,
                false,
                missing,
                missing,
                missing,
                missing,
                missing,
                missing,
                false,
                missing,
                missing,
                missing,
                missing);

            ApplyTemplateData(document, request);

            bool imageInserted = false;
            if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
            {
                imageInserted = InsertTemplateImage(document, imagePath, 170.0);
            }

            if (!imageInserted)
            {
                ReplaceLongTextToken(
                    document,
                    "{{HEATMAP_IMAGE}}",
                    string.IsNullOrWhiteSpace(imagePath)
                        ? "Изображение тепловой карты не приложено."
                        : $"Изображение отчета: {imagePath}");
            }

            RemoveManualPageBreaks(document);

            InvokeMethod(document, "Save");
            InvokeMethod(document, "ExportAsFixedFormat", outputPdfPath, WdExportFormatPdf);
            InvokeMethod(document, "Close", false);
            document = null;

            InvokeMethod(wordApplication, "Quit", 0);
            wordApplication = null;

            ReleaseComObject(documents);

            return File.Exists(outputDocxPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(GetDetailedExceptionMessage(ex), ex);
        }
        finally
        {
            ReleaseComObject(document);
            ReleaseComObject(wordApplication);
        }
    }

    private static void ApplyTemplateData(object document, CrowdReportExportRequest request)
    {
        Dictionary<string, string> placeholders = BuildPlaceholders(request);

        foreach (KeyValuePair<string, string> placeholder in placeholders)
        {
            if (placeholder.Key == "{{NOTES}}")
            {
                ReplaceLongTextToken(document, placeholder.Key, placeholder.Value);
            }
            else
            {
                ReplaceAllText(document, placeholder.Key, placeholder.Value);
            }
        }

    }

    private static Dictionary<string, string> BuildPlaceholders(CrowdReportExportRequest request)
    {
        CrowdSimulationResult simulation = request.Simulation;
        CrowdSimulationCoreMetrics core = simulation.CoreMetrics;
        CrowdHeatmapResult? heatmap = request.Heatmap;

        string projectName = SafeText(Fallback(request.ProjectName, "Crowd Flow Study"));
        string siteName = SafeText(Fallback(request.SiteName, "Site is not specified"));
        string scenarioName = SafeText(Fallback(request.ScenarioName, "Base scenario"));
        string notes = string.IsNullOrWhiteSpace(request.Notes)
            ? "Дополнительные комментарии не указаны."
            : request.Notes.Trim();
        PeakMetricDescriptor peakMetric = ResolvePeakMetric(request);

        string modeName = heatmap?.LegendTitle ?? heatmap?.Mode ?? "Heatmap is not attached";
        string minValue = heatmap != null ? FormatNumber(heatmap.MinimumValue) : "—";
        string maxValue = heatmap != null ? FormatNumber(heatmap.PeakValue) : "—";

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{PROJECT_NAME}}"] = projectName,
            ["{{SITE_NAME}}"] = siteName,
            ["{{REPORT_DATE}}"] = DateTime.Now.ToString("dd.MM.yyyy"),
            ["{{SCENARIO_NAME}}"] = scenarioName,
            ["{{AGENT_COUNT}}"] = simulation.TotalSpawned.ToString(),
            ["{{TIME_STEP}}"] = FormatNumber(simulation.Model.TimeStep),
            ["{{MODE_NAME}}"] = modeName,
            ["{{MIN_VALUE}}"] = minValue,
            ["{{MAX_VALUE}}"] = maxValue,
            ["{{CLEARANCE_TIME}}"] = FormatNumber(core.ClearanceTime),
            ["{{MEAN_TRAVEL_TIME}}"] = FormatNullable(core.MeanTravelTime),
            ["{{MAX_TRAVEL_TIME}}"] = FormatNullable(core.MaximumTravelTime),
            ["{{PEAK_DENSITY}}"] = peakMetric.Value,
            ["{{PEAK_METRIC_LABEL}}"] = peakMetric.Label,
            ["{{PEAK_METRIC_VALUE}}"] = peakMetric.Value,
            ["{{PEAK_METRIC_LINE}}"] = $"{peakMetric.Label} {peakMetric.Value}",
            ["{{EXIT_SPLIT}}"] = FormatExitSplit(core.ExitSplits),
            ["{{NOTES}}"] = notes,
        };
    }

    private static PeakMetricDescriptor ResolvePeakMetric(CrowdReportExportRequest request)
    {
        CrowdHeatmapResult? heatmap = request.Heatmap;
        string resolvedMode = ResolveMetricMode(request.ScenarioName, heatmap?.Mode, heatmap?.LegendTitle);

        if (heatmap == null)
        {
            return new PeakMetricDescriptor(GetPeakMetricLabel(resolvedMode), "—");
        }

        return new PeakMetricDescriptor(
            GetPeakMetricLabel(resolvedMode),
            FormatPeakMetricValue(resolvedMode, heatmap.PeakValue, heatmap.LegendTitle));
    }

    private static string ResolveMetricMode(params string?[] candidates)
    {
        foreach (string? candidate in candidates)
        {
            string normalized = NormalizeMetricMode(candidate);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return "occupancy";
    }

    private static string NormalizeMetricMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value!.Trim().ToLowerInvariant();
        if (normalized.Contains("throughput"))
        {
            return "throughput";
        }

        if (normalized.Contains("density"))
        {
            return "density";
        }

        if (normalized.Contains("speed"))
        {
            return "speed";
        }

        if (normalized.Contains("congestion"))
        {
            return "congestion";
        }

        if (normalized.Contains("occupancy"))
        {
            return "occupancy";
        }

        return string.Empty;
    }

    private static string GetPeakMetricLabel(string mode)
    {
        return mode switch
        {
            "throughput" => "4. Пиковая интенсивность потока:",
            "density" => "4. Пиковая плотность:",
            "speed" => "4. Максимальная скорость:",
            "congestion" => "4. Пиковый индекс перегруженности:",
            _ => "4. Пиковая занятость ячейки:",
        };
    }

    private static string FormatPeakMetricValue(string mode, double value, string? legendTitle)
    {
        string formattedValue = FormatNumber(value);
        string unit = ExtractLegendUnit(legendTitle);

        if (string.IsNullOrWhiteSpace(formattedValue) || string.Equals(formattedValue, "—", StringComparison.Ordinal))
        {
            return "—";
        }

        return mode switch
        {
            "throughput" => AppendUnit(formattedValue, string.IsNullOrWhiteSpace(unit) ? "agents/s" : unit),
            "density" => AppendUnit(formattedValue, string.IsNullOrWhiteSpace(unit) ? "agents/m2" : unit),
            "speed" => AppendUnit(formattedValue, string.IsNullOrWhiteSpace(unit) ? "m/s" : unit),
            "congestion" => AppendUnit(formattedValue, string.IsNullOrWhiteSpace(unit) ? "relative" : unit),
            _ => AppendUnit(formattedValue, unit),
        };
    }

    private static string ExtractLegendUnit(string? legendTitle)
    {
        if (string.IsNullOrWhiteSpace(legendTitle))
        {
            return string.Empty;
        }

        int separatorIndex = legendTitle!.LastIndexOf(',');
        if (separatorIndex < 0 || separatorIndex >= legendTitle.Length - 1)
        {
            return string.Empty;
        }

        return legendTitle.Substring(separatorIndex + 1).Trim();
    }

    private static string AppendUnit(string value, string? unit)
    {
        return string.IsNullOrWhiteSpace(unit) ? value : $"{value} {unit!.Trim()}";
    }

    private static string NormalizeBasePath(string outputPath, string scenarioName, string projectName)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new InvalidOperationException("Output path is required.");
        }

        string fullPath = Path.GetFullPath(outputPath);
        if (Directory.Exists(fullPath))
        {
            string fileName = SanitizeFileName(Fallback(scenarioName, Fallback(projectName, $"CrowdReport_{DateTime.Now:yyyyMMdd_HHmmss}")));
            return Path.Combine(fullPath, fileName);
        }

        string extension = Path.GetExtension(fullPath);
        if (string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(
                Path.GetDirectoryName(fullPath) ?? string.Empty,
                Path.GetFileNameWithoutExtension(fullPath));
        }

        return fullPath;
    }

    private static void PrepareOutputDocxFromTemplate(string templatePath, string outputDocxPath)
    {
        if (!File.Exists(templatePath))
        {
            throw new InvalidOperationException("Word template not found: " + templatePath);
        }

        string? outputDirectory = Path.GetDirectoryName(outputDocxPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory) && !Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        if (File.Exists(outputDocxPath))
        {
            File.Delete(outputDocxPath);
        }

        string outputPdfPath = Path.Combine(
            Path.GetDirectoryName(outputDocxPath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(outputDocxPath) + ".pdf");
        if (File.Exists(outputPdfPath))
        {
            File.Delete(outputPdfPath);
        }

        File.Copy(templatePath, outputDocxPath, true);
    }

    private static void NormalizeOutputDocxTemplate(string outputDocxPath, CrowdReportExportRequest request)
    {
        Dictionary<string, string> placeholders = BuildPlaceholders(request);
        string peakLine = placeholders["{{PEAK_METRIC_LINE}}"];

        using FileStream stream = new(outputDocxPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using ZipArchive archive = new(stream, ZipArchiveMode.Update);
        ZipArchiveEntry? entry = archive.GetEntry("word/document.xml")
            ?? archive.Entries.FirstOrDefault(candidate =>
                string.Equals(candidate.FullName, "word/document.xml", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.FullName, "word\\document.xml", StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            return;
        }

        string xml;
        using (Stream entryStream = entry.Open())
        using (StreamReader reader = new(entryStream, Encoding.UTF8))
        {
            xml = reader.ReadToEnd();
        }

        if (xml.Contains("{{PEAK_METRIC_LINE}}"))
        {
            xml = xml.Replace("{{PEAK_METRIC_LINE}}", placeholders["{{PEAK_METRIC_LINE}}"]);
        }

        if (xml.Contains("4. {{PEAK_METRIC_LABEL}} {{PEAK_METRIC_VALUE}}"))
        {
            xml = xml.Replace("4. {{PEAK_METRIC_LABEL}} {{PEAK_METRIC_VALUE}}", peakLine);
        }

        XDocument documentXml = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);

        foreach (XElement textNode in documentXml.Descendants().Where(node => node.Name.LocalName == "t"))
        {
            string value = textNode.Value;

            foreach (KeyValuePair<string, string> placeholder in placeholders)
            {
                if (placeholder.Key == "{{NOTES}}")
                {
                    continue;
                }

                if (value.Contains(placeholder.Key))
                {
                    value = value.Replace(placeholder.Key, placeholder.Value);
                }
            }

            textNode.Value = value;
        }

        XElement? peakParagraph = documentXml
            .Descendants()
            .FirstOrDefault(node =>
                node.Name.LocalName == "p" &&
                ContainsPeakMetricPlaceholder(GetParagraphText(node)));

        if (peakParagraph != null)
        {
            ReplaceParagraphText(peakParagraph, peakLine);
        }

        entry.Delete();
        ZipArchiveEntry updatedEntry = archive.CreateEntry("word/document.xml");
        using Stream updatedStream = updatedEntry.Open();
        using StreamWriter writer = new(updatedStream, new UTF8Encoding(false));
        documentXml.Save(writer, SaveOptions.DisableFormatting);
    }

    private static void ReplaceAllText(object document, string token, string replacement)
    {
        object? content = null;
        object? find = null;
        object? replacementObject = null;

        try
        {
            content = GetProperty(document, "Content");
            find = GetProperty(content, "Find");
            InvokeMethod(find, "ClearFormatting");
            replacementObject = GetProperty(find, "Replacement");
            InvokeMethod(replacementObject, "ClearFormatting");

            InvokeMethod(
                find,
                "Execute",
                token,
                false,
                false,
                false,
                false,
                false,
                true,
                WdFindContinue,
                false,
                replacement ?? string.Empty,
                WdReplaceAll,
                false,
                false,
                false,
                false);
        }
        finally
        {
            ReleaseComObject(replacementObject);
            ReleaseComObject(find);
            ReleaseComObject(content);
        }
    }

    private static void ReplaceLongTextToken(object document, string token, string replacement)
    {
        object? content = null;
        object? range = null;
        object? find = null;

        try
        {
            content = GetProperty(document, "Content");
            range = GetProperty(content, "Duplicate");
            find = GetProperty(range, "Find");
            InvokeMethod(find, "ClearFormatting");

            object found = InvokeMethod(
                find,
                "Execute",
                token,
                false,
                false,
                false,
                false,
                false,
                true,
                WdFindContinue,
                false,
                string.Empty,
                0,
                false,
                false,
                false,
                false);

            if (found is bool isFound && isFound)
            {
                SetProperty(range, "Text", replacement ?? string.Empty);
            }
        }
        finally
        {
            ReleaseComObject(find);
            ReleaseComObject(range);
            ReleaseComObject(content);
        }
    }

    private static bool ContainsPeakMetricPlaceholder(string text)
    {
        return text.Contains("{{PEAK_METRIC_LINE}}") ||
               text.Contains("{{PEAK_METRIC_LABEL") ||
               text.Contains("{{PEAK_METRIC_VALUE");
    }

    private static string GetParagraphText(XElement paragraph)
    {
        StringBuilder builder = new();

        foreach (XElement textNode in paragraph.Descendants().Where(node => node.Name.LocalName == "t"))
        {
            builder.Append(textNode.Value);
        }

        return builder.ToString();
    }

    private static void ReplaceParagraphText(XElement paragraph, string replacement)
    {
        List<XElement> textNodes = paragraph.Descendants().Where(node => node.Name.LocalName == "t").ToList();
        if (textNodes.Count == 0)
        {
            return;
        }

        textNodes[0].Value = replacement;

        for (int index = 1; index < textNodes.Count; index++)
        {
            textNodes[index].Value = string.Empty;
        }
    }

    private static bool ReplaceImageToken(object document, string token, string imagePath, double imageWidthMm)
    {
        object? content = null;
        object? range = null;
        object? find = null;
        object? inlineShapes = null;
        object? picture = null;

        try
        {
            content = GetProperty(document, "Content");
            range = GetProperty(content, "Duplicate");
            find = GetProperty(range, "Find");
            InvokeMethod(find, "ClearFormatting");

            object found = InvokeMethod(
                find,
                "Execute",
                token,
                false,
                false,
                false,
                false,
                false,
                true,
                WdFindContinue,
                false,
                string.Empty,
                0,
                false,
                false,
                false,
                false);

            if (!(found is bool isFound) || !isFound)
            {
                return false;
            }

            SetProperty(range, "Text", string.Empty);
            InvokeMethod(range, "Collapse", WdCollapseEnd);

            inlineShapes = GetProperty(document, "InlineShapes");
            picture = InvokeMethod(inlineShapes, "AddPicture", imagePath, false, true, range);

            if (picture != null)
            {
                SetProperty(picture, "LockAspectRatio", WdTrue);
                ApplyPictureWidth(document, picture, imageWidthMm);
            }

            return true;
        }
        finally
        {
            ReleaseComObject(picture);
            ReleaseComObject(inlineShapes);
            ReleaseComObject(find);
            ReleaseComObject(range);
            ReleaseComObject(content);
        }
    }

    private static void RemoveManualPageBreaks(object document)
    {
        object? content = null;
        object? find = null;
        object? replacementObject = null;

        try
        {
            content = GetProperty(document, "Content");
            find = GetProperty(content, "Find");
            InvokeMethod(find, "ClearFormatting");
            replacementObject = GetProperty(find, "Replacement");
            InvokeMethod(replacementObject, "ClearFormatting");

            InvokeMethod(
                find,
                "Execute",
                "^m",
                false,
                false,
                false,
                false,
                false,
                true,
                WdFindContinue,
                false,
                string.Empty,
                WdReplaceAll,
                false,
                false,
                false,
                false);
        }
        finally
        {
            ReleaseComObject(replacementObject);
            ReleaseComObject(find);
            ReleaseComObject(content);
        }
    }

    private static bool InsertTemplateImage(object document, string imagePath, double imageWidthMm)
    {
        if (ReplaceImageToken(document, "{{HEATMAP_IMAGE}}", imagePath, imageWidthMm))
        {
            return true;
        }

        object? range = null;
        object? inlineShapes = null;
        object? picture = null;

        try
        {
            range = TryGetBookmarkRange(document, "HEATMAP_IMAGE") ?? TryGetBookmarkRange(document, "HeatmapImage");
            if (range == null)
            {
                return false;
            }

            SetProperty(range, "Text", string.Empty);
            InvokeMethod(range, "Collapse", WdCollapseEnd);

            inlineShapes = GetProperty(document, "InlineShapes");
            picture = InvokeMethod(inlineShapes, "AddPicture", imagePath, false, true, range);

            if (picture != null)
            {
                SetProperty(picture, "LockAspectRatio", WdTrue);
                ApplyPictureWidth(document, picture, imageWidthMm);
            }

            return true;
        }
        finally
        {
            ReleaseComObject(picture);
            ReleaseComObject(inlineShapes);
            ReleaseComObject(range);
        }
    }

    private static object? TryGetBookmarkRange(object document, string bookmarkName)
    {
        object? bookmarks = null;
        object? bookmark = null;
        object? range = null;

        try
        {
            bookmarks = GetProperty(document, "Bookmarks");
            object exists = InvokeMethod(bookmarks, "Exists", bookmarkName);
            if (!(exists is bool hasBookmark) || !hasBookmark)
            {
                return null;
            }

            bookmark = InvokeMethod(bookmarks, "Item", bookmarkName);
            range = GetProperty(bookmark, "Range");
            return range;
        }
        finally
        {
            ReleaseComObject(bookmark);
            ReleaseComObject(bookmarks);
        }
    }

    private static void ApplyPictureWidth(object document, object picture, double imageWidthMm)
    {
        double maxWidthPoints = imageWidthMm > 0.0
            ? MmToPoints(imageWidthMm)
            : GetTemplateContentWidthPoints(document) * 0.98;

        double maxHeightPoints = GetTemplateImageHeightPoints(document);
        FitPictureIntoBounds(picture, maxWidthPoints, maxHeightPoints);
    }

    private static double GetTemplateContentWidthPoints(object document)
    {
        object? pageSetup = null;

        try
        {
            pageSetup = GetProperty(document, "PageSetup");
            double pageWidth = Convert.ToDouble(GetProperty(pageSetup, "PageWidth"));
            double leftMargin = Convert.ToDouble(GetProperty(pageSetup, "LeftMargin"));
            double rightMargin = Convert.ToDouble(GetProperty(pageSetup, "RightMargin"));
            double contentWidth = pageWidth - leftMargin - rightMargin;

            if (contentWidth > 0.0)
            {
                return contentWidth;
            }
        }
        catch
        {
            // Fall back to default A4 content width.
        }
        finally
        {
            ReleaseComObject(pageSetup);
        }

        return MmToPoints(180.0);
    }

    private static double GetTemplateImageHeightPoints(object document)
    {
        object? pageSetup = null;

        try
        {
            pageSetup = GetProperty(document, "PageSetup");
            double pageHeight = Convert.ToDouble(GetProperty(pageSetup, "PageHeight"));
            double topMargin = Convert.ToDouble(GetProperty(pageSetup, "TopMargin"));
            double bottomMargin = Convert.ToDouble(GetProperty(pageSetup, "BottomMargin"));
            double contentHeight = pageHeight - topMargin - bottomMargin;

            if (contentHeight > 0.0)
            {
                double reservedForText = MmToPoints(95.0);
                double imageHeight = contentHeight - reservedForText;
                if (imageHeight > MmToPoints(80.0))
                {
                    return imageHeight;
                }
            }
        }
        catch
        {
            // Fall back to a stable height.
        }
        finally
        {
            ReleaseComObject(pageSetup);
        }

        return MmToPoints(145.0);
    }

    private static void FitPictureIntoBounds(object picture, double maxWidthPoints, double maxHeightPoints)
    {
        double currentWidth = Convert.ToDouble(GetProperty(picture, "Width"));
        double currentHeight = Convert.ToDouble(GetProperty(picture, "Height"));

        if (currentWidth <= 0.0 || currentHeight <= 0.0)
        {
            return;
        }

        double widthScale = maxWidthPoints > 0.0 ? maxWidthPoints / currentWidth : double.MaxValue;
        double heightScale = maxHeightPoints > 0.0 ? maxHeightPoints / currentHeight : double.MaxValue;
        double scale = Math.Min(widthScale, heightScale);

        if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0.0)
        {
            return;
        }

        SetProperty(picture, "Width", (float)(currentWidth * scale));
    }

    // Word COM is only available on Windows; on other platforms return null so the caller
    // surfaces a clear "Word not installed" error rather than a platform exception
    private static Type? GetWordApplicationType()
    {
#if NETFRAMEWORK
        return Type.GetTypeFromProgID("Word.Application");
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }
        return Type.GetTypeFromProgID("Word.Application");
#endif
    }

    private static object GetProperty(object target, string name)
    {
        try
        {
            return target.GetType().InvokeMember(
                name,
                BindingFlags.GetProperty,
                null,
                target,
                null)!;
        }
        catch (TargetInvocationException ex)
        {
            throw new InvalidOperationException("Word property read failed: " + name + ". " + GetDetailedExceptionMessage(ex), ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Word property read failed: " + name + ". " + GetDetailedExceptionMessage(ex), ex);
        }
    }

    private static void SetProperty(object target, string name, object value)
    {
        try
        {
            target.GetType().InvokeMember(
                name,
                BindingFlags.SetProperty,
                null,
                target,
                new[] { value });
        }
        catch (TargetInvocationException ex)
        {
            throw new InvalidOperationException("Word property write failed: " + name + ". " + GetDetailedExceptionMessage(ex), ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Word property write failed: " + name + ". " + GetDetailedExceptionMessage(ex), ex);
        }
    }

    private static object InvokeMethod(object target, string name, params object[] args)
    {
        try
        {
            return target.GetType().InvokeMember(
                name,
                BindingFlags.InvokeMethod,
                null,
                target,
                args)!;
        }
        catch (TargetInvocationException ex)
        {
            throw new InvalidOperationException("Word method failed: " + name + ". " + GetDetailedExceptionMessage(ex), ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Word method failed: " + name + ". " + GetDetailedExceptionMessage(ex), ex);
        }
    }

    private static void ReleaseComObject(object? instance)
    {
        if (instance != null && System.Runtime.InteropServices.Marshal.IsComObject(instance))
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(instance);
            }
            catch
            {
                // Best-effort COM cleanup.
            }
        }
    }

    private static string GetDetailedExceptionMessage(Exception ex)
    {
        List<string> parts = new();
        Exception? current = ex;

        while (current != null)
        {
            string text = current.Message;
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (parts.Count == 0 || !string.Equals(parts[parts.Count - 1], text, StringComparison.Ordinal))
                {
                    parts.Add(text);
                }
            }

            current = current.InnerException;
        }

        return parts.Count == 0 ? ex.GetType().Name : string.Join(" | ", parts);
    }

    private static string NormalizeFileReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim();
        if (trimmed.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Uri uri = new(trimmed);
                if (uri.IsFile)
                {
                    return uri.LocalPath;
                }
            }
            catch
            {
                // Fall back to the trimmed input.
            }
        }

        return trimmed;
    }

    private static string SafeText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private static string FormatExitSplit(IReadOnlyList<CrowdExitSplitMetric> exitSplits)
    {
        if (exitSplits.Count == 0)
        {
            return "Распределение по выходам не определено.";
        }

        return string.Join(
            "; ",
            exitSplits.Select(split =>
                $"выход {split.ExitIndex + 1}: {split.CompletedAgents} чел. ({split.Share:P0})"));
    }

    private static string FormatNullable(double? value)
    {
        return value.HasValue ? FormatNumber(value.Value) : "—";
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.###");
    }

    private static string Fallback(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string SanitizeFileName(string value)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        StringBuilder builder = new(value.Length);

        foreach (char character in value)
        {
            builder.Append(invalidChars.Contains(character) ? '_' : character);
        }

        string sanitized = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "CrowdReport" : sanitized;
    }

    private static float MmToPoints(double millimeters)
    {
        return (float)(millimeters * 72.0 / 25.4);
    }
}

