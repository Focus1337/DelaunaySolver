using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System.Linq;
using Delaunay.Interfaces;

namespace Delaunay.Benchmark;

[SimpleJob(RunStrategy.ColdStart, warmupCount: 10, targetCount: 10)]
[HtmlExporter]
public class GetVoronoiCellsBenchmark
{
    private Distribution distribution = new();
    private IPoint[] points;

    [Params(5000)] public int Count;

    [ParamsAllValues] public Distribution.Type Type { get; set; }

    private Calculation _calculation;

    [GlobalSetup]
    public void GlobalSetup()
    {
        points = distribution.GetPoints(Type, Count).ToArray();
        _calculation = new Calculation(points);
    }

    [Benchmark]
    public void GetVoronoiCells()
    {
        using var enumerator = _calculation.GetVoronoiCells().GetEnumerator();
        while (enumerator.MoveNext())
        {
        }
    }
}