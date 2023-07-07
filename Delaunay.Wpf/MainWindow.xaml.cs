using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Delaunay.Interfaces;
using Delaunay.Models;

namespace Delaunay.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private Calculation _calculation;

    private readonly Brush _triangleBrush = Brushes.Black;
    private readonly Brush _triangleCircleBrush = Brushes.Red;

    private const string TimeFormat = @"hh\:mm\:ss";

    #region Observables

    private IObservable<TimeSpan> Interval(double time = 1)
    {
        var uiContext = SynchronizationContext.Current;
        return Observable
            .Interval(TimeSpan.FromSeconds(time))
            .TimeInterval()
            .Scan(TimeSpan.Zero, (result, item) => result + item.Interval)
            .ObserveOn(new SynchronizationContextScheduler(uiContext!));
    }

    #endregion Observables

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
        var samples = UniformPoissonDiskSampler.SampleCircle(new Vector2(width / 2, height / 3), 400, 40, 30)
            .Select(x => new Point(x.X, x.Y));

        foreach (var sample in samples)
        {
            _points.Add(sample);
            PointsCount.Content = _points.Count.ToString();
            DrawCircle(sample);
        }
    }

    private void DrawDelaunay()
    {
        if (!IsLengthOfPointsValid)
            return;

        Refresh();
        _calculation.ForEachTriangleEdge(edge => { DrawLine(edge.P, edge.Q, _triangleBrush); });
    }

    private void ClearDrawArea() =>
        DrawArea.Children.Clear();

    private void Refresh()
    {
        if (!IsLengthOfPointsValid || _points.Count == _calculation?.Points.Length) return;
        _calculation = new Calculation(_points.ToArray());
    }

    #region Canvas

    private void DrawCircle(IPoint point, Brush brush = null)
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

    #endregion Canvas

    #region ClickHandlers

    private void OnClearClick(object sender, System.Windows.RoutedEventArgs e)
    {
        _points.Clear();
        PointsCount.Content = 0.ToString();
        ClearDrawArea();
    }

    private void OnGenerateSamplesClick(object sender, System.Windows.RoutedEventArgs e)
    {
        for (var i = 0; i < 1000; i++)
            GenerateSamples();
    }

    private void OnDrawDelaunayClick(object sender, System.Windows.RoutedEventArgs e) => DrawDelaunay();

    #endregion ClickHandlers
}