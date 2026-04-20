using System.Windows;
using System.Runtime.InteropServices;


namespace ScreenshotTool;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 程序启动入口
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        // 启用高分屏感知，防止截图坐标偏移或界面模糊
        EnableHighDpi();

        // 保持默认，启动MainWindow
        base.OnStartup(e);
    }
    
    /// <summary>
    /// 设置进程DPI感知级别
    /// </summary>
    private static void EnableHighDpi()
    {
        try
        {
            // -4 = DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
            SetProcessDpiAwarenessContext(-4);
        }
        catch(Exception  e)
        {
            Console.WriteLine(e.Message);
        }
    }
    
    /// <summary>
    /// Win32 API 声明
    /// </summary>
    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(int value);
}


