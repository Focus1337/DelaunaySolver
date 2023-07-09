using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Delaunay.Interfaces;
using Point = Delaunay.Models.Point;

namespace Delaunay.Wpf;

public partial class MainWindow
{
    private Triangulator? _triangulator;
    private readonly Brush _triangleBrush = Brushes.Black;
    private readonly Brush _triangleCircleBrush = Brushes.Red;
    private const string TimeFormat = @"hh\:mm\:ss";
    private Stopwatch? _stopwatch;
    private bool IsLengthOfPointsValid => _points.Count > 2;
    private readonly ObservableCollection<IPoint> _points = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void GenerateSamples()
    {
        var width = (float)(ActualWidth != 0 ? ActualWidth : Width);
        var height = (float)(ActualHeight != 0 ? ActualHeight : Height);

        var samplesCircle = UniformPoissonDiskSampler
            .SampleCircle(new Vector2(width / 2, height / 3), (int)CircleRadius.Value, 40)
            .Select(x => new Point(x.X, x.Y));

        // var samplesRectangle =
        //     UniformPoissonDiskSampler
        //         .SampleRectangle(new Vector2(width / 4, height / 6), new Vector2(width / 4 * 3, height / 6 * 4), 40)
        //         .Select(x => new Point(x.X, x.Y));

        var samplesRectangle =
            UniformPoissonDiskSampler
                .SampleRectangle(new Vector2(0, 0), new Vector2(1000, 1000), 40)
                .Select(x => new Point(x.X, x.Y));

        foreach (var sample in samplesRectangle)
        {
            _points.Add(sample);
            PointsCount.Content = _points.Count.ToString();
            DrawCircle(sample);
        }
    }

    private void Triangulate()
    {
        if (!IsLengthOfPointsValid)
            return;

        _stopwatch = Stopwatch.StartNew();
        Refresh();
        TriangulationTime.Content = _stopwatch.Elapsed;

        _stopwatch.Restart();
        _triangulator?.ForEachTriangleEdge(edge => { DrawLine(edge.P, edge.Q, _triangleBrush); });
        RenderTime.Content = _stopwatch.Elapsed;
        _stopwatch.Stop();
    }

    private void ClearDrawArea() =>
        DrawArea.Children.Clear();

    private void Refresh()
    {
        if (!IsLengthOfPointsValid || _points.Count == _triangulator?.Points.Length) return;
        _triangulator = new Triangulator(_points.ToArray());
    }

    private void DrawCircle(IPoint point, Brush? brush = null)
    {
        var ellipse = new Ellipse
        {
            Width = 4,
            Height = 4,
            Fill = brush ?? _triangleCircleBrush,
            Stroke = brush ?? _triangleCircleBrush,
        };

        DrawArea.Children.Add(ellipse);
        Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
        Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);
    }

    private void DrawLine(IPoint startPoint, IPoint endPoint, Brush stroke, double thickness = .3)
    {
        var line = new Line
        {
            X1 = startPoint.X,
            Y1 = startPoint.Y,

            X2 = endPoint.X,
            Y2 = endPoint.Y,
            Stroke = stroke,
            StrokeThickness = thickness
        };

        DrawArea.Children.Add(line);
    }

    private static void SaveCanvasAsImage(Canvas canvas, string filePath)
    {
        var renderBitmap = new RenderTargetBitmap(
            (int)canvas.ActualWidth, (int)canvas.ActualHeight,
            96, 96, PixelFormats.Pbgra32);
        renderBitmap.Render(canvas);

        var bitmapEncoder = new PngBitmapEncoder();
        bitmapEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));

        using var fileStream = new FileStream(filePath, FileMode.Create);
        bitmapEncoder.Save(fileStream);
    }

    private void OnExportClick(object sender, RoutedEventArgs e) =>
        SaveCanvasAsImage(DrawArea,
            $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\t-result-{DateTime.Now:dd-MM-yyyy-h-mm-ss}.png");

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        _points.Clear();
        _stopwatch?.Reset();
        PointsCount.Content = 0.ToString();
        TriangulationTime.Content = TimeSpan.Zero.ToString(TimeFormat);
        RenderTime.Content = TimeSpan.Zero.ToString(TimeFormat);
        ClearDrawArea();
    }

    private void OnGeneratePointsClick(object sender, RoutedEventArgs e)
    {
        while (_points.Count <= (int)PointsMultiplier.Value)
            GenerateSamples();
    }

    private void OnTriangulateClick(object sender, RoutedEventArgs e) => Triangulate();
}