using Crowd.Models;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace Crowd.Services;

public static class CrowdHeatmapLegendService
{
    private const int LegendSegments = 24;
    private const double TitleGapFactor = 1.6;
    private const double LabelVerticalGapFactor = 0.9;
    private const double TextBaselineBelowBarFactor = 0.22;

    public static CrowdHeatmapLegendResult Build(CrowdHeatmapResult heatmap, double scale = 1.0, double textHeight = 0.0)
    {
        if (heatmap == null)
        {
            throw new ArgumentNullException(nameof(heatmap));
        }

        double safeScale = Math.Max(0.2, scale);
        BoundingBox bbox = heatmap.Bounds;
        double width = Math.Max(heatmap.CellSize * 10.0, bbox.Diagonal.X * 0.26) * safeScale;
        double height = Math.Max(heatmap.CellSize * 0.8, bbox.Diagonal.Y * 0.018) * safeScale;
        double horizontalMargin = 0.0;
        double verticalMargin = Math.Max(heatmap.CellSize * 1.2, Math.Min(bbox.Diagonal.X, bbox.Diagonal.Y) * 0.04) * safeScale;
        double labelGap = Math.Max(heatmap.CellSize * 0.45, height * LabelVerticalGapFactor);
        double z = bbox.Min.Z + Math.Max(0.0, heatmap.HeightScale) + (heatmap.CellSize * 0.15);

        Point3d origin = new(
            bbox.Max.X - width - horizontalMargin,
            bbox.Min.Y - height - verticalMargin,
            z);

        Mesh legendMesh = BuildLegendMesh(origin, width, height);
        double labelHeight = textHeight > 1e-6
            ? textHeight
            : Math.Max(heatmap.CellSize * 0.55, height * 1.45);
        string minText = FormatLegendValue(heatmap.MinimumValue);
        string maxText = FormatLegendValue(heatmap.PeakValue);
        string titleText = heatmap.LegendTitle;
        double titleGap = Math.Max(heatmap.CellSize * 0.6, labelHeight * TitleGapFactor);
        double labelY = origin.Y - labelGap - (labelHeight * TextBaselineBelowBarFactor);

        Point3d minAnchor = new(origin.X, labelY, z);
        Point3d maxAnchor = new(origin.X + width, labelY, z);
        Point3d titleAnchor = new(
            origin.X - titleGap,
            origin.Y + (height * 0.5),
            z);
        List<GeometryBase> labelGeometry = new()
        {
            CreateTextGeometry(titleText, titleAnchor, labelHeight, TextJustification.MiddleRight),
            CreateTextGeometry(minText, minAnchor, labelHeight, TextJustification.TopLeft),
            CreateTextGeometry(maxText, maxAnchor, labelHeight, TextJustification.TopRight)
        };

        return new CrowdHeatmapLegendResult(legendMesh, labelGeometry, heatmap.MinimumValue, heatmap.PeakValue);
    }

    private static Mesh BuildLegendMesh(Point3d origin, double width, double height)
    {
        Mesh legend = new();
        for (int i = 0; i < LegendSegments; i++)
        {
            double t0 = (double)i / LegendSegments;
            double t1 = (double)(i + 1) / LegendSegments;
            double x0 = origin.X + (width * t0);
            double x1 = origin.X + (width * t1);

            int a = legend.Vertices.Add(x0, origin.Y, origin.Z);
            int b = legend.Vertices.Add(x1, origin.Y, origin.Z);
            int c = legend.Vertices.Add(x1, origin.Y + height, origin.Z);
            int d = legend.Vertices.Add(x0, origin.Y + height, origin.Z);
            legend.Faces.AddFace(a, b, c, d);

            System.Drawing.Color color0 = CrowdHeatmapService.InterpolateHeatColor(t0);
            System.Drawing.Color color1 = CrowdHeatmapService.InterpolateHeatColor(t1);
            legend.VertexColors.Add(color0);
            legend.VertexColors.Add(color1);
            legend.VertexColors.Add(color1);
            legend.VertexColors.Add(color0);
        }

        legend.Normals.ComputeNormals();
        legend.Compact();
        return legend;
    }

    private static string FormatLegendValue(double value)
    {
        if (Math.Abs(value) >= 100.0)
        {
            return value.ToString("0.0");
        }

        if (Math.Abs(value) >= 10.0)
        {
            return value.ToString("0.00");
        }

        return value.ToString("0.###");
    }

    private static GeometryBase CreateTextGeometry(string text, Point3d anchor, double textHeight, TextJustification justification)
    {
        TextEntity entity = new()
        {
            Plane = new Plane(anchor, Vector3d.ZAxis),
            PlainText = text,
            TextHeight = textHeight,
            Justification = justification
        };

        return entity;
    }
}
