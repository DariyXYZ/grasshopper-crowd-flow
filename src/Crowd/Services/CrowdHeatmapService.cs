using Crowd.Models;
using Crowd.Utilities;
using Rhino.Geometry;
using System.Drawing;

namespace Crowd.Services;

public static class CrowdHeatmapService
{
    /// <summary>
    /// Builds a colored mesh heatmap from simulated crowd positions.
    /// Samples occupancy on the same routing grid used by the solver, smooths the scalar field, and colors a quad mesh for visualization.
    /// </summary>
    /// <param name="result">Simulation result containing frames and the original crowd model.</param>
    /// <param name="smoothingPasses">Number of scalar-field smoothing passes.</param>
    /// <param name="heightScale">Optional Z offset scale for turning a flat heatmap into a relief mesh.</param>
    /// <param name="mode">Heatmap mode: occupancy, density, throughput, speed, or congestion.</param>
    /// <param name="normalizeByFrameCount">Whether occupancy-like values are normalized by frame count.</param>
    /// <param name="presentationMode">Whether to apply report-friendly smoothing and local peak softening to the visual field.</param>
    /// <returns>Colored mesh heatmap plus scalar metadata for downstream reporting and legend generation.</returns>
    public static CrowdHeatmapResult Build(
        CrowdSimulationResult result,
        string mode,
        int smoothingPasses,
        double heightScale,
        bool normalizeByFrameCount,
        bool presentationMode = false)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        CrowdGrid grid = new(result.Model.Floor, result.Model.Obstacles);
        string safeMode = string.IsNullOrWhiteSpace(mode) ? "Occupancy" : mode.Trim();
        double[,] occupancy = new double[grid.Width, grid.Height];
        double[,] flow = new double[grid.Width, grid.Height];
        double[,] speedSum = new double[grid.Width, grid.Height];
        double[,] speedCount = new double[grid.Width, grid.Height];
        double simulatedDuration = Math.Max(result.SimulatedDuration, result.Frames.Count * result.Model.TimeStep);

        for (int frameIndex = 0; frameIndex < result.Frames.Count; frameIndex++)
        {
            CrowdFrame frame = result.Frames[frameIndex];
            for (int i = 0; i < frame.ActivePositions.Count && i < frame.ActiveSpeeds.Count; i++)
            {
                AddSoftCellContribution(grid, occupancy, frame.ActivePositions[i], result.Model.TimeStep);
                AddSoftCellContribution(grid, speedSum, frame.ActivePositions[i], frame.ActiveSpeeds[i]);
                AddSoftCellContribution(grid, speedCount, frame.ActivePositions[i], 1.0);
            }
        }

        foreach (CrowdAgentPath path in result.AgentPaths)
        {
            if (path.Polyline == null || path.Polyline.Count < 2)
            {
                continue;
            }

            HashSet<(int X, int Y)> traversedCells = new();
            for (int i = 1; i < path.Polyline.Count; i++)
            {
                Point3d from = path.Polyline[i - 1];
                Point3d to = path.Polyline[i];
                double segmentLength = from.DistanceTo(to);
                int samples = Math.Max(1, (int)Math.Ceiling(segmentLength / Math.Max(result.Model.Floor.CellSize * 0.5, 0.25)));
                for (int sampleIndex = 0; sampleIndex <= samples; sampleIndex++)
                {
                    double t = samples == 0 ? 0.0 : (double)sampleIndex / samples;
                    Point3d sample = new(
                        from.X + ((to.X - from.X) * t),
                        from.Y + ((to.Y - from.Y) * t),
                        from.Z + ((to.Z - from.Z) * t));

                    if (grid.TryGetClosestWalkableCell(sample, out int x, out int y) && traversedCells.Add((x, y)))
                    {
                        AddSoftCellContribution(grid, flow, sample, 1.0);
                    }
                }
            }
        }

