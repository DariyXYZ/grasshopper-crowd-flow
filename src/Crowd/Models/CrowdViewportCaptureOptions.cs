using Rhino.Geometry;

namespace Crowd.Models;

/// <summary>
/// Describes viewport capture settings for report and artifact export.
/// </summary>
public sealed class CrowdViewportCaptureOptions
{
    public CrowdViewportCaptureOptions(
        IReadOnlyList<GeometryBase> content,
        string filePath,
        BoundingBox frameBounds,
        int width,
        int height,
        double marginFactor,
        bool useTopView,
        bool cleanView,
        string displayModeName)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        FrameBounds = frameBounds;
        Width = width;
        Height = height;
        MarginFactor = marginFactor;
        UseTopView = useTopView;
        CleanView = cleanView;
        DisplayModeName = displayModeName ?? string.Empty;
    }

    public IReadOnlyList<GeometryBase> Content { get; }

    public string FilePath { get; }

    public BoundingBox FrameBounds { get; }

    public int Width { get; }

    public int Height { get; }

    public double MarginFactor { get; }

    public bool UseTopView { get; }

    public bool CleanView { get; }

    public string DisplayModeName { get; }
}
