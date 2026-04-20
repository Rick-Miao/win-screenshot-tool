/*
 * SelectionWindow.xaml.cs
 */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace ScreenshotTool;

public partial class SelectionWindow : Window
{
    private Point _startPoint;
    private bool _isDragging;
    private bool _isSelectionComplete;
    private double _screenWidth;
    private double _screenHeight;
    public Rect? SelectedRegion { get; private set; }

    public event Action<Rect>? SelectionCompleted;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    
    public SelectionWindow()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// 窗口初始化时，精确设置与主屏幕物理尺寸匹配的 DIP 宽高
    /// </summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // 获取当前屏幕的物理像素尺寸
        int physicalW = GetSystemMetrics(SM_CXSCREEN);
        int physicalH = GetSystemMetrics(SM_CYSCREEN);

        // 获取当前屏幕的 DPI 缩放比例
        var dpi = VisualTreeHelper.GetDpi(this);

        _screenWidth = physicalW / dpi.DpiScaleX;
        _screenHeight = physicalH / dpi.DpiScaleY;

        // 计算 WPF 逻辑单位(DIP)尺寸：物理像素 / 缩放比例
        this.Width = _screenWidth;
        this.Height = _screenHeight;
        this.Top = 0;
        this.Left = 0;

        DimMask.Data = new RectangleGeometry(new Rect(0, 0, _screenWidth, _screenHeight));
    }
    
    /// <summary>
    /// 设置全屏冻结背景
    /// </summary>
    public void SetBackgroundImage(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0) return;
        
        var bitmap = new BitmapImage();
        using var ms = new MemoryStream(imageBytes);
        
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad; // 读入内存后立即释放文件流
        bitmap.StreamSource = ms;
        bitmap.EndInit();
        bitmap.Freeze(); // 冻结位图，提升渲染性能并支持跨线程
        
        BgImage.Source = bitmap;
    }
    
    
    /// <summary>
    /// 鼠标左键按下时，开始选择
    /// </summary>
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isSelectionComplete) return;
        _startPoint = e.GetPosition(this);
        _isDragging = true;

        // 显示选择框
        SelectionRect.Visibility = Visibility.Visible;
        // UpdateDimMask(0, 0, 0, 0);
        CaptureMouse();
    }

    /// <summary>
    /// 鼠标移动时，更新选择框
    /// </summary>
    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        
        var currentPoint = e.GetPosition(this);

        // 计算左上角坐标与宽高
        double x = Math.Min(_startPoint.X, currentPoint.X);
        double y = Math.Min(_startPoint.Y, currentPoint.Y);
        double w = Math.Abs(currentPoint.X - _startPoint.X);
        double h = Math.Abs(currentPoint.Y - _startPoint.Y);

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = w;
        SelectionRect.Height = h;

        if (w > 2 && h > 2)
        {
            UpdateDimMask(x, y, w, h);
        }
    }
    
    /// <summary>
    /// 鼠标左键松开，结束选择，计算物理坐标
    /// </summary>
    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    { 
        if (!_isDragging) return;

        _isDragging = false;
        ReleaseMouseCapture();
        
        // 过滤小于5的矩形
        if (SelectionRect.Width > 5 && SelectionRect.Height > 5)
        {
            SelectedRegion = ConvertDipToPhysical(
                Canvas.GetLeft(SelectionRect),
                Canvas.GetTop(SelectionRect),
                SelectionRect.Width,
                SelectionRect.Height);
            _isSelectionComplete = true;
            SelectionCompleted?.Invoke(SelectedRegion.Value);
            this.Cursor = Cursors.Arrow;
            this.MouseLeftButtonDown -= Window_MouseLeftButtonDown;
            this.MouseMove -= Window_MouseMove;
            this.MouseLeftButtonUp -= Window_MouseLeftButtonUp;
            
            // 释放遮罩和画布的命中测试，确保键盘(ESC)仍可用，但鼠标完全失效
            DimMask.IsHitTestVisible = false;
            MainCanvas.IsHitTestVisible = false;
        }
    }
    
    /// <summary>
    /// 键盘按下ESC键时取消选择
    /// </summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // _isDragging = false;
            // ReleaseMouseCapture();
            Close();
        }
    }

    private void UpdateDimMask(double x, double y, double w, double h)
    {
        var screenRect = new RectangleGeometry(new Rect(0, 0, this._screenWidth, this._screenHeight));
        var selectionRect = new RectangleGeometry(new Rect(x, y, w, h));

        DimMask.Data = new CombinedGeometry(GeometryCombineMode.Xor, screenRect, selectionRect);

    }
    
    /// <summary>
    /// 将WPF的坐标转换为物理坐标
    /// </summary>
    private Rect ConvertDipToPhysical(double x, double y, double w, double h)
    {
        // 使用WPF的内置API获取DPI
        var dpi = VisualTreeHelper.GetDpi(this);
        return new Rect(x * dpi.DpiScaleX, y * dpi.DpiScaleY, w * dpi.DpiScaleX, h * dpi.DpiScaleY);
    }

    /// <summary>
    /// 供外部调用，最终销毁覆盖层
    /// </summary>
    public void FinalCleanup()
    {
        if (this.IsVisible)
        {
            this.Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        SelectionCompleted = null;
    }
}