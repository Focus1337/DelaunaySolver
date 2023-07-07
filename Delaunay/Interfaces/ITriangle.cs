using System.Collections.Generic;

namespace Delaunay.Interfaces;

public interface ITriangle
{
    IEnumerable<IPoint> Points { get; }
    int Index { get; }
}