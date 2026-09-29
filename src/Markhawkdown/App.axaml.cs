using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Markhawkdown.Services;
using Markhawkdown.ViewModels;
using Markhawkdown.Views;

namespace Markhawkdown;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        MarkdownThemeBridge.Attach(this);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            var viewModel = new MainWindowViewModel(new FileService(window), new DialogService(window));
            window.DataContext = viewModel;
            desktop.MainWindow = window;

            foreach (var path in desktop.Args ?? [])
            {
                if (File.Exists(path))
                    _ = viewModel.OpenPathAsync(path);
            }

            if (viewModel.Documents.Count == 0)
                viewModel.NewDocument();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
