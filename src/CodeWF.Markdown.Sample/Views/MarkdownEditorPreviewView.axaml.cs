using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

using CodeWF.Markdown.Editor.Controls;
using CodeWF.Markdown.Editor.Services;

using CodeWF.Markdown.Sample.ViewModels;

namespace CodeWF.Markdown.Sample.Views;

/// <summary>
/// 编辑 / 预览视图：编辑器使用库控件 <see cref="MarkdownEditorView"/>（与 Vex 同一份实现），
/// 这里只负责与 ViewModel 的数据同步、视图模式切换、焦点模式与打字机模式。
/// </summary>
public partial class MarkdownEditorPreviewView : UserControl
{
    private bool _syncingEditor;
    private MainWindowViewModel? _viewModel;

    public static readonly StyledProperty<string> ViewModeProperty =
        AvaloniaProperty.Register<MarkdownEditorPreviewView, string>(
            nameof(ViewMode), defaultValue: "split");

    public string ViewMode
    {
        get => GetValue(ViewModeProperty);
        set => SetValue(ViewModeProperty, value);
    }

    public static readonly StyledProperty<bool> FocusModeProperty =
        AvaloniaProperty.Register<MarkdownEditorPreviewView, bool>(
            nameof(FocusMode), defaultValue: false);

    public bool FocusMode
    {
        get => GetValue(FocusModeProperty);
        set => SetValue(FocusModeProperty, value);
    }

    public static readonly StyledProperty<bool> TypewriterModeProperty =
        AvaloniaProperty.Register<MarkdownEditorPreviewView, bool>(
            nameof(TypewriterMode), defaultValue: false);

    public bool TypewriterMode
    {
        get => GetValue(TypewriterModeProperty);
        set => SetValue(TypewriterModeProperty, value);
    }

    public MarkdownEditorPreviewView()
    {
        InitializeComponent();
        MarkdownEditor.MarkdownChanged += (_, text) => PushTextToViewModel(text);
        DataContextChanged += (_, _) => AttachViewModel(DataContext as MainWindowViewModel);
        AttachViewModel(DataContext as MainWindowViewModel);
        UpdateViewMode();
    }

    private void UpdateViewMode()
    {
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
        else if (e.Property == FocusModeProperty)
        {
            // Focus Mode：高亮当前行 + 非当前行前景弱化（对齐原型）
            MarkdownEditor.HighlightCurrentLine = FocusMode;
            MarkdownEditor.Foreground = FocusMode
                ? new SolidColorBrush(Color.Parse("#50101828"))
                : new SolidColorBrush(Color.Parse("#101828"));
        }
        else if (e.Property == TypewriterModeProperty)
        {
            if (TypewriterMode)
            {
                MarkdownEditor.SelectionChanged += OnTypewriterCaretChanged;
                CenterCaretLine();
            }
            else
            {
                MarkdownEditor.SelectionChanged -= OnTypewriterCaretChanged;
            }
        }
    }

    private void OnTypewriterCaretChanged(object? sender, MarkdownCaretEventArgs e) =>
        Dispatcher.UIThread.Post(CenterCaretLine, DispatcherPriority.Background);

    /// <summary>打字机模式：每次光标移动都把当前行带回视口。</summary>
    private void CenterCaretLine() => MarkdownEditor.Editor.TextArea.Caret.BringCaretToView();

    /// <summary>将编辑器选区（或光标处）用指定 Markdown 标记包裹。</summary>
    public void WrapSelection(string prefix, string suffix) => MarkdownEditor.WrapSelection(prefix, suffix);

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

    private void PushTextToViewModel(string text)
    {
        if (_syncingEditor || _viewModel is null)
        {
            return;
        }

        if (_viewModel.Markdown != text)
        {
            _viewModel.Markdown = text;
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
        MarkdownEditor.SetText(text);
        _syncingEditor = false;
    }
}
