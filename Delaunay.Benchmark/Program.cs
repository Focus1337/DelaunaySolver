using BenchmarkDotNet.Running;
using Delaunay.Benchmark;

var summary = BenchmarkRunner.Run<Benchmark>();