/*
 * ToolbarWindow.xaml.cs
 */
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ScreenshotTool;

public partial class ToolbarWindow : Window
{
    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
    
    public SelectionWindow? OverlayWindow { get; set; }
    
    public ToolbarWindow()
    {
        InitializeComponent();
        
        // this.Deactivated += (s, e) => this.Activate();

        this.Closed += (s, e) =>
        {
            OverlayWindow?.FinalCleanup();
            OverlayWindow = null;
        };
    }
    
    public void ShowAtPosition(Rect region)
    {
        this.Show();
        this.UpdateLayout();

        double left = region.Left + (region.Width / 2) - (this.ActualWidth / 2);
        double top = region.Bottom + 10;

        left = Math.Max(0, Math.Min(left, SystemParameters.WorkArea.Width - this.ActualWidth));
        top = Math.Max(0, Math.Min(top, SystemParameters.WorkArea.Height - this.ActualHeight));
        
        this.Left = left;
        this.Top = top;
        
        this.Activate();
        this.Focus();
    }

    private void Toolbar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.OriginalSource is Border)
        {
            DragMove();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.Close();
            return;
        }
        base.OnKeyDown(e);
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string fileName = $"Region_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fullPath = Path.Combine(desktopPath, fileName);

            await File.WriteAllBytesAsync(fullPath, ImageBytes);
            // MessageBox.Show($"已保存至桌面：\n{fileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            this.Close();
        }
    }

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var bitmap = new BitmapImage();
            using (var ms = new MemoryStream(ImageBytes))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
            }

            var dataObject = new DataObject();
            dataObject.SetImage(bitmap);
            dataObject.SetData("PNG", new MemoryStream(ImageBytes));
            Clipboard.SetDataObject(dataObject, true);
            
            // ShowTemporaryTooltip("✓ 已复制到剪贴板");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"复制失败：\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            this.Close();
        }
    }
    private void BtnAnnotate_Click(object sender, RoutedEventArgs e)   
    {
        MessageBox.Show("复制功能开发中...", "提示");
        this.Close();
    }
    private void BtnPin_Click(object sender, RoutedEventArgs e)    
    {
        MessageBox.Show("复制功能开发中...", "提示");
        this.Close();
    }
    
}