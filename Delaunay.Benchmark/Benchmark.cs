using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Delaunay.Interfaces;

namespace Delaunay.Benchmark;

[SimpleJob(RunStrategy.ColdStart, warmupCount: 10, targetCount: 10)]
[HtmlExporter]
public class Benchmark
{
    private readonly Distribution _distribution = new();
    private IPoint[] _points = null!;

    [Params(100000, 1000000)] public int Count;

    [ParamsAllValues] public Distribution.Type Type { get; set; }

    [GlobalSetup]
    public void GlobalSetup() => 
        _points = _distribution.GetPoints(Type, Count).ToArray();

    [Benchmark]
    public Triangulator Triangulator() => new(_points);
}