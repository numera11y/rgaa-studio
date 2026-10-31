using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace RgaaStudio.Desktop;

public partial class AboutWindow : Window
{
    private readonly string _licensePath;

    public AboutWindow() : this("Version inconnue", Path.Combine(AppContext.BaseDirectory, "LICENSE.txt")) { }

    public AboutWindow(string version, string licensePath)
    {
        InitializeComponent();
        VersionText.Text = $"Version {version}";
        _licensePath = licensePath;
    }

    private void Header_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OpenLicense_Click(object? sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo { FileName = _licensePath, UseShellExecute = true }); }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            AboutStatus.Text = "La licence est incluse dans le dossier d’installation de RGAA Studio.";
        }
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
