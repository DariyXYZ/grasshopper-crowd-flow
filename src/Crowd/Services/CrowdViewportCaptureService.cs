using System.Drawing;
using System.Drawing.Imaging;
using Crowd.Models;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;

namespace Crowd.Services;

/// <summary>
/// Captures a Rhino viewport into a raster image for downstream reporting.
/// </summary>
public static class CrowdViewportCaptureService
{
    /// <summary>
    /// Captures the requested content into a PNG file.
    /// </summary>
    /// <param name="options">Capture framing and style options.</param>
    /// <returns>Capture result with status and used bounds.</returns>
    public static CrowdViewportCaptureResult Capture(CrowdViewportCaptureOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.FilePath))
        {
            throw new InvalidOperationException("File path is required for image export.");
        }

        RhinoDoc? doc = RhinoDoc.ActiveDoc;
        if (doc == null)
        {
            throw new InvalidOperationException("RhinoDoc not found.");
        }

        RhinoView? originalView = doc.Views.ActiveView;
        if (originalView == null)
        {
            throw new InvalidOperationException("Active view not found.");
        }

        RhinoView captureView = originalView;
        RhinoView? tempTopView = null;
        RhinoViewport? styledViewport = null;
        DisplayModeDescription? originalDisplayMode = null;
        bool originalGridVisible = false;
        bool originalConstructionAxesVisible = false;
        bool originalWorldAxesVisible = false;
        bool hasViewportStyleSnapshot = false;
        BoundingBox usedBounds = BoundingBox.Empty;

        try
        {
            string? directory = Path.GetDirectoryName(options.FilePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            BoundingBox targetBounds = options.FrameBounds.IsValid
                ? options.FrameBounds
                : BuildBoundingBox(options.Content);

            if (targetBounds.IsValid)
            {
                targetBounds = ExpandBoundingBox(targetBounds, options.MarginFactor, doc.ModelAbsoluteTolerance);
            }

            if (options.UseTopView)
            {
                tempTopView = CreateTempTopView(doc, options.Width, options.Height, targetBounds, doc.ModelAbsoluteTolerance);
                if (tempTopView == null)
                {
                    throw new InvalidOperationException("Failed to create temporary Top view.");
                }

                captureView = tempTopView;
            }

            RhinoViewport? viewport = GetTargetViewport(captureView);
            if (viewport == null)
            {
                throw new InvalidOperationException("Viewport not found.");
            }

            styledViewport = viewport;
            originalDisplayMode = viewport.DisplayMode;
            originalGridVisible = viewport.ConstructionGridVisible;
            originalConstructionAxesVisible = viewport.ConstructionAxesVisible;
            originalWorldAxesVisible = viewport.WorldAxesVisible;
            hasViewportStyleSnapshot = true;

            if (options.CleanView)
            {
                ApplyCleanViewStyle(viewport, options.DisplayModeName);
            }

            if (targetBounds.IsValid)
            {
                if (options.UseTopView)
                {
                    viewport.SetProjection(DefinedViewportProjection.Top, "Top", true);
                    viewport.ChangeToParallelProjection(true);
                    viewport.SetCameraDirection(-Vector3d.ZAxis, false);
                    viewport.CameraUp = Vector3d.YAxis;

                    Point3d center = targetBounds.Center;
                    double distance = Math.Max(targetBounds.Diagonal.Length, Math.Max(doc.ModelAbsoluteTolerance * 1000.0, 10.0));
                    viewport.SetCameraTarget(center, false);
                    viewport.SetCameraLocation(center + (Vector3d.ZAxis * distance), false);
                }

                viewport.ZoomBoundingBox(targetBounds);
                viewport.SetClippingPlanes(targetBounds);
                usedBounds = targetBounds;
            }

            WaitForViewReady(captureView, doc, 3);

            ViewCapture capture = new()
            {
                Width = Math.Max(1, options.Width),
                Height = Math.Max(1, options.Height),
                DrawGrid = false,
                DrawGridAxes = false,
                DrawAxes = false,
                ScaleScreenItems = false,
                TransparentBackground = false,
            };

            Bitmap? bitmap = CaptureBitmapSafe(capture, captureView);
            if (bitmap == null && options.UseTopView)
            {
                WaitForViewReady(captureView, doc, 2);
                bitmap = CaptureBitmapSafe(capture, captureView);
            }

            if (bitmap == null)
            {
                throw new InvalidOperationException("Capture failed.");
            }

            using (bitmap)
            {
                bitmap.Save(options.FilePath, ImageFormat.Png);
            }

            string status = options.UseTopView
                ? $"Saved from dedicated Top view: {options.FilePath}"
                : $"Saved: {options.FilePath}";

            return new CrowdViewportCaptureResult(true, options.FilePath, status, usedBounds);
        }
        finally
        {
            if (tempTopView != null)
            {
                try
                {
                    tempTopView.Close();
                }
                catch
                {
                    // Best-effort cleanup.
                }
            }
            else if (hasViewportStyleSnapshot && styledViewport != null)
            {
                RestoreViewportStyle(
                    styledViewport,
                    originalDisplayMode,
                    originalGridVisible,
                    originalConstructionAxesVisible,
                    originalWorldAxesVisible);
            }

            try
            {
                doc.Views.ActiveView = originalView;
            }
            catch
            {
                // Best-effort cleanup.
            }

            RefreshUiAfterCapture(doc);
        }
    }

    private static void ApplyCleanViewStyle(RhinoViewport viewport, string displayModeName)
    {
        DisplayModeDescription? mode = ResolveDisplayMode(displayModeName);
        if (mode != null)
        {
            viewport.DisplayMode = mode;
        }

        viewport.ConstructionGridVisible = false;
        viewport.ConstructionAxesVisible = false;
        viewport.WorldAxesVisible = false;
    }

    private static void RestoreViewportStyle(
        RhinoViewport viewport,
        DisplayModeDescription? mode,
        bool gridVisible,
        bool constructionAxesVisible,
        bool worldAxesVisible)
    {
        try
        {
            if (mode != null)
            {
                viewport.DisplayMode = mode;
            }
        }
        catch
        {
            // Ignore restore issues.
        }

        viewport.ConstructionGridVisible = gridVisible;
        viewport.ConstructionAxesVisible = constructionAxesVisible;
        viewport.WorldAxesVisible = worldAxesVisible;
    }

    private static DisplayModeDescription? ResolveDisplayMode(string displayModeName)
    {
        if (!string.IsNullOrWhiteSpace(displayModeName))
        {
            DisplayModeDescription? requested = DisplayModeDescription.FindByName(displayModeName.Trim());
            if (requested != null)
            {
                return requested;
            }
        }

        return DisplayModeDescription.FindByName("Rendered")
            ?? DisplayModeDescription.FindByName("Shaded");
    }

    private static RhinoView? CreateTempTopView(RhinoDoc doc, int width, int height, BoundingBox box, double tolerance)
    {
        const string ViewName = "__GH_CROWD_CAPTURE_TOP__";

        RhinoView? existing = doc.Views.Find(ViewName, false);
        if (existing != null)
        {
            try
            {
                existing.Close();
            }
            catch
            {
                // Ignore stale view cleanup issues.
            }
        }

        Rectangle rectangle = new(80, 80, Math.Max(480, Math.Min(width, 1600)), Math.Max(360, Math.Min(height, 1200)));
        RhinoView? view = doc.Views.Add(ViewName, DefinedViewportProjection.Top, rectangle, true);
        if (view == null)
        {
            return null;
        }

        RhinoViewport? viewport = GetTargetViewport(view);
        if (viewport == null)
        {
            return null;
        }

        viewport.LockedProjection = false;
        viewport.ChangeToParallelProjection(true);
        viewport.SetProjection(DefinedViewportProjection.Top, "Top", true);
        viewport.SetCameraDirection(-Vector3d.ZAxis, false);
        viewport.CameraUp = Vector3d.YAxis;

        if (box.IsValid)
        {
            Point3d center = box.Center;
            double distance = Math.Max(box.Diagonal.Length, Math.Max(tolerance * 1000.0, 10.0));
            viewport.SetCameraTarget(center, false);
            viewport.SetCameraLocation(center + (Vector3d.ZAxis * distance), false);
            viewport.ZoomBoundingBox(box);
            viewport.SetClippingPlanes(box);
        }

        view.Redraw();
        WaitForViewReady(view, doc, 2);
        return view;
    }

    private static void WaitForViewReady(RhinoView? view, RhinoDoc? doc, int cycles)
    {
        int cycleCount = Math.Max(1, cycles);
        for (int i = 0; i < cycleCount; i++)
        {
            try
            {
                view?.Redraw();
                doc?.Views.Redraw();
                RhinoApp.Wait();
            }
            catch
            {
                // Ignore refresh issues.
            }
        }
    }

    private static void RefreshUiAfterCapture(RhinoDoc? doc)
    {
        try
        {
            doc?.Views.Redraw();
            RhinoApp.Wait();
        }
        catch
        {
            // Ignore refresh issues.
        }
    }

    private static Bitmap? CaptureBitmapSafe(ViewCapture capture, RhinoView view)
    {
        try
        {
            return capture.CaptureToBitmap(view);
        }
        catch
        {
            return null;
        }
    }

    private static RhinoViewport? GetTargetViewport(RhinoView? view)
    {
        if (view is RhinoPageView pageView && pageView.ActiveDetail != null)
        {
            try
            {
                DetailView? detail = pageView.ActiveDetail.DetailGeometry;
                if (detail != null && detail.IsProjectionLocked)
                {
                    detail.IsProjectionLocked = false;
                }
            }
            catch
            {
                // Ignore detail locking issues.
            }

            return pageView.ActiveDetail.Viewport;
        }

        return view?.ActiveViewport;
    }

    private static BoundingBox BuildBoundingBox(IReadOnlyList<GeometryBase> geometry)
    {
        BoundingBox box = BoundingBox.Empty;
        foreach (GeometryBase item in geometry)
        {
            BoundingBox bounds = item.GetBoundingBox(true);
            if (!bounds.IsValid)
            {
                continue;
            }

            if (!box.IsValid)
            {
                box = bounds;
            }
            else
            {
                box.Union(bounds);
            }
        }

        return box;
    }

    private static BoundingBox ExpandBoundingBox(BoundingBox box, double marginFactor, double tolerance)
    {
        if (!box.IsValid)
        {
            return box;
        }

        double diagonal = box.Diagonal.Length;
        double inflate = diagonal * Math.Max(0.0, marginFactor);
        if (inflate <= tolerance)
        {
            inflate = Math.Max(tolerance * 100.0, 1.0);
        }

        box.Inflate(inflate);
        if (box.Diagonal.Length <= tolerance)
        {
            box.Inflate(Math.Max(tolerance * 100.0, 1.0));
        }

        return box;
    }
}
