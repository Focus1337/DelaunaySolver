using System.Collections.Generic;
using Delaunay.Interfaces;

namespace Delaunay.Models;

public struct Triangle : ITriangle
{
    public int Index { get; set; }

    public IEnumerable<IPoint> Points { get; set; }

    public Triangle(int t, IEnumerable<IPoint> points)
    {
        Points = points;
        Index = t;
    }
}