        string legendTitle = BuildLegendTitle(safeMode, normalizeByFrameCount, presentationMode);
        double[,] values = BuildModeField(safeMode, occupancy, flow, speedSum, speedCount, result, grid, normalizeByFrameCount, simulatedDuration);

        for (int pass = 0; pass < Math.Max(0, smoothingPasses); pass++)
        {
            values = Smooth(values, grid);
        }

        if (presentationMode)
        {
            values = ApplyPresentationSmoothing(values, grid, Math.Max(2, Math.Min(5, smoothingPasses + 1)));
            values = SoftenCornerPeaks(values, grid);
        }

        double minimum = GetMinimum(values, grid);
        double peak = GetPeak(values, grid);
        Mesh mesh = new();
        List<double> flatValues = new();
        BoundingBox bounds = result.Model.Floor.Boundary.GetBoundingBox(true);

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (!grid.IsWalkable(x, y))
                {
                    continue;
                }

                double cellValue = values[x, y];
                Point3d center = grid.GetCellCenter(x, y);
                flatValues.Add(cellValue);

                double normalized = peak <= 1e-9 ? 0.0 : cellValue / peak;
                double z = heightScale * normalized;
                double half = result.Model.Floor.CellSize * 0.5;

                int a = mesh.Vertices.Add(center.X - half, center.Y - half, center.Z + z);
                int b = mesh.Vertices.Add(center.X + half, center.Y - half, center.Z + z);
                int c = mesh.Vertices.Add(center.X + half, center.Y + half, center.Z + z);
                int d = mesh.Vertices.Add(center.X - half, center.Y + half, center.Z + z);
                mesh.Faces.AddFace(a, b, c, d);

