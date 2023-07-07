using BenchmarkDotNet.Running;
using System.Linq;
using Delaunay.Benchmark;

var summary = BenchmarkRunner.Run<DelaunatorBenchmark>();