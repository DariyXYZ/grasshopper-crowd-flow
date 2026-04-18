using Crowd.Models;
using Rhino.Geometry;

namespace Crowd.Utilities;

public static class CrowdPathFieldBuilder
{
    private const double BoundaryPenaltyDistanceFactor = 2.15;
    private const double BoundaryPenaltyStrength = 0.72;

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

        MinOpenSet open = new();
        field[exitX, exitY] = 0.0;
        open.Enqueue((exitX, exitY), 0.0);

        while (open.Count > 0)
        {
            (int currentX, int currentY) = open.Dequeue();
            double current = field[currentX, currentY];

            foreach ((int offsetX, int offsetY) in NeighborOffsets)
            {
                int nextX = currentX + offsetX;
                int nextY = currentY + offsetY;
                if (!grid.IsWalkable(nextX, nextY))
                {
                    continue;
                }

                Point3d nextCenter = grid.GetCellCenter(nextX, nextY);
                double stepCost = (offsetX != 0 && offsetY != 0) ? Math.Sqrt(2.0) : 1.0;
                double safeDistance = Math.Max(grid.Floor.CellSize * BoundaryPenaltyDistanceFactor, 1e-6);
                double boundaryDistance = grid.GetBoundaryDistance(nextCenter);
                double boundaryPenalty = 0.0;
                if (!double.IsInfinity(boundaryDistance) && boundaryDistance < safeDistance)
                {
                    double closeness = 1.0 - (boundaryDistance / safeDistance);
                    boundaryPenalty = closeness * BoundaryPenaltyStrength;
                }

                stepCost *= 1.0 + boundaryPenalty;
                double candidate = current + stepCost;
                if (candidate >= field[nextX, nextY])
                {
                    continue;
                }

                field[nextX, nextY] = candidate;
                open.Enqueue((nextX, nextY), candidate);
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

    private sealed class MinOpenSet
    {
        private readonly List<((int X, int Y) Cell, double Priority)> _items = new();

        public int Count => _items.Count;

        public void Enqueue((int X, int Y) cell, double priority)
        {
            _items.Add((cell, priority));
            BubbleUp(_items.Count - 1);
        }

        public (int X, int Y) Dequeue()
        {
            (int X, int Y) result = _items[0].Cell;
            int lastIndex = _items.Count - 1;
            _items[0] = _items[lastIndex];
            _items.RemoveAt(lastIndex);
            if (_items.Count > 0)
            {
                BubbleDown(0);
            }

            return result;
        }

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_items[parent].Priority <= _items[index].Priority)
                {
                    break;
                }

                (_items[parent], _items[index]) = (_items[index], _items[parent]);
                index = parent;
            }
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                int left = (index * 2) + 1;
                int right = left + 1;
                int smallest = index;

                if (left < _items.Count && _items[left].Priority < _items[smallest].Priority)
                {
                    smallest = left;
                }

                if (right < _items.Count && _items[right].Priority < _items[smallest].Priority)
                {
                    smallest = right;
                }

                if (smallest == index)
                {
                    break;
                }

                (_items[smallest], _items[index]) = (_items[index], _items[smallest]);
                index = smallest;
            }
        }
    }
}