                Color color = InterpolateHeatColor(normalized);
                mesh.VertexColors.Add(color);
                mesh.VertexColors.Add(color);
                mesh.VertexColors.Add(color);
                mesh.VertexColors.Add(color);
            }
        }

        mesh.Normals.ComputeNormals();
        mesh.Compact();

        return new CrowdHeatmapResult(mesh, flatValues, bounds, result.Model.Floor.CellSize, heightScale, minimum, peak, safeMode, legendTitle);
    }

    private static void AddSoftCellContribution(CrowdGrid grid, double[,] values, Point3d point, double amount)
    {
        double continuousX = ((point.X - grid.MinX) / grid.Floor.CellSize) - 0.5;
        double continuousY = ((point.Y - grid.MinY) / grid.Floor.CellSize) - 0.5;

        int x0 = (int)Math.Floor(continuousX);
        int y0 = (int)Math.Floor(continuousY);
        double tx = Math.Max(0.0, Math.Min(1.0, continuousX - x0));
        double ty = Math.Max(0.0, Math.Min(1.0, continuousY - y0));

        AddWeightedCellValue(grid, values, x0, y0, amount * (1.0 - tx) * (1.0 - ty));
        AddWeightedCellValue(grid, values, x0 + 1, y0, amount * tx * (1.0 - ty));
        AddWeightedCellValue(grid, values, x0, y0 + 1, amount * (1.0 - tx) * ty);
        AddWeightedCellValue(grid, values, x0 + 1, y0 + 1, amount * tx * ty);
    }

    private static void AddWeightedCellValue(CrowdGrid grid, double[,] values, int x, int y, double amount)
    {
        if (amount <= 1e-12 || !grid.IsWalkable(x, y))
        {
            return;
        }

        values[x, y] += amount;
    }

    private static double[,] BuildModeField(
        string mode,
        double[,] occupancy,
        double[,] flow,
        double[,] speedSum,
        double[,] speedCount,
        CrowdSimulationResult result,
        CrowdGrid grid,
        bool normalizeByFrameCount,
        double simulatedDuration)
    {
        double[,] values = new double[grid.Width, grid.Height];
        double frameDivisor = normalizeByFrameCount ? Math.Max(1, result.Frames.Count) : 1.0;
        double cellArea = Math.Max(grid.Floor.CellSize * grid.Floor.CellSize, 1e-9);
        double safeDuration = Math.Max(simulatedDuration, result.Model.TimeStep);

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (!grid.IsWalkable(x, y))
                {
                    continue;
                }

                double averageSpeed = speedCount[x, y] <= 1e-9 ? 0.0 : speedSum[x, y] / speedCount[x, y];
                double occupancyValue = occupancy[x, y] / frameDivisor;
                double normalizedOccupancy = occupancy[x, y] / safeDuration;
                double densityValue = normalizedOccupancy / cellArea;
                double flowValue = flow[x, y] / safeDuration;
                double congestionValue = occupancyValue * (1.0 + Math.Max(0.0, result.Model.AgentProfile.PreferredSpeed - averageSpeed));

                switch (mode.ToLowerInvariant())
                {
                    case "throughput":
                        values[x, y] = flowValue;
                        break;
                    case "speed":
                        values[x, y] = averageSpeed;
                        break;
                    case "density":
                        values[x, y] = densityValue;
                        break;
                    case "congestion":
                        values[x, y] = congestionValue;
                        break;
                    default:
                        values[x, y] = normalizeByFrameCount ? normalizedOccupancy : occupancy[x, y];
                        break;
                }
            }
        }

        return values;
    }

    private static string BuildLegendTitle(string mode, bool normalizeByFrameCount, bool presentationMode)
    {
        string title = mode.ToLowerInvariant() switch
        {
            "speed" => "Speed, m/s",
            "density" => "Density, agents/m2",
            "throughput" => "Cell Throughput, agents/s",
            "congestion" => "Congestion, relative",
            _ => normalizeByFrameCount ? "Occupancy, normalized" : "Occupancy, agent-s/cell"
        };

        return presentationMode ? $"{title} (presentation)" : title;
    }

    private static double[,] Smooth(double[,] values, CrowdGrid grid)
    {
        double[,] smoothed = new double[grid.Width, grid.Height];
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (!grid.IsWalkable(x, y))
                {
                    continue;
                }

                double sum = 0.0;
                double weightSum = 0.0;
                for (int ox = -1; ox <= 1; ox++)
                {
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        int nx = x + ox;
                        int ny = y + oy;
                        if (!grid.IsWalkable(nx, ny))
                        {
                            continue;
                        }

                        double weight = (ox == 0 && oy == 0) ? 0.4 : ((ox == 0 || oy == 0) ? 0.15 : 0.075);
                        sum += values[nx, ny] * weight;
                        weightSum += weight;
                    }
                }

                smoothed[x, y] = weightSum <= 1e-9 ? values[x, y] : sum / weightSum;
            }
        }

        return smoothed;
    }

    private static double[,] ApplyPresentationSmoothing(double[,] values, CrowdGrid grid, int passes)
    {
        double[,] current = values;
        for (int pass = 0; pass < passes; pass++)
        {
            current = SmoothGaussianLike(current, grid);
        }

        return current;
    }

    private static double[,] SmoothGaussianLike(double[,] values, CrowdGrid grid)
    {
        double[,] smoothed = new double[grid.Width, grid.Height];
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (!grid.IsWalkable(x, y))
                {
                    continue;
                }

                double sum = 0.0;
                double weightSum = 0.0;
                for (int ox = -2; ox <= 2; ox++)
                {
                    for (int oy = -2; oy <= 2; oy++)
                    {
                        int nx = x + ox;
                        int ny = y + oy;
                        if (!grid.IsWalkable(nx, ny) || !HasLineOfWalkableCells(grid, x, y, nx, ny))
                        {
                            continue;
                        }

                        double distanceSquared = (ox * ox) + (oy * oy);
                        double weight = Math.Exp(-distanceSquared / 3.0);
                        sum += values[nx, ny] * weight;
                        weightSum += weight;
                    }
                }

                smoothed[x, y] = weightSum <= 1e-9 ? values[x, y] : sum / weightSum;
            }
        }

        return smoothed;
    }

    private static double[,] SoftenCornerPeaks(double[,] values, CrowdGrid grid)
    {
        double[,] softened = new double[grid.Width, grid.Height];
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (!grid.IsWalkable(x, y))
                {
                    continue;
                }

                double neighborSum = 0.0;
                double neighborWeight = 0.0;
                for (int ox = -1; ox <= 1; ox++)
                {
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        if (ox == 0 && oy == 0)
                        {
                            continue;
                        }

                        int nx = x + ox;
                        int ny = y + oy;
                        if (!grid.IsWalkable(nx, ny))
                        {
                            continue;
                        }

                        double weight = (ox == 0 || oy == 0) ? 1.0 : 0.55;
                        neighborSum += values[nx, ny] * weight;
                        neighborWeight += weight;
                    }
                }

                if (neighborWeight <= 1e-9)
                {
                    softened[x, y] = values[x, y];
                    continue;
                }

                double neighborAverage = neighborSum / neighborWeight;
                double excess = Math.Max(0.0, values[x, y] - (neighborAverage * 1.45));
                double cornerFactor = GetCornerProximityFactor(grid, x, y);
                softened[x, y] = values[x, y] - (excess * cornerFactor * 0.42);
            }
        }

        return softened;
    }

    private static bool HasLineOfWalkableCells(CrowdGrid grid, int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int x = x0;
        int y = y0;

        while (true)
        {
            if (!grid.IsWalkable(x, y))
            {
                return false;
            }

            if (x == x1 && y == y1)
            {
                return true;
            }

            int e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }

    private static double GetCornerProximityFactor(CrowdGrid grid, int x, int y)
    {
        int blockedCardinal = 0;
        if (!grid.IsWalkable(x - 1, y))
        {
            blockedCardinal++;
        }

        if (!grid.IsWalkable(x + 1, y))
        {
            blockedCardinal++;
        }

        if (!grid.IsWalkable(x, y - 1))
        {
            blockedCardinal++;
        }

        if (!grid.IsWalkable(x, y + 1))
        {
            blockedCardinal++;
        }

        return Math.Max(0.0, Math.Min(1.0, blockedCardinal / 2.0));
    }

    private static double GetMinimum(double[,] values, CrowdGrid grid)
    {
        double minimum = double.PositiveInfinity;
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (!grid.IsWalkable(x, y))
                {
                    continue;
                }

                minimum = Math.Min(minimum, values[x, y]);
            }
        }

        return double.IsPositiveInfinity(minimum) ? 0.0 : minimum;
    }

    private static double GetPeak(double[,] values, CrowdGrid grid)
    {
        double peak = 0.0;
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (!grid.IsWalkable(x, y))
                {
                    continue;
                }

                peak = Math.Max(peak, values[x, y]);
            }
        }

        return peak;
    }

    internal static Color InterpolateHeatColor(double t)
    {
        t = Math.Max(0.0, Math.Min(1.0, t));
        if (t < 0.25)
        {
            return Lerp(Color.FromArgb(24, 75, 217), Color.FromArgb(46, 196, 182), t / 0.25);
        }

        if (t < 0.5)
        {
            return Lerp(Color.FromArgb(46, 196, 182), Color.FromArgb(255, 230, 109), (t - 0.25) / 0.25);
        }

        if (t < 0.75)
        {
            return Lerp(Color.FromArgb(255, 230, 109), Color.FromArgb(255, 159, 28), (t - 0.5) / 0.25);
        }

        return Lerp(Color.FromArgb(255, 159, 28), Color.FromArgb(214, 40, 40), (t - 0.75) / 0.25);
    }

    private static Color Lerp(Color from, Color to, double t)
    {
        int r = (int)Math.Round(from.R + ((to.R - from.R) * t));
        int g = (int)Math.Round(from.G + ((to.G - from.G) * t));
        int b = (int)Math.Round(from.B + ((to.B - from.B) * t));
        return Color.FromArgb(r, g, b);
    }
}
