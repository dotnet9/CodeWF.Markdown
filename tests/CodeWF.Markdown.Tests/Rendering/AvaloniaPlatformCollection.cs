using Avalonia;
using Avalonia.Headless;

using Xunit;

namespace CodeWF.Markdown.Tests.Rendering;

/// <summary>
/// 需要真实 Avalonia 平台（Skia 离屏绘制、控件构造）的测试集合夹具。
/// <para>
/// Headless 平台每个进程只能初始化一次，且 Dispatcher 归属初始化线程。测试程序集通过
/// <c>DisableTestParallelization</c> 保证所有测试在同一个线程上串行执行，
/// 因此这里在**首次调用线程**上初始化平台，<see cref="Run(Action)"/> 直接内联执行，
/// 不会出现跨线程持有控件（The calling thread cannot access this object）。
/// </para>
/// </summary>
public sealed class AvaloniaPlatformFixture
{
    private static readonly object SyncRoot = new();
    private static bool _initialized;

    public AvaloniaPlatformFixture() => EnsureInitialized();

    /// <summary>在平台线程上执行（串行测试下即当前线程）。</summary>
    public void Run(Action action)
    {
        EnsureInitialized();
        action();
    }

    /// <summary>在平台线程上执行并取回结果。</summary>
    public T Run<T>(Func<T> action)
    {
        EnsureInitialized();
        return action();
    }

    private static void EnsureInitialized()
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
