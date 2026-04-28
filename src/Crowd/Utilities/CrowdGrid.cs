using Crowd.Models;
using Rhino.Geometry;

namespace Crowd.Utilities;

/// <summary>Rasterises a floor and its obstacles into a discrete walkability grid used for pathfinding and spatial queries.</summary>
public sealed class CrowdGrid
{
    private readonly bool[,] _walkable;
    private readonly Point3d[,] _cellCenters;
    private readonly double[,] _boundaryDistances;
    private readonly double _tolerance;
    private readonly Vector3d[,] _floorRepulsionDir;
    private readonly double[,] _floorBoundaryDist;
    private readonly Vector3d[][,]? _obstacleRepulsionDirs;
    private readonly double[][,]? _obstacleBoundaryDists;

    /// <summary>Builds the walkability and boundary-distance maps from the given floor and obstacles.</summary>
    public CrowdGrid(CrowdFloor floor, IReadOnlyList<CrowdObstacle> obstacles, double tolerance = 0.01)
    {
        if (floor == null)
        {
            throw new ArgumentNullException(nameof(floor));
        }

        if (obstacles == null)
        {
            throw new ArgumentNullException(nameof(obstacles));
        }

        Floor = floor;
        Obstacles = obstacles;
        _tolerance = tolerance;

        BoundingBox bbox = floor.Boundary.GetBoundingBox(true);
        MinX = bbox.Min.X;
        MinY = bbox.Min.Y;
        MaxX = bbox.Max.X;
        MaxY = bbox.Max.Y;

        Width = Math.Max(1, (int)Math.Ceiling((MaxX - MinX) / floor.CellSize));
        Height = Math.Max(1, (int)Math.Ceiling((MaxY - MinY) / floor.CellSize));

        _walkable = new bool[Width, Height];
        _cellCenters = new Point3d[Width, Height];
        _boundaryDistances = new double[Width, Height];
        _floorRepulsionDir = new Vector3d[Width, Height];
        _floorBoundaryDist = new double[Width, Height];
        if (obstacles.Count > 0)
        {
            _obstacleRepulsionDirs = new Vector3d[obstacles.Count][,];
            _obstacleBoundaryDists = new double[obstacles.Count][,];
            for (int i = 0; i < obstacles.Count; i++)
            {
                _obstacleRepulsionDirs[i] = new Vector3d[Width, Height];
                _obstacleBoundaryDists[i] = new double[Width, Height];
            }
        }

        BuildWalkableMap();
        BuildBoundaryDistanceMap();
    }

    /// <summary>Floor geometry this grid was built from.</summary>
    public CrowdFloor Floor { get; }

    /// <summary>Obstacles whose interiors are marked non-walkable in the grid.</summary>
    public IReadOnlyList<CrowdObstacle> Obstacles { get; }

    /// <summary>Number of grid columns along the X axis.</summary>
    public int Width { get; }

    /// <summary>Number of grid rows along the Y axis.</summary>
    public int Height { get; }

    /// <summary>World-space minimum X coordinate of the grid.</summary>
    public double MinX { get; }

    /// <summary>World-space minimum Y coordinate of the grid.</summary>
    public double MinY { get; }

    /// <summary>World-space maximum X coordinate of the grid.</summary>
    public double MaxX { get; }

    /// <summary>World-space maximum Y coordinate of the grid.</summary>
    public double MaxY { get; }

    /// <summary>Returns true when the cell at <paramref name="x"/>, <paramref name="y"/> is within bounds and walkable.</summary>
    public bool IsWalkable(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height && _walkable[x, y];
    }

    /// <summary>Finds the nearest walkable cell to a world-space point; returns false when no walkable cell exists.</summary>
    public bool TryGetClosestWalkableCell(Point3d point, out int x, out int y)
    {
        (int px, int py) = ToCell(point);
        if (IsWalkable(px, py))
        {
            x = px;
            y = py;
            return true;
        }

        int searchRadius = Math.Max(Width, Height);
        for (int radius = 1; radius <= searchRadius; radius++)
        {
            for (int ix = px - radius; ix <= px + radius; ix++)
            {
                for (int iy = py - radius; iy <= py + radius; iy++)
                {
                    if (!IsWalkable(ix, iy))
                    {
                        continue;
                    }

                    x = ix;
                    y = iy;
                    return true;
                }
            }
        }

        x = -1;
        y = -1;
        return false;
    }

    /// <summary>Converts a world-space point to the nearest grid cell coordinates, clamped to grid bounds.</summary>
    public (int X, int Y) ToCell(Point3d point)
    {
        int x = (int)Math.Floor((point.X - MinX) / Floor.CellSize);
        int y = (int)Math.Floor((point.Y - MinY) / Floor.CellSize);
        return (Math.Max(0, Math.Min(Width - 1, x)), Math.Max(0, Math.Min(Height - 1, y)));
    }

    /// <summary>Returns the world-space center point of the cell at grid coordinates <paramref name="x"/>, <paramref name="y"/>.</summary>
    public Point3d GetCellCenter(int x, int y)
    {
        return _cellCenters[x, y];
    }

    /// <summary>Returns true when the world-space point maps to a walkable cell within a reasonable snapping distance.</summary>
    public bool IsWalkable(Point3d point)
    {
        if (!TryGetClosestWalkableCell(point, out int x, out int y))
        {
            return false;
        }

        Point3d cellCenter = GetCellCenter(x, y);
        return cellCenter.DistanceTo(point) <= Floor.CellSize * 1.5;
    }

