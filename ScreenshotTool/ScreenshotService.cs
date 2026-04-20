/*
 * ScreenshotService.cs
 */
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Size = System.Drawing.Size;

namespace ScreenshotTool;

public class ScreenshotService
{
    /// <summary>
    /// 截取屏幕指定区域
    /// </summary>
    /// <param name="x">屏幕左上角 X 坐标</param>
    /// <param name="y">屏幕左上角 Y 坐标</param>
    /// <param name="width">截取宽度</param>
    /// <param name="height">截取高度</param>
    public static byte[] CaptureRegion(int x, int y, int width, int height)
    {
        // 创建一个Bitmap对象，用于保存屏幕截图
        using Bitmap bitmap = new(width, height);
        // 创建一个Graphics对象，用于绘制屏幕截图
        using Graphics graphics = Graphics.FromImage(bitmap);

        // 从屏幕（x，y）开始，将屏幕内容复制到bitmap中
        graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height));
        
        // 将bitmap保存为PNG格式
        using MemoryStream ms = new();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
}