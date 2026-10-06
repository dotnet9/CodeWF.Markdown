using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

using CodeWF.Markdown.Sample.ViewModels;

namespace CodeWF.Markdown.Sample.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        ExtendClientAreaToDecorationsHint = true;
        DataContext = new MainWindowViewModel();
        _viewModel = DataContext as MainWindowViewModel;
        _viewModel?.ConfigureHost(this);
        _viewModel?.PropertyChanged += ViewModel_PropertyChanged;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        SetViewSegment("split");
    }

    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsSidebarCollapsed))
        {
            UpdateSidebarLayout();
        }
    }

    private void UpdateSidebarLayout()
    {
        if (Vm is null)
        {
            return;
        }

        var collapsed = Vm.IsSidebarCollapsed;
        if (this.GetControl<Grid>("BodyGrid") is { } bodyGrid
            && bodyGrid.ColumnDefinitions.Count > 0)
        {
            bodyGrid.ColumnDefinitions[0].Width = collapsed
                ? new GridLength(56)
                : new GridLength(216);
        }

        if (this.GetControl<Border>("SidebarBorder") is { } sidebar)
        {
            if (collapsed)
            {
                sidebar.Classes.Add("collapsed");
            }
            else
            {
                sidebar.Classes.Remove("collapsed");
            }
        }
    }

    private void SetViewSegment(string mode)
    {
        if (this.GetControl<Border>("ViewSegment") is { Child: StackPanel panel })
        {
            foreach (var child in panel.Children)
            {
                if (child is Button button)
                {
                    var active = string.Equals(button.Tag as string, mode, StringComparison.Ordinal);
                    if (active)
                    {
                        button.Classes.Add("on");
                    }
                    else
                    {
                        button.Classes.Remove("on");
                    }
                }
            }
        }
    }

    private void ViewModeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string mode } button)
        {
            Vm?.SetViewMode(mode);
            SetViewSegment(mode);
        }
    }

    private void FontSizeSelect_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Vm is not null)
        {
            Vm.IsCompactLayout = FontSizeSelect.SelectedIndex == 1;
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void TitleBar_DoubleTapped(object? sender, TappedEventArgs e)
    {
        MaximizeClick(sender, e);
    }

    private void MinimizeClick(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        UpdateMaxIcon();
    }

    private void CloseClick(object? sender, RoutedEventArgs e) => Close();

    private void GitHubClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo("https://github.com/dotnet9/CodeWF.Markdown")
                {
                    UseShellExecute = true
                });
        }
        catch
        {
            // 打开浏览器失败时静默
        }
    }

    private void UpdateMaxIcon()
    {
        if (this.GetControl<Avalonia.Controls.Shapes.Path>("MaxPath") is not { } path)
        {
            return;
        }

        path.Data = WindowState == WindowState.Maximized
            ? Geometry.Parse("M2.6 3.6h5.8v5.8H2.6z M4 3.6V1.6h6.8v6.8H4z")
            : Geometry.Parse("M1.6 1.6h6.8v6.8H1.6z");
    }
}