    /// <summary>Returns a repulsion vector pushing away from the nearest floor or obstacle boundary within the given influence radius.</summary>
    public Vector3d GetBoundaryRepulsion(Point3d point, double influenceRadius)
    {
        if (influenceRadius <= 1e-6)
        {
            return Vector3d.Zero;
        }

        (int x, int y) = ToCell(point);
        if (IsWalkable(x, y))
        {
            Vector3d result = Vector3d.Zero;
            double fd = _floorBoundaryDist[x, y];
            if (fd < influenceRadius)
            {
                result += _floorRepulsionDir[x, y] * ((influenceRadius - fd) / influenceRadius);
            }

            if (_obstacleBoundaryDists != null)
            {
                for (int i = 0; i < Obstacles.Count; i++)
                {
                    double od = _obstacleBoundaryDists[i][x, y];
                    if (od < influenceRadius)
                    {
                        result += _obstacleRepulsionDirs![i][x, y] * ((influenceRadius - od) / influenceRadius);
                    }
                }
            }

            return result;
        }

        Vector3d repulsion = Vector3d.Zero;
        repulsion += GetCurveRepulsion(Floor.Boundary, point, influenceRadius, invert: false);
        foreach (CrowdObstacle obstacle in Obstacles)
        {
            repulsion += GetCurveRepulsion(obstacle.Boundary, point, influenceRadius, invert: false);
        }

        return repulsion;
    }

    /// <summary>Returns the minimum distance from the point to any floor or obstacle boundary.</summary>
    public double GetBoundaryDistance(Point3d point)
    {
        (int x, int y) = ToCell(point);
        if (IsWalkable(x, y))
        {
            return _boundaryDistances[x, y];
        }

        double minDistance = GetCurveDistance(Floor.Boundary, point);
        foreach (CrowdObstacle obstacle in Obstacles)
        {
            minDistance = Math.Min(minDistance, GetCurveDistance(obstacle.Boundary, point));
        }

        return minDistance;
    }

    private void BuildWalkableMap()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Point3d center = new(
                    MinX + ((x + 0.5) * Floor.CellSize),
                    MinY + ((y + 0.5) * Floor.CellSize),
                    Floor.Plane.OriginZ);

                _cellCenters[x, y] = center;
                _walkable[x, y] = IsInsideFloor(center) && !IsInsideObstacle(center);
            }
        }
    }

    private void BuildBoundaryDistanceMap()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Point3d center = _cellCenters[x, y];
                double floorDist = GetCurveDistance(Floor.Boundary, center);
                _floorBoundaryDist[x, y] = floorDist;
                _floorRepulsionDir[x, y] = GetCurveRepulsionDirection(Floor.Boundary, center);

                double minDistance = floorDist;
                if (_obstacleBoundaryDists != null)
                {
                    for (int i = 0; i < Obstacles.Count; i++)
                    {
                        double obsDist = GetCurveDistance(Obstacles[i].Boundary, center);
                        _obstacleBoundaryDists[i][x, y] = obsDist;
                        _obstacleRepulsionDirs![i][x, y] = GetCurveRepulsionDirection(Obstacles[i].Boundary, center);
                        minDistance = Math.Min(minDistance, obsDist);
                    }
                }

                _boundaryDistances[x, y] = minDistance;
            }
        }
    }

    private bool IsInsideFloor(Point3d point)
    {
        return Floor.Boundary.Contains(point, Plane.WorldXY, _tolerance) != PointContainment.Outside;
    }

    private bool IsInsideObstacle(Point3d point)
    {
        foreach (CrowdObstacle obstacle in Obstacles)
        {
            if (obstacle.Boundary.Contains(point, Plane.WorldXY, _tolerance) != PointContainment.Outside)
            {
                return true;
            }
        }

        return false;
    }

    private Vector3d GetCurveRepulsion(Curve curve, Point3d point, double influenceRadius, bool invert)
    {
        double curveParameter = 0.0;
        Point3d closestPoint;
        if (!curve.ClosestPoint(point, out curveParameter))
        {
            return Vector3d.Zero;
        }

        closestPoint = curve.PointAt(curveParameter);
        double distance = point.DistanceTo(closestPoint);
        if (distance > influenceRadius || distance <= 1e-6)
        {
            return Vector3d.Zero;
        }

        Vector3d direction = point - closestPoint;
        if (invert)
        {
            direction *= -1.0;
        }

        if (!direction.Unitize())
        {
            return Vector3d.Zero;
        }

        double strength = (influenceRadius - distance) / influenceRadius;
        return direction * strength;
    }

    private static double GetCurveDistance(Curve curve, Point3d point)
    {
        if (!curve.ClosestPoint(point, out double curveParameter))
        {
            return double.PositiveInfinity;
        }

        return point.DistanceTo(curve.PointAt(curveParameter));
    }

    private static Vector3d GetCurveRepulsionDirection(Curve curve, Point3d point)
    {
        if (!curve.ClosestPoint(point, out double curveParameter))
        {
            return Vector3d.Zero;
        }

        Vector3d direction = point - curve.PointAt(curveParameter);
        if (direction.Length <= 1e-9)
        {
            return Vector3d.Zero;
        }

        direction.Unitize();
        return direction;
    }
}
