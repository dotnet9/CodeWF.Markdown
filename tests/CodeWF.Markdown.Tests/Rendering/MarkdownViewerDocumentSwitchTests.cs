using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using CodeWF.Markdown.Controls;
using Xunit;

namespace CodeWF.Markdown.Tests.Rendering;

[Collection("AvaloniaPlatform")]
public sealed class MarkdownViewerDocumentSwitchTests(AvaloniaPlatformFixture platform)
{
    [Fact]
    public void SwitchFromCustomContainerToImages_PreservesRenderedBlockIndices() => platform.Run(() =>
    {
        var (window, viewer, host) = CreateViewer("# Initial\n\nInitial paragraph");
        try
        {
            var container = "# HTML sample\n\n::: warning\nUnsupported container content\n:::\n\nFollowing paragraph";
            var images = "# Images and links\n\n![Missing image](not-found.png)\n\n[Link](https://example.com)";
            foreach (var source in new[] { container, images, container, images })
            {
                SwitchDocument(viewer, source);
                Assert.Equal(viewer.CurrentModel.Blocks.Count, host.Children.Count);
                Assert.Equal(source, viewer.CurrentModel.Source);
                if (source == container)
                {
                    Assert.Equal("::: warning\nUnsupported container content\n:::",
                        Assert.IsAssignableFrom<SelectableTextBlock>(host.Children[1]).Text);
                }
            }
        }
        finally
        {
            window.Close();
        }
    });

    [Fact]
    public void EditBeforeCustomContainer_ReusesFollowingControlsWithCorrectSourceBounds() => platform.Run(() =>
    {
        var (window, viewer, host) = CreateViewer("# Initial\n\nInitial paragraph");
        try
        {
            var source = "# Title\n\n::: warning\nContainer text\n:::\n\nFollowing paragraph";
            SwitchDocument(viewer, source);
            // 首次后台解析建立快照；之后应按模型索引增量复用未变化的后缀。
            var container = host.Children[1];
            var following = host.Children[2];
            var changed = source.Replace("# Title", "# A longer title");
            SwitchDocument(viewer, changed);

            Assert.Equal(MarkdownRenderMode.Incremental, viewer.LastRenderMode);
            Assert.Equal(3, host.Children.Count);
            Assert.Same(container, host.Children[1]);
            Assert.Same(following, host.Children[2]);
            Assert.True(viewer.TryGetSourceOffsetBounds(changed.IndexOf("Following paragraph", StringComparison.Ordinal), out var bounds));
            Assert.True(bounds.Height > 0);
        }
        finally
        {
            window.Close();
        }
    });

    private static (Window Window, MarkdownViewer Viewer, StackPanel Host) CreateViewer(string source)
    {
        var host = new StackPanel();
        var viewer = new MarkdownViewer
        {
            Markdown = source,
            Template = new FuncControlTemplate<MarkdownViewer>((_, scope) =>
            {
                scope.Register("PART_DocumentHost", host);
                return host;
            })
        };
        var window = new Window { Width = 600, Height = 500, Content = viewer };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        SwitchDocument(viewer, source + "\n\nPrime background rendering");
        return (window, viewer, host);
    }

    private static void SwitchDocument(MarkdownViewer viewer, string source)
    {
        viewer.Markdown = source;
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (viewer.CurrentModel.Source != source)
        {
            Dispatcher.UIThread.RunJobs();
            if (DateTime.UtcNow > deadline) throw new TimeoutException("Document rendering did not finish");
            Thread.Sleep(10);
        }
        Dispatcher.UIThread.RunJobs();
    }
}
