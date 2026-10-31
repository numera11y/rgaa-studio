using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace RgaaStudio.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splash = new SplashWindow();
            desktop.MainWindow = splash;
            splash.Show();
            var startupTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
            startupTimer.Tick += (_, _) =>
            {
                startupTimer.Stop();
                var mainWindow = new MainWindow();
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                splash.Close();
            };
            startupTimer.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
