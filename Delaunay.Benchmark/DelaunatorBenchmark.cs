using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Delaunay.Interfaces;

namespace Delaunay.Benchmark;

[SimpleJob(RunStrategy.ColdStart, warmupCount: 10, targetCount: 10)]
[HtmlExporter]
public class DelaunatorBenchmark
{
    private Distribution distribution = new();
    private IPoint[] points;

    [Params(100000, 1000000)] public int Count;

    [ParamsAllValues] public Distribution.Type Type { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        points = distribution.GetPoints(Type, Count).ToArray();
    }

    [Benchmark]
    public Calculation Delaunator()
    {
        return new Calculation(points);
    }
}