using System.ComponentModel;
using Avalonia.Controls;
using AvaloniaEdit.Highlighting;
using CodeWF.Markdown.Lite.Sample.ViewModels;

namespace CodeWF.Markdown.Lite.Sample.Views;

public partial class MainWindow : Window
{
    private bool _syncingEditor;
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        ConfigureMarkdownEditor();
        DataContextChanged += (_, _) => AttachViewModel(DataContext as MainWindowViewModel);
        AttachViewModel(DataContext as MainWindowViewModel);
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
