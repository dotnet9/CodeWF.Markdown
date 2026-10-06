using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;

using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Rendering;

using CodeWF.Markdown.Sample.ViewModels;

namespace CodeWF.Markdown.Sample.Views;

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
        ConfigureMarkdownEditor();
        UpdateViewMode();
        DataContextChanged += (_, _) => AttachViewModel(DataContext as MainWindowViewModel);
        AttachViewModel(DataContext as MainWindowViewModel);
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
            // Focus Mode：高亮当前行 + 编辑器前景半透明弱化非当前行
            MarkdownEditor.Options.HighlightCurrentLine = FocusMode;
            MarkdownEditor.Foreground = FocusMode
                ? new SolidColorBrush(Color.Parse("#50101828"))
                : new SolidColorBrush(Color.Parse("#101828"));
            MarkdownEditor.TextArea.TextView.InvalidateVisual();
        }
        else if (e.Property == TypewriterModeProperty)
        {
            if (TypewriterMode)
            {
                MarkdownEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged_Typewriter;
                CenterCaretLine();
            }
            else
            {
                MarkdownEditor.TextArea.Caret.PositionChanged -= Caret_PositionChanged_Typewriter;
            }
        }
    }

    private void Caret_PositionChanged_Typewriter(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(CenterCaretLine, DispatcherPriority.Background);
    }

    private void CenterCaretLine()
    {
        var docLine = MarkdownEditor.Document?.GetLineByOffset(MarkdownEditor.CaretOffset);
        if (docLine is null) return;
        var textview = MarkdownEditor.TextArea.TextView;
        var visualLine = textview.GetVisualLine(docLine.LineNumber);
        if (visualLine is null) return;

        var scrollViewer = this.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scrollViewer is null) return;

        var lineTop = visualLine.VisualTop - scrollViewer.Offset.Y;
        var viewportHeight = scrollViewer.Bounds.Height;
        if (lineTop < viewportHeight * 0.3 || lineTop > viewportHeight * 0.7)
        {
            var target = scrollViewer.Offset.Y + (lineTop - viewportHeight / 2);
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, Math.Max(0, target));
        }
    }

    private void ConfigureMarkdownEditor()
    {
        MarkdownEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("MarkDown");
        MarkdownEditor.TextArea.TextView.Options.HighlightCurrentLine = true;

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

        // Auto Pair：括号/引号自动配对
        MarkdownEditor.TextArea.TextInput += AutoPairOnTextInput;

        // 右键菜单：Copy as Plain Text
        var copyPlain = new MenuItem { Header = "复制为纯文本" };
        copyPlain.Click += (_, _) =>
        {
            var text = string.IsNullOrEmpty(MarkdownEditor.SelectedText)
                ? MarkdownEditor.Text
                : MarkdownEditor.SelectedText;
            TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text ?? string.Empty);
        };
        var menu = new ContextMenu();
        menu.Items.Add(copyPlain);
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "复制" });
        MarkdownEditor.ContextMenu = menu;
    }

    private void AutoPairOnTextInput(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        var closing = e.Text switch
        {
            "(" => ")",
            "[" => "]",
            "{" => "}",
            "\"" => "\"",
            "'" => "'",
            _ => null
        };

        if (closing is null)
        {
            return;
        }

        var caret = MarkdownEditor.CaretOffset;
        var hasSelection = MarkdownEditor.SelectionLength > 0;
        var selectedText = hasSelection ? MarkdownEditor.SelectedText : string.Empty;

        MarkdownEditor.Document.Insert(caret, e.Text + (hasSelection ? selectedText : string.Empty) + closing);
        MarkdownEditor.CaretOffset = caret + 1 + (hasSelection ? selectedText.Length : 0);
        e.Handled = true;
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
