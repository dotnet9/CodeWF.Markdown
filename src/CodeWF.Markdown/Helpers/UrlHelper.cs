using System.Diagnostics;

namespace CodeWF.Markdown.Helpers;

internal static class UrlHelper
{
    /// <summary>
    /// 使用系统默认方式打开链接，控件层只负责触发，不内置浏览器。
    /// </summary>
    public static void Open(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Markdown 渲染不能因为外部链接打开失败而中断。
        }
    }
}
