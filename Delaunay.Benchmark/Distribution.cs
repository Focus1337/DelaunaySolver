using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Delaunay.Interfaces;
using Delaunay.Models;

namespace Delaunay.Benchmark;

public class Distribution
{
    private readonly Random _random = new();

    public enum Type
    {
        Uniform,
        Gaussian,
        Grid,
        Poisson
    };

    public IEnumerable<IPoint> GetPoints(Type type, int count) =>
        type switch
        {
            Type.Uniform => Uniform(count),
            Type.Gaussian => Gaussian(count),
            Type.Grid => Grid(count),
            Type.Poisson => Poisson(count),
            _ => Enumerable.Empty<IPoint>()
        };

    private IEnumerable<IPoint> Uniform(int count)
    {
        for (var i = 0; i < count; i++)
            yield return new Point(_random.NextDouble() * Math.Pow(10, 3), _random.NextDouble() * Math.Pow(10, 3));
    }

    private IEnumerable<IPoint> Poisson(int count)
    {
        var points = new List<IPoint>();
        const int minDist = 40;
        const int density = 30;
        while (points.Count <= count)
            points.AddRange(UniformPoissonDiskSampler
                .SampleRectangle(new Vector2(0, 0), new Vector2(1000, 1000), minDist, density)
                .Select(v => new Point(v.X, v.Y)).Cast<IPoint>());

        foreach (var point in points)
            yield return point;
    }

    private IEnumerable<IPoint> Grid(int count)
    {
        var size = Math.Sqrt(count);
        for (var i = 0; i < size; i++)
        for (var j = 0; j < size; j++)
            yield return new Point(i, j);
    }

    private IEnumerable<IPoint> Gaussian(int count)
    {
        for (var i = 0; i < count; i++)
            yield return new Point(PseudoNormal() * Math.Pow(10, 3), PseudoNormal() * Math.Pow(10, 3));
    }

    private double PseudoNormal()
    {
        var v = _random.NextDouble() + _random.NextDouble() + _random.NextDouble() + _random.NextDouble() +
                _random.NextDouble() + _random.NextDouble();
        return Math.Min(0.5 * (v - 3) / 3, 1);
    }
}