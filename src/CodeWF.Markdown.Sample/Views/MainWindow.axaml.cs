using System.ComponentModel;

using Avalonia.Controls;

using CodeWF.Markdown.Sample.ViewModels;

namespace CodeWF.Markdown.Sample.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachViewModel(DataContext as MainWindowViewModel);
        AttachedToVisualTree += (_, _) => ApplyTypographyResources();
        AttachViewModel(DataContext as MainWindowViewModel);
    }

    private void AttachViewModel(MainWindowViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            ApplyTypographyResources();
            return;
        }

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = viewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        ApplyTypographyResources();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.SelectedTypographyTheme)
            or nameof(MainWindowViewModel.IsCompactLayout)
            or nameof(MainWindowViewModel.CurrentTypographySize)
            or null)
        {
            ApplyTypographyResources();
        }
    }

    private void ApplyTypographyResources()
    {
        if (_viewModel == null)
        {
            return;
        }

        _viewModel.ApplyTypographyResourcesTo(PreviewTabs);
    }
}
