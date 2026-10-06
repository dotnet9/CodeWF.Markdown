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
        SetViewSegment("split");
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsSidebarCollapsed))
        {
            UpdateSidebarLayout();
        }
    }

    private void UpdateSidebarLayout()
    {
        if (_viewModel is null)
        {
            return;
        }

        BodyGrid.ColumnDefinitions[0].Width = _viewModel.IsSidebarCollapsed ? new GridLength(56) : new GridLength(216);
        if (_viewModel.IsSidebarCollapsed)
        {
            SidebarBorder.Classes.Add("collapsed");
            DocList.Classes.Add("collapsed");
        }
        else
        {
            SidebarBorder.Classes.Remove("collapsed");
            DocList.Classes.Remove("collapsed");
        }
    }

    private void SetViewSegment(string mode)
    {
        if (ViewSegment.Child is not StackPanel panel)
        {
            return;
        }

        foreach (var child in panel.Children)
        {
            if (child is Button button)
            {
                if (string.Equals(button.Tag as string, mode, StringComparison.Ordinal))
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

    private void ViewModeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string mode } button)
        {
            _viewModel?.SetViewMode(mode);
            SetViewSegment(mode);
        }
    }

    private void FontSizeSelect_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.IsCompactLayout = FontSizeSelect.SelectedIndex == 1;
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

    private void UpdateMaxIcon()
    {
        var maximized = WindowState == WindowState.Maximized;
        MaxPath.Data = maximized
            ? Geometry.Parse("M2.6 3.6h5.8v5.8H2.6z M4 3.6V1.6h6.8v6.8H4z")
            : Geometry.Parse("M1.6 1.6h6.8v6.8H1.6z");
    }
}
