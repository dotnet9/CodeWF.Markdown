using Avalonia;
using Avalonia.Headless;

using Xunit;

namespace CodeWF.Markdown.Tests.Rendering;

/// <summary>
/// 需要真实 Avalonia 平台（Skia 离屏绘制、控件构造）的测试集合夹具。
/// <para>
/// Headless 平台每个进程只能 <c>SetupWithoutStarting</c> 一次，且 Dispatcher 归属初始化线程；
/// xunit 的集合夹具由该集合在**单一测试线程**上创建一次，因此把「初始化」放在这里，
/// 就保证了初始化线程与该集合内所有测试的执行线程一致，不会出现跨线程持有控件
/// （The calling thread cannot access this object）的随机失败。
/// </para>
/// </summary>
public sealed class AvaloniaPlatformFixture
{
    private static readonly object SyncRoot = new();
    private static bool _initialized;

    public AvaloniaPlatformFixture()
    {
        lock (SyncRoot)
        {
            if (_initialized)
            {
                return;
            }

            if (Application.Current is null)
            {
                AppBuilder.Configure<Application>()
                    .UseSkia()
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
                    .SetupWithoutStarting();
            }

            _initialized = true;
        }
    }
}

[CollectionDefinition("AvaloniaPlatform")]
public sealed class AvaloniaPlatformCollection : ICollectionFixture<AvaloniaPlatformFixture>
{
}
