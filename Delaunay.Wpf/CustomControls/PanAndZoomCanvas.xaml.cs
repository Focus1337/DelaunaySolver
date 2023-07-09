using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Delaunay.Wpf.CustomControls;

public partial class PanAndZoomCanvas
{
    private readonly MatrixTransform _transform = new();
    private Point _initialMousePosition;

    private bool _dragging;
    private UIElement? _selectedElement;
    private Vector _draggingDelta;

    public PanAndZoomCanvas()
    {
        InitializeComponent();

        MouseDown += PanAndZoomCanvas_MouseDown;
        MouseUp += PanAndZoomCanvas_MouseUp;
        MouseMove += PanAndZoomCanvas_MouseMove;
        MouseWheel += PanAndZoomCanvas_MouseWheel;
    }

    public float ZoomFactor { get; set; } = 1.1f;

    private void PanAndZoomCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Right)
            _initialMousePosition = _transform.Inverse.Transform(e.GetPosition(this));

        if (e.ChangedButton != MouseButton.Left) return;
        
        if (Children.Contains((UIElement)e.Source))
        {
            _selectedElement = (UIElement)e.Source;
            var mousePosition = Mouse.GetPosition(this);
            var x = GetLeft(_selectedElement);
            var y = GetTop(_selectedElement);
            var elementPosition = new Point(x, y);
            _draggingDelta = elementPosition - mousePosition;
        }

        _dragging = true;
    }

    private void PanAndZoomCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        _selectedElement = null;
    }

    private void PanAndZoomCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.RightButton == MouseButtonState.Pressed)
        {
            var mousePosition = _transform.Inverse.Transform(e.GetPosition(this));
            var delta = Point.Subtract(mousePosition, _initialMousePosition);
            var translate = new TranslateTransform(delta.X, delta.Y);
            _transform.Matrix = translate.Value * _transform.Matrix;

            foreach (UIElement child in Children)
            {
                child.RenderTransform = _transform;
            }
        }

        if (!_dragging || e.LeftButton != MouseButtonState.Pressed) return;

        var x = Mouse.GetPosition(this).X;
        var y = Mouse.GetPosition(this).Y;

        if (_selectedElement == null) return;

        SetLeft(_selectedElement, x + _draggingDelta.X);
        SetTop(_selectedElement, y + _draggingDelta.Y);
    }

    private void PanAndZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scaleFactor = ZoomFactor;
        if (e.Delta < 0) scaleFactor = 1f / scaleFactor;

        var mousePosition = e.GetPosition(this);

        var scaleMatrix = _transform.Matrix;
        scaleMatrix.ScaleAt(scaleFactor, scaleFactor, mousePosition.X, mousePosition.Y);
        _transform.Matrix = scaleMatrix;

        foreach (UIElement child in Children)
        {
            var x = GetLeft(child);
            var y = GetTop(child);

            var sx = x * scaleFactor;
            var sy = y * scaleFactor;

            SetLeft(child, sx);
            SetTop(child, sy);

            child.RenderTransform = _transform;
        }
    }
}