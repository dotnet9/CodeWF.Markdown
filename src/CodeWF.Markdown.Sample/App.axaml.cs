using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using CodeWF.Markdown.Sample.ViewModels;
using CodeWF.Markdown.Sample.Views;

using Lang.Avalonia;
using Lang.Avalonia.Json;
using System.Globalization;

namespace CodeWF.Markdown.Sample;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
		var langPlugin = new JsonLangPlugin
		{
			ResourceFolder = Path.Combine(AppContext.BaseDirectory, "I18n")
		};
		I18nManager.Instance.Register(langPlugin, new CultureInfo("zh-CN"), out _);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = new MainWindow
			{
				DataContext = new MainWindowViewModel()
			};
		}

		base.OnFrameworkInitializationCompleted();
	}
}
