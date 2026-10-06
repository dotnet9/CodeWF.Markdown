using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;

using AvaloniaEdit.Highlighting;

using CodeWF.Markdown.Sample.ViewModels;

namespace CodeWF.Markdown.Sample.Views;

public partial class MarkdownEditorPreviewView : UserControl
{
    private bool _syncingEditor;
    private MainWindowViewModel? _viewModel;


    public static readonly StyledProperty<string> ViewModeProperty =
        AvaloniaProperty.Register<MarkdownEditorPreviewView, string>(
            nameof(ViewMode), defaultValue: "split");

    /// <summary>
    /// 编辑 / 分栏 / 预览 / 对比：控制编辑器列与预览列的宽度分配。
    /// </summary>
    public string ViewMode
    {
        get => GetValue(ViewModeProperty);
        set => SetValue(ViewModeProperty, value);
    }

    public MarkdownEditorPreviewView()
    {
        InitializeComponent();
        ConfigureMarkdownEditor();
        UpdateViewMode();
        DataContextChanged += (_, _) => AttachViewModel(DataContext as MainWindowViewModel);
        AttachViewModel(DataContext as MainWindowViewModel);
    }

    private void UpdateViewMode()
    {
        // pair（对比模式）由宿主整体切换到 MarkdownViewerPairDemoView，这里视同分栏。
        switch (ViewMode)
        {
            case "edit":
                LayoutGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                LayoutGrid.ColumnDefinitions[0].MinWidth = 0;
                LayoutGrid.ColumnDefinitions[1].Width = new GridLength(0);
                LayoutGrid.ColumnDefinitions[2].Width = new GridLength(0);
                LayoutGrid.ColumnDefinitions[2].MinWidth = 0;
                break;
            case "preview":
                LayoutGrid.ColumnDefinitions[0].Width = new GridLength(0);
                LayoutGrid.ColumnDefinitions[0].MinWidth = 0;
                LayoutGrid.ColumnDefinitions[1].Width = new GridLength(0);
                LayoutGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                LayoutGrid.ColumnDefinitions[2].MinWidth = 0;
                break;
            default:
                LayoutGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                LayoutGrid.ColumnDefinitions[0].MinWidth = 300;
                LayoutGrid.ColumnDefinitions[1].Width = new GridLength(6);
                LayoutGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                LayoutGrid.ColumnDefinitions[2].MinWidth = 360;
                break;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == ViewModeProperty)
        {
            UpdateViewMode();
        }
    }

    private void ConfigureMarkdownEditor()
    {
        MarkdownEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("MarkDown");
        MarkdownEditor.TextChanged += (_, _) =>
        {
            if (_syncingEditor || _viewModel == null)
            {
                return;
            }

            var text = MarkdownEditor.Text ?? string.Empty;
            if (_viewModel.Markdown != text)
            {
                _viewModel.Markdown = text;
            }
        };
    }

    private void AttachViewModel(MainWindowViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            SyncEditorFromViewModel();
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

        SyncEditorFromViewModel();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.Markdown) or null)
        {
            SyncEditorFromViewModel();
        }
    }

    private void SyncEditorFromViewModel()
    {
        if (_viewModel == null)
        {
            return;
        }

        var text = _viewModel.Markdown ?? string.Empty;
        if (MarkdownEditor.Text == text)
        {
            return;
        }

        _syncingEditor = true;
        MarkdownEditor.Text = text;
        _syncingEditor = false;
    }
}
