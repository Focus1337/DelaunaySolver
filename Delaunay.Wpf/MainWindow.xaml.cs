using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Delaunay.Interfaces;
using Delaunay.Models;

namespace Delaunay.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private Calculation _calculation;

        private readonly Brush _triangleBrush = Brushes.Black;
        private readonly Brush _voronoiBrush = Brushes.White;
        private readonly Brush _voronoiCircleBrush = Brushes.Blue;
        private readonly Brush _triangleCircleBrush = Brushes.Red;

        private const string TimeFormat = @"hh\:mm\:ss";

        #region Observables

        private IObservable<Point> MouseMoveStream => Observable
            .FromEventPattern<MouseEventArgs>(this, nameof(MouseMove))
            .Select(x => x.EventArgs.GetPosition(this))
            .Select(point => new Point(point.X, point.Y));

        private IObservable<Point> MouseDownStream => Observable
            .FromEventPattern<MouseEventArgs>(this, nameof(MouseLeftButtonDown))
            .Select(evt => evt.EventArgs.GetPosition(this))
            .Select(point => new Point(point.X, point.Y));

        // private IObservable<TimeSpan> Interval(double time = 1) => Observable
        //     .Interval(TimeSpan.FromSeconds(time))
        //     .TimeInterval()
        //     .Scan(TimeSpan.Zero, (result, item) => result += item.Interval)
        // .ObserveOn(Dispatcher);

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
            Interval(1).Subscribe(x => ApplicationTime.Content = x.ToString(TimeFormat));
            InitializeMouseStreams();

            GenerateSamples();
            DrawDiagram();
        }

        private void InitializeMouseStreams()
        {
            MouseMoveStream.Subscribe(point => MousePosition.Content = point.ToString());

            MouseDownStream.Subscribe(x =>
            {
                _points.Add(x);
                if (IsLengthOfPointsValid)
                {
                    DrawDiagram();
                }
            });
        }

        private void GenerateSamples()
        {
            var width = (float)(ActualWidth != 0 ? ActualWidth : Width);
            var height = (float)(ActualHeight != 0 ? ActualHeight : Height);
            var samples = UniformPoissonDiskSampler.SampleCircle(new Vector2(width / 2, height / 3), 220, 40)
                .Select(x => new Point(x.X, x.Y));

            foreach (var sample in samples)
            {
                _points.Add(sample);
                PointsCount.Content = _points.Count.ToString();
                DrawCircle(sample);
            }
        }

        private void DrawDiagram()
        {
            if (!IsLengthOfPointsValid) return;
            ClearDiagram();

            _calculation = new Calculation(_points.ToArray());
            DrawCircles(_points);
            DrawDelaunay();
            DrawVoronoi();
            DrawHull();
        }

        private void DrawVoronoi()
        {
            RefreshDelaunator();
            _calculation.ForEachVoronoiCell((cell) =>
            {
                var polygon = new Polygon
                {
                    Stroke = _voronoiBrush,
                    StrokeThickness = .2,
                    Points = new PointCollection(
                        cell.Points.Select(point => new System.Windows.Point(point.X, point.Y)))
                };

                DrawCircles(cell.Points, _voronoiCircleBrush);
                PlayGround.Children.Add(polygon);
            });
        }

        private void DrawDelaunay()
        {
            RefreshDelaunator();
            _calculation.ForEachTriangleEdge(edge => { DrawLine(edge.P, edge.Q, _triangleBrush); });
        }

        private void DrawHull()
        {
            RefreshDelaunator();
            foreach (var edge in _calculation.GetHullEdges())
            {
                DrawLine(edge.P, edge.Q, Brushes.BlueViolet, .5);
            }
        }

        private void ClearDiagram()
        {
            PlayGround.Children.Clear();
        }

        private void RefreshDelaunator()
        {
            if (!IsLengthOfPointsValid || _points.Count == _calculation?.Points.Length) return;
            _calculation = new Calculation(_points.ToArray());
        }

        #region Canvas

        private void DrawCircles(IEnumerable<IPoint> points, Brush brush = null)
        {
            foreach (var point in points)
            {
                DrawCircle(point, brush);
            }
        }

        private void DrawCircle(IPoint point, Brush brush = null)
        {
            var ellipse = new Ellipse
            {
                Width = 4,
                Height = 4,
                Fill = brush ?? _triangleCircleBrush,
                Stroke = brush ?? _triangleCircleBrush,
            };

            PlayGround.Children.Add(ellipse);
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

            PlayGround.Children.Add(line);
        }

        #endregion Canvas

        #region ClickHandlers

        private void OnClearClick(object sender, System.Windows.RoutedEventArgs e)
        {
            _points.Clear();
            PointsCount.Content = 0.ToString();
            ClearDiagram();
        }

        private void OnGenerateSamplesClick(object sender, System.Windows.RoutedEventArgs e) => GenerateSamples();
        private void OnDrawVoronoiClick(object sender, System.Windows.RoutedEventArgs e) => DrawVoronoi();
        private void OnDrawDelaunayClick(object sender, System.Windows.RoutedEventArgs e) => DrawDelaunay();
        private void OnDrawDiagramClick(object sender, System.Windows.RoutedEventArgs e) => DrawDiagram();
        private void OnDrawHullClick(object sender, System.Windows.RoutedEventArgs e) => DrawHull();

        #endregion ClickHandlers
    }
}