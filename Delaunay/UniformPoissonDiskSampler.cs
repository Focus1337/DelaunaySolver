using System.Numerics;

namespace Delaunay;

/// <summary>
/// Распределение/выборка Пуассона.
/// Алгоритм для генерации равномерно распределенных точек в двумерном пространстве с минимальным расстоянием между точками.
/// Взял отсюда: http://theinstructionlimit.com/fast-uniform-poisson-disk-sampling-in-c
/// </summary>
public static class UniformPoissonDiskSampler
{
    private static readonly Random Random = new();
    
    // отвечает за плотность заполнения пространства
    private const int DefaultPointsPerIteration = 30;

    static readonly float SquareRootTwo = (float)Math.Sqrt(2);

    private struct Settings
    {
        public Vector2 TopLeft, LowerRight, Center;
        public Vector2 Dimensions;
        public float? RejectionSqDistance;
        public float MinimumDistance;
        public float CellSize;
        public int GridWidth, GridHeight;
    }

    private struct State
    {
        public Vector2?[,] Grid;
        public List<Vector2> ActivePoints, Points;
    }

    public static List<Vector2> SampleCircle(Vector2 center, float radius, float minimumDistance) =>
        SampleCircle(center, radius, minimumDistance, DefaultPointsPerIteration);

    public static List<Vector2> SampleCircle(Vector2 center, float radius, float minimumDistance,
        int pointsPerIteration) =>
        Sample(center - new Vector2(radius), center + new Vector2(radius), radius, minimumDistance,
            pointsPerIteration);

    public static List<Vector2> SampleRectangle(Vector2 topLeft, Vector2 lowerRight, float minimumDistance) =>
        SampleRectangle(topLeft, lowerRight, minimumDistance, DefaultPointsPerIteration);

    public static List<Vector2> SampleRectangle(Vector2 topLeft, Vector2 lowerRight, float minimumDistance,
        int pointsPerIteration) => Sample(topLeft, lowerRight, null, minimumDistance, pointsPerIteration);

    private static List<Vector2> Sample(Vector2 topLeft, Vector2 lowerRight, float? rejectionDistance,
        float minimumDistance, int pointsPerIteration)
    {
        var settings = new Settings
        {
            TopLeft = topLeft,
            LowerRight = lowerRight,
            Dimensions = lowerRight - topLeft,
            Center = (topLeft + lowerRight) / 2,
            CellSize = minimumDistance / SquareRootTwo,
            MinimumDistance = minimumDistance,
            RejectionSqDistance = rejectionDistance == null ? null : rejectionDistance * rejectionDistance
        };
        settings.GridWidth = (int)(settings.Dimensions.X / settings.CellSize) + 1;
        settings.GridHeight = (int)(settings.Dimensions.Y / settings.CellSize) + 1;

        var state = new State
        {
            Grid = new Vector2?[settings.GridWidth, settings.GridHeight],
            ActivePoints = new List<Vector2>(),
            Points = new List<Vector2>()
        };

        AddFirstPoint(ref settings, ref state);

        while (state.ActivePoints.Count != 0)
        {
            var listIndex = Random.Next(state.ActivePoints.Count);

            var point = state.ActivePoints[listIndex];
            var found = false;

            for (var k = 0; k < pointsPerIteration; k++)
                found |= AddNextPoint(point, ref settings, ref state);

            if (!found)
                state.ActivePoints.RemoveAt(listIndex);
        }

        return state.Points;
    }

    private static void AddFirstPoint(ref Settings settings, ref State state)
    {
        var added = false;
        while (!added)
        {
            var d = Random.NextDouble();
            var xr = settings.TopLeft.X + settings.Dimensions.X * d;

            d = Random.NextDouble();
            var yr = settings.TopLeft.Y + settings.Dimensions.Y * d;

            var p = new Vector2((float)xr, (float)yr);
            if (settings.RejectionSqDistance != null &&
                Vector2.DistanceSquared(settings.Center, p) > settings.RejectionSqDistance)
                continue;
            added = true;

            var index = Denormalize(p, settings.TopLeft, settings.CellSize);

            state.Grid[(int)index.X, (int)index.Y] = p;

            state.ActivePoints.Add(p);
            state.Points.Add(p);
        }
    }

    private static bool AddNextPoint(Vector2 point, ref Settings settings, ref State state)
    {
        var found = false;
        var q = GenerateRandomAround(point, settings.MinimumDistance);

        if (!(q.X >= settings.TopLeft.X) || !(q.X < settings.LowerRight.X) ||
            !(q.Y > settings.TopLeft.Y) || !(q.Y < settings.LowerRight.Y) ||
            (settings.RejectionSqDistance != null &&
             !(Vector2.DistanceSquared(settings.Center, q) <= settings.RejectionSqDistance)))
            return found;

        var qIndex = Denormalize(q, settings.TopLeft, settings.CellSize);
        var tooClose = false;

        for (var i = (int)Math.Max(0, qIndex.X - 2);
             i < Math.Min(settings.GridWidth, qIndex.X + 3) && !tooClose;
             i++)
        for (var j = (int)Math.Max(0, qIndex.Y - 2);
             j < Math.Min(settings.GridHeight, qIndex.Y + 3) && !tooClose;
             j++)
            if (state.Grid[i, j].HasValue &&
                Vector2.Distance(state.Grid[i, j]!.Value, q) < settings.MinimumDistance)
                tooClose = true;

        if (tooClose)
            return found;

        found = true;
        state.ActivePoints.Add(q);
        state.Points.Add(q);
        state.Grid[(int)qIndex.X, (int)qIndex.Y] = q;

        return found;
    }

    private static Vector2 GenerateRandomAround(Vector2 center, float minimumDistance)
    {
        var d = Random.NextDouble();
        var radius = minimumDistance + minimumDistance * d;

        d = Random.NextDouble();
        var angle = (float)(Math.PI * 2) * d;

        var newX = radius * Math.Sin(angle);
        var newY = radius * Math.Cos(angle);

        return new Vector2((float)(center.X + newX), (float)(center.Y + newY));
    }

    private static Vector2 Denormalize(Vector2 point, Vector2 origin, double cellSize) =>
        new((int)((point.X - origin.X) / cellSize), (int)((point.Y - origin.Y) / cellSize));
}