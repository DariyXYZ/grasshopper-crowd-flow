using Crowd.Models;
using Rhino.Geometry;

namespace GrasshopperComponents.Utilities;

internal static class CrowdExportGeometryExtraction
{
    public static List<GeometryBase> ExtractContent(IEnumerable<object> inputs)
    {
        List<GeometryBase> geometry = new();

        foreach (object input in inputs)
        {
            AddContent(input, geometry);
        }

        return geometry;
    }

    private static void AddContent(object? input, List<GeometryBase> geometry)
    {
        if (input == null)
        {
            return;
        }

        if (GhObjectExtraction.TryExtract(input, out CrowdHeatmapResult? heatmap) && heatmap != null)
        {
            geometry.Add(heatmap.Mesh.DuplicateMesh());
            return;
        }

        if (GhObjectExtraction.TryExtract(input, out CrowdSimulationResult? simulation) && simulation != null)
        {
            AddModelGeometry(simulation.Model, geometry);
            foreach (CrowdAgentPath path in simulation.AgentPaths.Where(item => item.Polyline.Count >= 2))
            {
                geometry.Add(new PolylineCurve(path.Polyline));
            }

            return;
        }

        if (GhObjectExtraction.TryExtract(input, out CrowdModel? model) && model != null)
        {
            AddModelGeometry(model, geometry);
            return;
        }

        if (GhObjectExtraction.TryExtract(input, out GeometryBase? directGeometry) && directGeometry != null)
        {
            geometry.Add(directGeometry.Duplicate());
        }
    }

    private static void AddModelGeometry(CrowdModel model, List<GeometryBase> geometry)
    {
        geometry.Add(model.Floor.Boundary.DuplicateCurve());

        foreach (CrowdObstacle obstacle in model.Obstacles)
        {
            geometry.Add(obstacle.Boundary.DuplicateCurve());
        }

        foreach (CrowdSource source in model.Sources)
        {
            geometry.Add(new Point(source.Location));
        }

        foreach (CrowdExit exit in model.Exits)
        {
            geometry.Add(new Circle(Plane.WorldXY, exit.Location, Math.Max(exit.Radius, 0.01)).ToNurbsCurve());
        }
    }
}
