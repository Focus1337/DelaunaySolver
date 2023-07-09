namespace Delaunay.Interfaces;

/// <summary>
/// Представляет собой определенную точку, которая имеет координаты X, Y
/// </summary>
public interface IPoint
{
    double X { get; set; }
    double Y { get; set; }
}