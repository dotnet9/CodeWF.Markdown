using System.Diagnostics;

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CodeWF.Markdown.Sample.Views;

public partial class StatusBarView : UserControl
{
    private const string RepositoryUrl = "https://github.com/dotnet9/CodeWF.Markdown";

    public StatusBarView()
    {
        InitializeComponent();
    }

    private void GitHubClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            using var process = Process.Start(
                new ProcessStartInfo(RepositoryUrl)
                {
                    UseShellExecute = true
                });
        }
        catch
        {
            // 打开浏览器失败时静默
        }
    }
}