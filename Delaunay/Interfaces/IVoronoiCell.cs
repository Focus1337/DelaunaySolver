namespace Delaunay.Interfaces;

public interface IVoronoiCell
{
    IPoint[] Points { get; }
    int Index { get; }
}