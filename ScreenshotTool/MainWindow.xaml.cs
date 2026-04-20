/*
 * MainWindow.xaml.cs
 */
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace ScreenshotTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private SelectionWindow? _currentOverlay;
    private IntPtr _mainHwnd;
    
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    private const int WM_HOTKEY = 0x312;
    private const int HOTKEY_ID_SCREENSHOT = 1;
    
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_NOREPEAT = 0x4000;

    private const uint VK_2 = 0x32;
    
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _mainHwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(_mainHwnd);
        source?.AddHook(WndProc);

        RegisterGlobalHotKey();
    }

    private void RegisterGlobalHotKey()
    {
        bool success = RegisterHotKey(_mainHwnd, HOTKEY_ID_SCREENSHOT, MOD_CONTROL | MOD_NOREPEAT, VK_2);
        if (!success)
        {
            System.Diagnostics.Debug.WriteLine("热键 Ctrl+2 注册失败");
        }
    }
    
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID_SCREENSHOT)
        {
            // 热键触发：执行截图流程
            TriggerScreenshotByHotKey();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void TriggerScreenshotByHotKey()
    {
        // 避免重复触发
        if (_currentOverlay != null) return;
    
        // 隐藏主窗口
        if (_mainHwnd != IntPtr.Zero)
        {
            ShowWindow(_mainHwnd, SW_HIDE);
        }
    
        BtnCapture.IsEnabled = false;
    
        // 启动截图流程（复用现有异步方法）
        _ = StartScreenshotFlowAsync();
    }
    
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 截图按钮点击事件
    /// </summary>
    private void BtnCapture_Click(object sender, RoutedEventArgs e)
    {
        if (_currentOverlay != null) return;
        
        ShowWindow(_mainHwnd, SW_HIDE);
        
        // 防止重复点击
        BtnCapture.IsEnabled = false;

        _ = StartScreenshotFlowAsync();
    }

    private async Task StartScreenshotFlowAsync()
    {
        try
        {
            await Task.Delay(120);

            // 先截取全屏，作为后续框选层的冻结背景
            var dpi = VisualTreeHelper.GetDpi(this);
            int physicalWidth = (int)(SystemParameters.PrimaryScreenWidth * dpi.DpiScaleX);
            int physicalHeight = (int)(SystemParameters.PrimaryScreenHeight * dpi.DpiScaleY);

            byte[] fullScreenBytes = await Task.Run(() =>
                ScreenshotService.CaptureRegion(0, 0, physicalWidth, physicalHeight));

            // 创建框选窗口，注入背景图
            _currentOverlay = new SelectionWindow();
            _currentOverlay.SetBackgroundImage(fullScreenBytes);
            _currentOverlay.Show();

            _currentOverlay.SelectionCompleted += OnSelectionCompleted;

            _currentOverlay.Closed += (s, e) => HandleOverlayClosed();
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    private async void OnSelectionCompleted(Rect selectedRegion)
    {
        if (_currentOverlay != null)
        {
            _currentOverlay.SelectionCompleted -= OnSelectionCompleted;
        }

        try
        {
            byte[] regionBytes = await Task.Run(() =>
                ScreenshotService.CaptureRegion(
                    (int)selectedRegion.X,
                    (int)selectedRegion.Y,
                    (int)selectedRegion.Width,
                    (int)selectedRegion.Height));

            var toolbar = new ToolbarWindow()
            {
                ImageBytes = regionBytes,
                OverlayWindow = _currentOverlay,
                Owner = _currentOverlay
            };
            
            toolbar.Show();
            toolbar.Activate();
            toolbar.Focus();
            
            toolbar.ShowAtPosition(selectedRegion);

            toolbar.Closed += (s, e) => RestoreMainWindow();
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    private void HandleOverlayClosed()
    {
        CleanupState();
        RestoreMainWindow();
    }

    private void RestoreMainWindow()
    {
        if (_mainHwnd != IntPtr.Zero)
        {
            ShowWindow(_mainHwnd, SW_SHOW);
        }
        this.Show();
        this.Activate();
    }

    private void HandleError(Exception ex)
    {
        System.Windows.MessageBox.Show($"截图失败：\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        CleanupState();
        RestoreMainWindow();
    }

    private void CleanupState()
    {
        if (_currentOverlay != null)
        {
            _currentOverlay.SelectionCompleted -= OnSelectionCompleted;
            _currentOverlay = null;
        }
        BtnCapture.IsEnabled = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _currentOverlay?.FinalCleanup();
        if (_mainHwnd != IntPtr.Zero)
        {
            UnregisterHotKey(_mainHwnd, HOTKEY_ID_SCREENSHOT);
            ShowWindow(_mainHwnd, SW_SHOW);
        }
    }
}