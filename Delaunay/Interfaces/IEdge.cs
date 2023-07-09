namespace Delaunay.Interfaces;

/// <summary>
/// Ребро, где P и Q - это точки (IPoint), соединенные с данным ребром
/// </summary>
public interface IEdge
{
    IPoint P { get; }
    IPoint Q { get; }
    int Index { get; }
}