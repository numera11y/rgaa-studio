using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace RgaaStudio.Desktop;

public partial class SettingsWindow : Window
{
    private readonly string _dataDirectory;
    private readonly Func<DesktopUserPreferences, bool> _save;

    public SettingsWindow() : this(new DesktopUserPreferences(),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Numera11y", "RGAAStudio"), _ => false) { }

    public SettingsWindow(DesktopUserPreferences preferences, string dataDirectory,
        Func<DesktopUserPreferences, bool> save)
    {
        InitializeComponent();
        _dataDirectory = dataDirectory;
        _save = save;
        ShowInspectionPanelCheckBox.IsChecked = preferences.ShowInspectionPanel;
        CollapsePageScopeCheckBox.IsChecked = preferences.CollapsePageScope;
        DataLocationBox.Text = dataDirectory;
    }

    private void Header_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OpenDataFolder_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_dataDirectory);
            Process.Start(new ProcessStartInfo { FileName = _dataDirectory, UseShellExecute = true });
            SettingsStatus.Text = string.Empty;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            SettingsStatus.Text = "Impossible d’ouvrir ce dossier. Tu peux copier son chemin depuis le champ ci-dessus.";
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var preferences = new DesktopUserPreferences(
            ShowInspectionPanelCheckBox.IsChecked == true,
            CollapsePageScopeCheckBox.IsChecked == true);
        if (_save(preferences))
        {
            Close(preferences);
            return;
        }
        SettingsStatus.Text = "Les préférences n’ont pas pu être enregistrées. Vérifie l’accès au stockage local puis réessaie.";
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
    private void Close_Click(object? sender, RoutedEventArgs e) => Close(null);
}
