using Crowd.Models;

namespace Crowd.Utilities;

public static class CrowdPathFieldBuilder
{
    private static readonly (int X, int Y)[] NeighborOffsets = new (int X, int Y)[]
    {
        (-1, -1), (-1, 0), (-1, 1),
        (0, -1),           (0, 1),
        (1, -1),  (1, 0),  (1, 1)
    };

    /// <summary>
    /// Builds a discrete distance field from every walkable grid cell to a target exit.
    /// The field is later used by the crowd solver to follow the steepest local descent.
    /// </summary>
    /// <param name="grid">Walkable grid generated from floor and obstacles.</param>
    /// <param name="exit">Exit used as the destination for path costs.</param>
    /// <returns>Distance field where unreachable cells are set to <see cref="double.PositiveInfinity"/>.</returns>
    public static double[,] Build(CrowdGrid grid, CrowdExit exit)
    {
        if (grid == null)
        {
            throw new ArgumentNullException(nameof(grid));
        }

        if (exit == null)
        {
            throw new ArgumentNullException(nameof(exit));
        }

        double[,] field = new double[grid.Width, grid.Height];
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                field[x, y] = double.PositiveInfinity;
            }
        }

        if (!grid.TryGetClosestWalkableCell(exit.Location, out int exitX, out int exitY))
        {
            return field;
        }

        Queue<(int X, int Y)> queue = new();
        field[exitX, exitY] = 0.0;
        queue.Enqueue((exitX, exitY));

        while (queue.Count > 0)
        {
            (int currentX, int currentY) = queue.Dequeue();
            double current = field[currentX, currentY];

            foreach ((int offsetX, int offsetY) in NeighborOffsets)
            {
                int nextX = currentX + offsetX;
                int nextY = currentY + offsetY;
                if (!grid.IsWalkable(nextX, nextY))
                {
                    continue;
                }

                double stepCost = (offsetX != 0 && offsetY != 0) ? Math.Sqrt(2.0) : 1.0;
                double candidate = current + stepCost;
                if (candidate >= field[nextX, nextY])
                {
                    continue;
                }

                field[nextX, nextY] = candidate;
                queue.Enqueue((nextX, nextY));
            }
        }

        return field;
    }

    public static (int X, int Y) GetBestNeighbor(CrowdGrid grid, double[,] field, int x, int y)
    {
        double current = field[x, y];
        int bestX = x;
        int bestY = y;
        double best = current;

        foreach ((int offsetX, int offsetY) in NeighborOffsets)
        {
            int nextX = x + offsetX;
            int nextY = y + offsetY;
            if (!grid.IsWalkable(nextX, nextY))
            {
                continue;
            }

            double candidate = field[nextX, nextY];
            if (candidate < best)
            {
                best = candidate;
                bestX = nextX;
                bestY = nextY;
            }
        }

        return (bestX, bestY);
    }
}
