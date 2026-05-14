using System.Drawing;
using Crowd.Models;
using Rhino.Geometry;

namespace Crowd.Services;

/// <summary>Builds lightweight static meshes for visual playback of crowd simulation results.</summary>
public static class CrowdAgentPreviewService
{
    /// <summary>Creates agent meshes at a specific simulation time.</summary>
    public static CrowdAgentPreviewFrame CreateFrame(CrowdSimulationResult result, double simulationTime, Color color)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        double duration = Math.Max(0.0, result.SimulatedDuration);
        double clampedTime = duration > 0.0
            ? Math.Max(0.0, Math.Min(simulationTime, duration))
            : 0.0;

        double radius = Math.Max(0.05, result.Model.AgentProfile.Radius);
        const double height = 1.7;
        List<Mesh> meshes = new();
        List<Point3d> positions = new();

        foreach (CrowdAgentPath path in result.AgentPaths)
        {
            if (!TryEvaluateAgent(path, result.Model.TimeStep, clampedTime, out Point3d position, out Vector3d heading))
            {
                continue;
            }

            Point3d planePosition = ProjectToFloorPlane(position, result.Model.Floor.Plane);
            Mesh mesh = CreatePersonMesh(planePosition, heading, radius, height, color);
            meshes.Add(mesh);
            positions.Add(planePosition);
        }

        return new CrowdAgentPreviewFrame(meshes, positions, clampedTime, positions.Count);
    }

    private static Point3d ProjectToFloorPlane(Point3d point, Plane plane)
    {
        if (!plane.IsValid)
        {
            return new Point3d(point.X, point.Y, 0.0);
        }

        return plane.ClosestPoint(point);
    }

    private static bool TryEvaluateAgent(CrowdAgentPath path, double timeStep, double simulationTime, out Point3d position, out Vector3d heading)
    {
        position = Point3d.Unset;
        heading = Vector3d.XAxis;

        if (path.Polyline.Count == 0 || simulationTime < path.SpawnTime)
        {
            return false;
        }

        if (path.FinishTime.HasValue && simulationTime > path.FinishTime.Value)
        {
            return false;
        }

        if (path.Polyline.Count == 1 || timeStep <= 0.0)
        {
            position = path.Polyline[0];
            return true;
        }

        double localTime = Math.Max(0.0, simulationTime - path.SpawnTime);
        double rawIndex = localTime / timeStep;
        int lower = Math.Max(0, Math.Min((int)Math.Floor(rawIndex), path.Polyline.Count - 1));
        int upper = Math.Min(lower + 1, path.Polyline.Count - 1);
        double t = Math.Max(0.0, Math.Min(rawIndex - lower, 1.0));

        Point3d from = path.Polyline[lower];
        Point3d to = path.Polyline[upper];
        position = from + ((to - from) * t);

        Vector3d direction = to - from;
        if (!direction.Unitize())
        {
            direction = GetNearbyDirection(path.Polyline, lower);
        }

        heading = direction.IsValid && direction.SquareLength > 0.0 ? direction : Vector3d.XAxis;
        return true;
    }

    private static Vector3d GetNearbyDirection(Polyline polyline, int index)
    {
        for (int i = index; i < polyline.Count - 1; i++)
        {
            Vector3d forward = polyline[i + 1] - polyline[i];
            if (forward.Unitize())
            {
                return forward;
            }
        }

        for (int i = index; i > 0; i--)
        {
            Vector3d backward = polyline[i] - polyline[i - 1];
            if (backward.Unitize())
            {
                return backward;
            }
        }

        return Vector3d.XAxis;
    }

    private static Mesh CreatePersonMesh(Point3d origin, Vector3d heading, double radius, double height, Color color)
    {
        Mesh mesh = new();
        heading.Z = 0.0;
        if (!heading.Unitize())
        {
            heading = Vector3d.YAxis;
        }

        Vector3d side = Vector3d.CrossProduct(Vector3d.ZAxis, heading);
        if (!side.Unitize())
        {
            side = Vector3d.XAxis;
        }

        double bodyWidth = Math.Max(radius * 0.82, 0.24);
        double bodyDepth = Math.Max(radius * 0.46, 0.14);
        double legWidth = bodyWidth * 0.36;
        double legGap = bodyWidth * 0.24;
        double legHeight = height * 0.365;
        double torsoHeight = height * 0.365;
        double shoulderHeight = legHeight + torsoHeight;
        double headSize = height * 0.155;
        double headZ = shoulderHeight + (height * 0.045) + (headSize * 0.5);

        AddOrientedBox(mesh, origin, side, heading, -((legGap * 0.5) + (legWidth * 0.5)), 0.0, legHeight * 0.5, legWidth, bodyDepth, legHeight, color);
        AddOrientedBox(mesh, origin, side, heading, ((legGap * 0.5) + (legWidth * 0.5)), 0.0, legHeight * 0.5, legWidth, bodyDepth, legHeight, color);
        AddOrientedBox(mesh, origin, side, heading, 0.0, 0.0, legHeight + (torsoHeight * 0.5), bodyWidth, bodyDepth, torsoHeight, color);
        AddOrientedBox(mesh, origin, side, heading, 0.0, 0.0, headZ, headSize, headSize * 0.86, headSize, color);

        mesh.Normals.ComputeNormals();
        mesh.Compact();
        return mesh;
    }

    private static void AddOrientedBox(
        Mesh mesh,
        Point3d origin,
        Vector3d side,
        Vector3d forward,
        double centerSide,
        double centerForward,
        double centerZ,
        double width,
        double depth,
        double height,
        Color color)
    {
        int start = mesh.Vertices.Count;
        double halfWidth = width * 0.5;
        double halfDepth = depth * 0.5;
        double halfHeight = height * 0.5;

        for (int z = -1; z <= 1; z += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int x = -1; x <= 1; x += 2)
                {
                    Point3d point = origin
                        + (side * (centerSide + (x * halfWidth)))
                        + (forward * (centerForward + (y * halfDepth)))
                        + new Vector3d(0.0, 0.0, centerZ + (z * halfHeight));
                    AddVertex(mesh, point, color);
                }
            }
        }

        mesh.Faces.AddFace(start + 0, start + 1, start + 3, start + 2);
        mesh.Faces.AddFace(start + 4, start + 6, start + 7, start + 5);
        mesh.Faces.AddFace(start + 0, start + 4, start + 5, start + 1);
        mesh.Faces.AddFace(start + 2, start + 3, start + 7, start + 6);
        mesh.Faces.AddFace(start + 0, start + 2, start + 6, start + 4);
        mesh.Faces.AddFace(start + 1, start + 5, start + 7, start + 3);
    }

    private static int AddVertex(Mesh mesh, Point3d point, Color color)
    {
        int index = mesh.Vertices.Add(point);
        mesh.VertexColors.Add(color);
        return index;
    }

}
