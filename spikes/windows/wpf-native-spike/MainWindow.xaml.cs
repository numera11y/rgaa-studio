using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace RgaaAuditor.Windows;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<OpenAuditTab> openTabs = [];
    private readonly string profilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RgaaStudioSpike", "WebView2Profile");
    private readonly string auditRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RgaaStudioSpike", "AuditDossier");
    private string currentAuditId = "";
    private string CurrentDossierPath => Path.Combine(auditRoot, currentAuditId);
    private string fixtureUri = "";
    private string? latestProof;
    private bool inspectMode;
    private bool windowIsActive = true;
    private string? draggedTabId;
    private bool restoreMenuFocusAfterInspect;
    private bool optionsMenuCommandInvoked;
    private bool focusMenuTriggerAfterCommand;
    private MenuItem? keyboardFocusedMenuItem;
    private string selectedTag = "";
    private string selectedText = "";
    private Task? browserInitializationTask;

    public MainWindow()
    {
        InitializeComponent();
        SidePanelToggle.Tag = SidePanelToggle.IsChecked == true;
        SidePanelActiveMarker.Visibility = SidePanelToggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        foreach (var menuItem in OptionsMenu.Items.OfType<MenuItem>()) menuItem.Icon = null;
        if (FaviconConsent.Content is TextBlock faviconDisclosure)
        {
            faviconDisclosure.Text = "À la création, si l’hôte HTTPS public passe les contrôles, l’application demande /favicon.ico séparément du navigateur. Le site reçoit la requête et votre adresse IP ; le résolveur DNS reçoit le nom d’hôte. Aucun cookie ni identifiant n’est envoyé. Option facultative et non bloquante : si l’hôte est refusé ou si le fetch échoue, le globe local s’affiche.";
            FaviconConsent.ToolTip = faviconDisclosure.Text;
            AutomationProperties.SetHelpText(FaviconConsent, faviconDisclosure.Text);
        }
        OpenTabs.ItemsSource = openTabs;
        OpenTabs.ItemTemplate = (DataTemplate)FindResource("FaviconTabTemplate");
        AuditList.ItemTemplate = (DataTemplate)FindResource("FaviconAuditCardTemplate");
        UpdateInspectMenuCommands();
        OptionsMenu.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(OptionsMenu_ItemInvoked));
        OptionsMenu.Opened += OptionsMenu_Opened;
        OptionsMenu.PreviewKeyDown += OptionsMenu_PreviewKeyDown;
        OptionsMenu.PreviewMouseMove += OptionsMenu_PreviewMouseMove;
        Activated += MainWindow_Activated;
        Deactivated += MainWindow_Deactivated;
        UpdateProjectListState();
        StateChanged += (_, _) => SetMaximizeIcon();
        Loaded += async (_, _) => await EnsureBrowserAsync();
        Loaded += (_, _) => RefreshAuditList();
        PreviewKeyDown += Window_PreviewKeyDown;
        SetMaximizeIcon();
    }

    private void SetMaximizeIcon()
    {
        var maximized = WindowState == WindowState.Maximized;
        var action = maximized ? "Restaurer la fenêtre" : "Agrandir la fenêtre";
        MaximizeButton.ToolTip = action;
        AutomationProperties.SetName(MaximizeButton, action);
        MaximizeGlyph.Visibility = maximized ? Visibility.Collapsed : Visibility.Visible;
        RestoreGlyph.Visibility = maximized ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MainWindow_Activated(object? sender, EventArgs e)
    {
        windowIsActive = true;
        TopToolbar.Background = (Brush)FindResource("ToolbarSurface");
        TopToolbar.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE9, 0xDB, 0xFF));
        RefreshTabAutomationNames();
    }

    private void MainWindow_Deactivated(object? sender, EventArgs e)
    {
        windowIsActive = false;
        TopToolbar.Background = (Brush)FindResource("InactiveToolbarSurface");
        TopToolbar.BorderBrush = new SolidColorBrush(Color.FromRgb(0xD2, 0xD4, 0xD6));
        if (OptionsMenu.IsOpen) OptionsMenu.IsOpen = false;
        OptionsButton.Tag = false;
        AutomationProperties.SetItemStatus(OptionsButton, "Fermé");
        RefreshTabAutomationNames();
        // Deactivation changes only window chrome/menu state; inspection itself remains active.
    }

    private Task EnsureBrowserAsync() => browserInitializationTask ??= InitializeBrowserCoreAsync();

    private async Task InitializeBrowserCoreAsync()
    {
        try
        {
            Directory.CreateDirectory(profilePath);
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: profilePath);
            await Browser.EnsureCoreWebView2Async(environment);
            var core = Browser.CoreWebView2;
            var fixture = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Assets", "fixture.html"));
            if (!File.Exists(fixture)) throw new FileNotFoundException("La fixture locale est introuvable.", fixture);
            fixtureUri = new Uri(fixture).AbsoluteUri;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.IsGeneralAutofillEnabled = false;
            core.Settings.IsPasswordAutosaveEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsWebMessageEnabled = true;
            core.WebMessageReceived += OnWebMessageReceived;
            core.NavigationStarting += (_, e) =>
            {
                if (!string.Equals(e.Uri, fixtureUri, StringComparison.OrdinalIgnoreCase))
                {
                    e.Cancel = true;
                    SelectionSummary.Text = "Navigation externe bloquée dans ce spike synthétique.";
                }
            };
            core.NewWindowRequested += (_, e) => { e.Handled = true; SelectionSummary.Text = "Les nouvelles fenêtres sont bloquées dans ce spike."; };
            core.PermissionRequested += (_, e) => { e.State = CoreWebView2PermissionState.Deny; e.Handled = true; };
            core.DownloadStarting += (_, e) => { e.Cancel = true; ProofStatus.Text = "Téléchargement bloqué dans ce spike synthétique."; };
            SelectionSummary.Text = "Aucun élément sélectionné. Ouvrez Options, activez l’inspection, puis utilisez Tab et F6 sur un élément de la fixture.";
            core.Navigate(fixtureUri);
        }
        catch (Exception ex) { SelectionSummary.Text = "WebView2 indisponible : " + ex.Message; ProofStatus.Text = "Vérifiez que le runtime WebView2 est installé."; }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            if (!string.Equals(e.Source, fixtureUri, StringComparison.OrdinalIgnoreCase) || e.WebMessageAsJson.Length > 2048) return;
            using var doc = JsonDocument.Parse(e.WebMessageAsJson, new JsonDocumentOptions { MaxDepth = 4 });
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String || type.GetString() != "selection") return;
            var tag = SafeField(root, "tag", 24).ToUpperInvariant();
            if (tag is not ("INPUT" or "BUTTON" or "A" or "LABEL" or "SELECT" or "TEXTAREA" or "H1" or "P" or "FORM")) return;
            var text = SafeField(root, "text", 160);
            var aria = SafeField(root, "aria", 100);
            var state = SafeField(root, "state", 80);
            selectedTag = tag;
            selectedText = text;
            SelectionSummary.Text = $"{tag}\nTexte visible : {text}\nIndication ARIA : {aria}\nÉtat : {state}";
            if (tag == "FORM" || tag is "INPUT" or "LABEL" or "SELECT" or "TEXTAREA") RuleCombo.SelectedIndex = 1;
            else if (tag == "A") RuleCombo.SelectedIndex = 2;
            else if (tag == "BUTTON") RuleCombo.SelectedIndex = 3;
        }
        catch (JsonException) { }
        catch (InvalidOperationException) { }
    }

    private static string SafeField(JsonElement root, string name, int max)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String) return "";
        return new string((value.GetString() ?? "").Where(c => !char.IsControl(c) || c is '\n' or '\t').Take(max).ToArray());
    }

    private async void Inspect_Click(object sender, RoutedEventArgs e) => await SetInspectModeAsync(true);
    private async void InspectMenu_Click(object sender, RoutedEventArgs e)
    {
        MarkMenuAction(focusTrigger: true);
        await SetInspectModeAsync(true, returnFocusToTrigger: true);
    }
    private async void ExitInspect_Click(object sender, RoutedEventArgs e)
    {
        MarkMenuAction(focusTrigger: true);
        await SetInspectModeAsync(false, returnFocusToTrigger: true);
    }
    private async Task SetInspectModeAsync(bool enabled, bool returnFocusToTrigger = false)
    {
        if (enabled && !inspectMode) restoreMenuFocusAfterInspect = true;
        inspectMode = enabled;
        InspectStatusBadge.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetItemStatus(InspectStatusBadge, enabled ? "Inspection active" : "Inspection inactive");
        AutomationProperties.SetName(InspectStatusBadge, "Inspection active · Échap hors menu");
        AutomationProperties.SetItemStatus(InspectStatusText, enabled ? "Inspection active" : "Inspection inactive");
        var statusPeer = UIElementAutomationPeer.FromElement(InspectStatusText) ?? UIElementAutomationPeer.CreatePeerForElement(InspectStatusText);
        statusPeer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        UpdateInspectMenuCommands();
        if (Browser.CoreWebView2 is not null)
            await Browser.CoreWebView2.ExecuteScriptAsync($"window.fixtureSetInspect({(enabled ? "true" : "false")})");
        if (returnFocusToTrigger)
        {
            OptionsButton.Focus();
            if (!enabled) restoreMenuFocusAfterInspect = false;
        }
        else if (enabled) Browser.Focus();
        else if (restoreMenuFocusAfterInspect) { OptionsButton.Focus(); restoreMenuFocusAfterInspect = false; }
    }
    private void UpdateInspectMenuCommands()
    {
        if (OptionsButton.ContextMenu is ContextMenu menu)
        {
            var inspectItem = menu.Items.OfType<MenuItem>().FirstOrDefault(item => item.Name == "InspectMenuItem");
            var exitItem = menu.Items.OfType<MenuItem>().FirstOrDefault(item => item.Name == "ExitInspectMenuItem");
            if (inspectItem is not null) inspectItem.IsEnabled = !inspectMode;
            if (exitItem is not null) exitItem.IsEnabled = inspectMode;
            if (inspectItem is not null) AutomationProperties.SetHelpText(inspectItem, inspectMode ? "Indisponible : l’inspection est déjà active." : "Démarre l’inspection des éléments.");
            if (exitItem is not null) AutomationProperties.SetHelpText(exitItem, inspectMode ? "Termine l’inspection." : "Indisponible : aucune inspection active.");
        }
    }

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (OptionsMenu.IsOpen)
        {
            HandleOptionsMenuKey(e);
            return;
        }
        if (OpenTabs.IsKeyboardFocusWithin && Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.W)
        {
            var focusedTabId = (Keyboard.FocusedElement as FrameworkElement)?.Tag as string;
            CloseTab(focusedTabId ?? currentAuditId);
            e.Handled = true;
            return;
        }
        if (OpenTabs.IsKeyboardFocusWithin && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key is Key.Left or Key.Right)
        {
            var focusedTabId = (Keyboard.FocusedElement as FrameworkElement)?.Tag as string;
            var targetTabId = focusedTabId is not null && openTabs.Any(tab => tab.Id == focusedTabId) ? focusedTabId : currentAuditId;
            var index = openTabs.ToList().FindIndex(tab => tab.Id == targetTabId);
            var destination = index + (e.Key == Key.Left ? -1 : 1);
            if (index >= 0 && destination >= 0 && destination < openTabs.Count)
            {
                openTabs.Move(index, destination);
                _ = Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!FocusOpenTab(targetTabId)) OptionsButton.Focus();
                }), DispatcherPriority.ContextIdle);
                e.Handled = true;
            }
            return;
        }
        if (e.Key == Key.Escape && inspectMode)
        {
            await SetInspectModeAsync(false, returnFocusToTrigger: true);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.F6)
        {
            if (!inspectMode)
            {
                await SetInspectModeAsync(true, returnFocusToTrigger: true);
                e.Handled = true;
            }
            else if (Browser.IsKeyboardFocusWithin && Browser.CoreWebView2 is not null)
            {
                await SelectFocusedFixtureElementAsync();
                e.Handled = true;
            }
        }
    }

    private async Task SelectFocusedFixtureElementAsync()
    {
        if (Browser.CoreWebView2 is not null) await Browser.CoreWebView2.ExecuteScriptAsync("window.fixtureSelectFocused && window.fixtureSelectFocused()");
    }

    private void Decision_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (SaveReviewButton is null) return;
        var value = (DecisionCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        SaveReviewButton.IsEnabled = value is "Conforme" or "Non conforme" or "Non applicable" or "Non vérifiable";
    }

    private async void SaveReview_Click(object sender, RoutedEventArgs e)
    {
        var decision = (DecisionCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        if ((decision is "Non applicable" or "Non vérifiable") && string.IsNullOrWhiteSpace(JustificationBox.Text))
        { ReviewStatus.Text = "Saisissez une justification avant d’enregistrer cette décision."; JustificationBox.Focus(); return; }
        var record = new SyntheticReviewRecord(Guid.NewGuid().ToString("N"), currentAuditId, CurrentPageHeader.Text, RuleCombo.Text, decision, JustificationBox.Text.Trim(), false, DateTimeOffset.UtcNow);
        Directory.CreateDirectory(Path.Combine(CurrentDossierPath, "decisions"));
        await File.WriteAllTextAsync(Path.Combine(CurrentDossierPath, "decisions", $"{record.UpdatedAt:yyyyMMddHHmmssfffffff}-{record.Id}.json"), JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
        TouchAuditManifest();
        ReviewStatus.Text = $"Décision humaine enregistrée localement : {decision}. L’achèvement de la revue reste une étape distincte.";
        CompleteReviewButton.IsEnabled = true;
    }

    private void RestoreReviewRecord()
    {
        DecisionCombo.SelectedIndex = 0;
        JustificationBox.Clear();
        SaveReviewButton.IsEnabled = false;
        CompleteReviewButton.IsEnabled = false;
        var records = GetReviewRecords();
        if (records.Count == 0)
        {
            ReviewStatus.Text = "La revue de démonstration reste à faire.";
            return;
        }
        try
        {
            var record = records[^1];
            if (record is null) return;
            for (var index = 0; index < DecisionCombo.Items.Count; index++)
            {
                if ((DecisionCombo.Items[index] as ComboBoxItem)?.Content?.ToString() == record.HumanDecision)
                { DecisionCombo.SelectedIndex = index; break; }
            }
            JustificationBox.Text = record.Justification;
            SaveReviewButton.IsEnabled = true;
            CompleteReviewButton.IsEnabled = !record.ReviewComplete;
            ReviewStatus.Text = record.ReviewComplete
                ? "Décision humaine retrouvée ; revue explicitement terminée. Cela ne vaut pas conformité."
                : "Décision humaine retrouvée ; la revue reste à terminer.";
        }
        catch (Exception ex) { ReviewStatus.Text = "Impossible de relire la décision de démonstration : " + ex.Message; }
    }

    private async void CompleteReview_Click(object sender, RoutedEventArgs e)
    {
        if (!CompleteReviewButton.IsEnabled) return;
        var saved = GetReviewRecords().LastOrDefault();
        if (saved is null) { ReviewStatus.Text = "La décision enregistrée est illisible ; la revue reste incomplète."; return; }
        var record = saved with { Id = Guid.NewGuid().ToString("N"), ReviewComplete = true, UpdatedAt = DateTimeOffset.UtcNow };
        await File.WriteAllTextAsync(Path.Combine(CurrentDossierPath, "decisions", $"{record.UpdatedAt:yyyyMMddHHmmssfffffff}-{record.Id}.json"), JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
        TouchAuditManifest();
        ReviewStatus.Text = "Revue explicitement terminée par l’auditrice. Cela ne signifie pas que la page est conforme.";
        CompleteReviewButton.IsEnabled = false;
    }

    private async void Capture_Click(object sender, RoutedEventArgs e)
    {
        if (Browser.CoreWebView2 is null) { ProofStatus.Text = "La fixture WebView2 locale n’est pas prête."; return; }
        try
        {
            Directory.CreateDirectory(CurrentDossierPath);
            var id = Guid.NewGuid().ToString("N");
            var pngName = $"synthetique-{id}.png";
            var png = Path.Combine(CurrentDossierPath, pngName);
            using (var file = File.Create(png)) await Browser.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, file);
            var record = new SyntheticProofRecord(id, "Espace d’audit — démonstrateur Windows", currentAuditId, "fixture-locale-synthetique", Browser.Source?.AbsoluteUri ?? "", CurrentPageHeader.Text, selectedTag, selectedText, DateTimeOffset.UtcNow, "CapturePreviewAsync WebView2", pngName, "Aucun identifiant, cookie ou jeton collecté.", "Illustration de la fixture locale uniquement ; aucun site réel ni verdict de conformité.");
            var json = Path.Combine(CurrentDossierPath, $"synthetique-{id}.json");
            await File.WriteAllTextAsync(json, JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
            latestProof = json;
            ProofPreview.Source = new BitmapImage(new Uri(png));
            ProofStatus.Text = $"Capture réelle de la fixture synthétique enregistrée :\n{json}";
            TouchAuditManifest();
        }
        catch (Exception ex) { ProofStatus.Text = "Échec de la capture : " + ex.Message; }
    }

    private void OpenProof_Click(object sender, RoutedEventArgs e)
    {
        latestProof ??= GetLatestProofPath();
        var proof = latestProof;
        if (proof is null || !File.Exists(proof)) { ProofStatus.Text = "Aucune preuve dans cette session. Capturez d’abord la fixture."; return; }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(proof) { UseShellExecute = true });
    }

    private void LoadLatestProof()
    {
        latestProof = null;
        ProofPreview.Source = null;
        ProofStatus.Text = "Aucune preuve n’a encore été enregistrée pour cet audit.";
        latestProof = GetLatestProofPath();
        if (latestProof is null) return;
        var image = Path.ChangeExtension(latestProof, ".png");
        if (File.Exists(image)) ProofPreview.Source = new BitmapImage(new Uri(image));
        ProofStatus.Text = "Une preuve synthétique précédente est disponible dans le dossier local.";
    }

    private string? GetLatestProofPath()
    {
        if (!Directory.Exists(CurrentDossierPath)) return null;
        return Directory.GetFiles(CurrentDossierPath, "synthetique-*.json").Where(path =>
        {
            try { return JsonSerializer.Deserialize<SyntheticProofRecord>(File.ReadAllText(path))?.AuditId == currentAuditId; }
            catch (Exception) { return false; }
        }).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
    }

    private List<SyntheticReviewRecord> GetReviewRecords()
    {
        var directory = Path.Combine(CurrentDossierPath, "decisions");
        if (!Directory.Exists(directory)) return [];
        var records = new List<SyntheticReviewRecord>();
        foreach (var path in Directory.GetFiles(directory, "*.json"))
        {
            try
            {
                var record = JsonSerializer.Deserialize<SyntheticReviewRecord>(File.ReadAllText(path));
                if (record is not null && record.AuditId == currentAuditId) records.Add(record);
            }
            catch (JsonException) { }
            catch (IOException) { }
        }
        return records.OrderBy(record => record.UpdatedAt).ToList();
    }

    private void RefreshAuditList()
    {
        Directory.CreateDirectory(auditRoot);
        var audits = Directory.GetDirectories(auditRoot).Select(path =>
        {
            try
            {
                var id = Path.GetFileName(path);
                if (!Guid.TryParseExact(id, "N", out _)) return null;
                var manifest = JsonSerializer.Deserialize<SyntheticAuditManifest>(File.ReadAllText(Path.Combine(path, "manifest.json")));
                return manifest?.Id == id ? manifest : null;
            }
            catch (Exception) { return null; }
        }).Where(audit => audit is not null).Cast<SyntheticAuditManifest>().OrderByDescending(audit => audit.UpdatedAt)
          .Select(audit => new AuditListItem(audit.Id, audit.Title, $"{audit.SiteUrl}  ·  {audit.Version}{(string.IsNullOrEmpty(audit.Level) ? "" : " · " + audit.Level)}", audit.UpdatedAt.ToLocalTime().ToString("g"), IconFor(audit.Id))).ToList();
        AuditList.ItemsSource = audits;
        NoAuditsMessage.Visibility = audits.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        _ = Dispatcher.BeginInvoke(new Action(() =>
        {
            AuditList.UpdateLayout();
            foreach (var item in audits)
                if (AuditList.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem container)
                    AutomationProperties.SetName(container, $"{item.Title}, {item.Summary}, mis à jour {item.Updated}. Reprendre");
        }), DispatcherPriority.ContextIdle);
    }

    private void AuditList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Selection remains a ListBox selection for keyboard navigation and UIA announcements.
    }

    private async void AuditList_MouseActivate(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.OriginalSource is not DependencyObject source) return;
        var item = FindAncestor<ListBoxItem>(source);
        if (item?.DataContext is not AuditListItem selected) return;
        e.Handled = true;
        await ResumeAuditAsync(selected.Id);
    }

    private async void AuditList_KeyActivate(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Space) || AuditList.SelectedItem is not AuditListItem selected) return;
        e.Handled = true;
        await ResumeAuditAsync(selected.Id);
    }

    private static T? FindAncestor<T>(DependencyObject child) where T : DependencyObject
    {
        for (var current = child; current is not null; current = VisualTreeHelper.GetParent(current))
            if (current is T match) return match;
        return null;
    }

    private void UpdateProjectListState()
    {
        if (ProjectListButton is null) return;
        var active = HomeView.Visibility == Visibility.Visible;
        ProjectListButton.Tag = active;
        ProjectAccessMarker.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetName(ProjectListButton, active ? "Projets — vue active" : "Ouvrir la liste des projets");
        AutomationProperties.SetItemStatus(ProjectListButton, active ? "Vue active" : "");
    }

    private void TouchAuditManifest()
    {
        var path = Path.Combine(CurrentDossierPath, "manifest.json");
        try
        {
            var manifest = JsonSerializer.Deserialize<SyntheticAuditManifest>(File.ReadAllText(path));
            if (manifest is null || manifest.Id != currentAuditId) return;
            File.WriteAllText(path, JsonSerializer.Serialize(manifest with { UpdatedAt = DateTimeOffset.UtcNow }, new JsonSerializerOptions { WriteIndented = true }));
            RefreshAuditList();
        }
        catch (Exception) { }
    }

    private void Criteria_Click(object sender, RoutedEventArgs e) => RuleCombo.Focus();
    private void Evidence_Click(object sender, RoutedEventArgs e) => CaptureButton.Focus();
    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this, "Démonstrateur natif Windows WPF/WebView2. Fixture française locale et fictive uniquement. Cette expérimentation ne fixe pas le choix de stack, n’est pas prête pour le Microsoft Store et ne modifie pas le périmètre Linux v1.", "À propos", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void NewAudit_Click(object sender, RoutedEventArgs e)
    {
        currentAuditId = Guid.NewGuid().ToString("N");
        SetupStatus.Text = ""; SiteUrlBox.Text = "https://exemple.test"; FaviconConsent.IsChecked = true; ReferenceCombo.SelectedIndex = 0; VersionCombo.SelectedIndex = 0; LevelCombo.SelectedIndex = 0; LevelPanel.Visibility = Visibility.Collapsed;
        HomeView.Visibility = Visibility.Collapsed; WorkspaceView.Visibility = Visibility.Collapsed; SetupView.Visibility = Visibility.Visible; UpdateProjectListState();
        SiteUrlBox.Focus();
    }
    private void Reference_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (LevelPanel is null) return;
        LevelPanel.Visibility = ReferenceCombo.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        if (ReferenceCombo.SelectedIndex != 2 && LevelCombo is not null) LevelCombo.SelectedIndex = 0;
        if (VersionCombo is not null) VersionCombo.SelectedIndex = 0;
    }
    private async void CreateAudit_Click(object sender, RoutedEventArgs e)
    {
        var reference = (ReferenceCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        var version = (VersionCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        var level = (LevelCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        if (!Uri.TryCreate(SiteUrlBox.Text, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps || !url.IsDefaultPort || !string.IsNullOrEmpty(url.UserInfo) || string.IsNullOrWhiteSpace(url.Host))
        { SetupStatus.Text = "Saisissez une URL HTTPS valide, sans identifiants ni port personnalisé."; SiteUrlBox.Focus(); return; }
        if (reference is not ("RGAA" or "WCAG")) { SetupStatus.Text = "Choisissez un référentiel."; ReferenceCombo.Focus(); return; }
        if ((reference == "RGAA" && version != "RGAA 4.1.2") || (reference == "WCAG" && version != "WCAG 2.2")) { SetupStatus.Text = "Choisissez la version du référentiel."; VersionCombo.Focus(); return; }
        if (reference == "WCAG" && level is not ("A" or "AA" or "AAA")) { SetupStatus.Text = "Choisissez explicitement une cible A, AA ou AAA."; LevelCombo.Focus(); return; }
        var requestFavicon = FaviconConsent.IsChecked == true;
        var manifest = new SyntheticAuditManifest(currentAuditId, SiteUrlBox.Text.Trim(), reference, version, reference == "WCAG" ? level : "", "Portail de services — démonstration", DateTimeOffset.UtcNow);
        Directory.CreateDirectory(CurrentDossierPath);
        File.WriteAllText(Path.Combine(CurrentDossierPath, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        CurrentAuditHeader.Text = $"{manifest.Title} · {version}{(reference == "WCAG" ? $" · {level}" : "")}";
        AddOrActivateTab(manifest.Id, TabTitle(manifest), IconFor(manifest.Id));
        CurrentPageHeader.Text = "Recherche — démonstration";
        SetupView.Visibility = Visibility.Collapsed; HomeView.Visibility = Visibility.Collapsed; WorkspaceView.Visibility = Visibility.Visible; UpdateProjectListState();
        RestoreReviewRecord();
        LoadLatestProof();
        RefreshAuditList();
        _ = FetchAndApplyFaviconAsync(manifest.Id, manifest.SiteUrl, requestFavicon);
        await EnsureBrowserAsync();
        Browser.Focus();
    }
    private void Home_Click(object sender, RoutedEventArgs e)
    {
        WorkspaceView.Visibility = Visibility.Collapsed; SetupView.Visibility = Visibility.Collapsed; HomeView.Visibility = Visibility.Visible; RefreshAuditList();
        UpdateProjectListState();
        AuditList.Focus();
    }
    private void Options_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsButton.ContextMenu is not ContextMenu menu) return;
        if (menu.IsOpen)
        {
            DismissOptionsMenu(restoreFocus: true);
            return;
        }
        menu.PlacementTarget = OptionsButton;
        menu.Placement = PlacementMode.Bottom;
        optionsMenuCommandInvoked = false;
        focusMenuTriggerAfterCommand = false;
        OptionsButton.Tag = true;
        menu.IsOpen = true;
        AutomationProperties.SetItemStatus(OptionsButton, "Ouvert");
    }
    private void OptionsMenu_Opened(object? sender, RoutedEventArgs e)
    {
        optionsMenuCommandInvoked = false;
        focusMenuTriggerAfterCommand = false;
        keyboardFocusedMenuItem = null;
        OptionsButton.Tag = true;
        AutomationProperties.SetItemStatus(OptionsButton, "Ouvert");
        _ = Dispatcher.BeginInvoke(new Action(() =>
        {
            OptionsMenu.UpdateLayout();
            var firstEnabled = OptionsMenu.Items.OfType<MenuItem>().FirstOrDefault(item => item.IsEnabled);
            if (firstEnabled is not null)
            {
                keyboardFocusedMenuItem = firstEnabled;
                OptionsMenu.Focus();
                firstEnabled.Focus();
                Keyboard.Focus(firstEnabled);
            }
        }), DispatcherPriority.ContextIdle);
    }
    private void OptionsMenu_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        HandleOptionsMenuKey(e);
    }
    private void OptionsMenu_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!OptionsMenu.IsOpen || e.LeftButton == MouseButtonState.Pressed || keyboardFocusedMenuItem is null) return;
        var preserveFocus = keyboardFocusedMenuItem;
        _ = Dispatcher.BeginInvoke(new Action(() =>
        {
            if (OptionsMenu.IsOpen && e.LeftButton != MouseButtonState.Pressed && preserveFocus.IsEnabled && ReferenceEquals(preserveFocus, keyboardFocusedMenuItem))
                Keyboard.Focus(preserveFocus);
        }), DispatcherPriority.ContextIdle);
    }
    private bool HandleOptionsMenuKey(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DismissOptionsMenu(restoreFocus: true);
            e.Handled = true;
            return true;
        }
        if (e.Key == Key.F6)
        {
            e.Handled = true;
            if (!inspectMode && InspectMenuItem.IsEnabled)
            {
                MarkMenuAction(focusTrigger: true);
                DismissOptionsMenu(restoreFocus: false);
                _ = SetInspectModeAsync(true, returnFocusToTrigger: true);
            }
            return true;
        }
        var enabledItems = OptionsMenu.Items.OfType<MenuItem>().Where(item => item.IsEnabled).ToList();
        if (e.Key is Key.Up or Key.Down)
        {
            if (enabledItems.Count == 0) { e.Handled = true; return true; }
            var current = Keyboard.FocusedElement as MenuItem;
            var index = enabledItems.IndexOf(current!);
            if (index < 0) index = e.Key == Key.Down ? -1 : enabledItems.Count;
            var nextIndex = Math.Clamp(index + (e.Key == Key.Down ? 1 : -1), 0, enabledItems.Count - 1);
            keyboardFocusedMenuItem = enabledItems[nextIndex];
            Keyboard.Focus(enabledItems[nextIndex]);
            e.Handled = true;
            return true;
        }
        if (e.Key == Key.Enter)
        {
            var focusedItem = Keyboard.FocusedElement as MenuItem;
            if (focusedItem is null || !focusedItem.IsEnabled)
            {
                e.Handled = true;
                return true;
            }
            keyboardFocusedMenuItem = focusedItem;
            focusedItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, focusedItem));
            if (IsLoaded && OptionsMenu.IsOpen) DismissOptionsMenu(restoreFocus: false);
            e.Handled = true;
            return true;
        }
        if (e.Key == Key.Space)
        {
            // Menu commands activate with Enter; Space must not trigger an unrelated command.
            e.Handled = true;
            return true;
        }
        return false;
    }
    private void OptionsMenu_ItemInvoked(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not MenuItem item) return;
        optionsMenuCommandInvoked = true;
        focusMenuTriggerAfterCommand = item.Name is "InspectMenuItem" or "ExitInspectMenuItem";
    }
    private void MarkMenuAction(bool focusTrigger)
    {
        if (!OptionsMenu.IsOpen) return;
        optionsMenuCommandInvoked = true;
        focusMenuTriggerAfterCommand = focusTrigger;
    }
    private void DismissOptionsMenu(bool restoreFocus)
    {
        if (OptionsMenu.IsOpen) OptionsMenu.IsOpen = false;
        OptionsButton.Tag = false;
        AutomationProperties.SetItemStatus(OptionsButton, "Fermé");
        if (restoreFocus && IsActive)
            _ = Dispatcher.BeginInvoke(new Action(() => OptionsButton.Focus()), DispatcherPriority.ContextIdle);
    }
    private void OptionsMenu_Closed(object sender, RoutedEventArgs e)
    {
        OptionsButton.Tag = false;
        AutomationProperties.SetItemStatus(OptionsButton, "Fermé");
        if (IsActive && (!optionsMenuCommandInvoked || focusMenuTriggerAfterCommand))
            _ = Dispatcher.BeginInvoke(new Action(() => OptionsButton.Focus()), DispatcherPriority.ContextIdle);
        optionsMenuCommandInvoked = false;
        focusMenuTriggerAfterCommand = false;
        keyboardFocusedMenuItem = null;
    }
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this, "Les paramètres détaillés ne sont pas configurés dans ce prototype.", "Paramètres", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void UpdateInfo_Click(object sender, RoutedEventArgs e) => MessageBox.Show(this, "Aucun service de mise à jour n’est disponible ou configuré dans ce prototype.", "Mises à jour", MessageBoxButton.OK, MessageBoxImage.Information);
    private void SidePanelToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (AuditPanelColumn is null || SidePanelToggle is null) return;
        var shown = SidePanelToggle.IsChecked == true;
        SidePanelToggle.Tag = shown;
        SidePanelActiveMarker.Visibility = shown ? Visibility.Visible : Visibility.Collapsed;
        AuditPanelColumn.Width = shown ? new GridLength(350) : new GridLength(0);
        AutomationProperties.SetHelpText(SidePanelToggle, shown ? "Développé" : "Réduit");
        SidePanelToggle.ToolTip = shown ? "Masquer le panneau d’audit" : "Afficher le panneau d’audit";
        AutomationProperties.SetName(SidePanelToggle, shown ? "Masquer le panneau d’audit" : "Afficher le panneau d’audit");
    }
    private void AddOrActivateTab(string id, string title, string? iconPath = null)
    {
        var found = openTabs.FirstOrDefault(tab => tab.Id == id);
        foreach (var tab in openTabs) tab.IsActive = tab.Id == id;
        if (found is null) openTabs.Add(new OpenAuditTab(id, title, true, IconFor(id)));
        else { found.IsActive = true; found.IconPath = iconPath ?? IconFor(id); }
        RefreshTabAutomationNames();
    }
    private string FaviconPath(string id) => Path.Combine(auditRoot, id, "favicon.png");
    private string IconFor(string id)
    {
        try
        {
            var manifest = JsonSerializer.Deserialize<SyntheticAuditManifest>(File.ReadAllText(Path.Combine(auditRoot, id, "manifest.json")));
            return manifest?.Id == id && manifest.FaviconFile == "favicon.png" && File.Exists(FaviconPath(id))
                ? new Uri(FaviconPath(id)).AbsoluteUri : "pack://application:,,,/Assets/Icons/globe.svg";
        }
        catch (Exception) { return "pack://application:,,,/Assets/Icons/globe.svg"; }
    }
    private async Task FetchAndApplyFaviconAsync(string id, string siteUrl, bool consented)
    {
        try
        {
            var png = await FaviconWorkflow.FetchIfConsentedAsync(consented, siteUrl, url => FaviconFetcher.FetchPngAsync(url));
            if (png is null || !Guid.TryParseExact(id, "N", out _)) return;
            var path = await FaviconStore.SaveAsync(auditRoot, id, png);
            if (path is null) return;
            await Dispatcher.InvokeAsync(() =>
            {
                if (AuditList.ItemsSource is IEnumerable<AuditListItem> items)
                {
                    var selectedId = (AuditList.SelectedItem as AuditListItem)?.Id;
                    AuditList.ItemsSource = FaviconWorkflow.ApplyIconByAuditId(id, new Uri(path).AbsoluteUri, openTabs, items);
                    if (selectedId is not null) AuditList.SelectedItem = AuditList.Items.Cast<AuditListItem>().FirstOrDefault(item => item.Id == selectedId);
                }
                else FaviconWorkflow.ApplyIconByAuditId(id, new Uri(path).AbsoluteUri, openTabs, []);
            });
        }
        catch (Exception) { }
    }
    private void RefreshTabAutomationNames()
    {
        _ = Dispatcher.BeginInvoke(new Action(() =>
        {
            OpenTabs.UpdateLayout();
            foreach (var border in FindVisualChildren<Border>(OpenTabs))
            {
                if (border.DataContext is not OpenAuditTab tab) continue;
                border.Background = tab.IsActive && windowIsActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(0xE7, 0xE8, 0xEB));
                border.BorderBrush = tab.IsActive && windowIsActive ? new SolidColorBrush(Color.FromRgb(0x21, 0x4B, 0x9A)) : new SolidColorBrush(Color.FromRgb(0xC4, 0xC7, 0xCC));
                border.BorderThickness = tab.IsActive && windowIsActive ? new Thickness(1, 1, 1, 3) : new Thickness(1);
            }
            foreach (var button in FindVisualChildren<Button>(OpenTabs))
            {
                if (button.Tag is not string id || AutomationProperties.GetName(button) != "Fermer l’onglet") continue;
                var tab = openTabs.FirstOrDefault(candidate => candidate.Id == id);
                if (tab is not null)
                {
                    button.Style = (Style)FindResource("DesignSystemButtonStyle");
                    button.Background = Brushes.White;
                    button.BorderBrush = (Brush)FindResource("ChromeHoverBorder");
                    button.BorderThickness = new Thickness(1);
                    AutomationProperties.SetName(button, $"Fermer le dossier {tab.Title}");
                    button.ToolTip = "Fermer l’onglet";
                    button.BorderThickness = new Thickness(1);
                }
            }
        }), DispatcherPriority.ContextIdle);
    }
    private static string TabTitle(SyntheticAuditManifest manifest) => $"{new Uri(manifest.SiteUrl).Host} · {manifest.Version}{(string.IsNullOrEmpty(manifest.Level) ? "" : $" · {manifest.Level}")}";
    private async void TabActivate_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string id) return;
        if (id == currentAuditId) return;
        var index = AuditList.Items.Cast<AuditListItem>().ToList().FindIndex(item => item.Id == id);
        if (index >= 0) { AuditList.SelectedIndex = index; await ResumeAuditAsync(id); }
    }
    private void TabClose_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string id) CloseTab(id);
    }
    private void Tab_DragStart(object sender, MouseButtonEventArgs e)
    {
        draggedTabId = (sender as FrameworkElement)?.Tag as string;
    }
    private void Tab_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) { draggedTabId = null; return; }
        if (draggedTabId is null || (sender as FrameworkElement)?.DataContext is not OpenAuditTab target) return;
        var sourceIndex = openTabs.ToList().FindIndex(tab => tab.Id == draggedTabId);
        var targetIndex = openTabs.ToList().FindIndex(tab => tab.Id == target.Id);
        if (sourceIndex >= 0 && targetIndex >= 0 && sourceIndex != targetIndex) openTabs.Move(sourceIndex, targetIndex);
    }
    private void CloseCurrentTab() => CloseTab(currentAuditId);
    private void CloseTab(string id)
    {
        var index = openTabs.ToList().FindIndex(tab => tab.Id == id);
        if (index < 0) return;
        openTabs.RemoveAt(index);
        if (id != currentAuditId)
        {
            var activeId = currentAuditId;
            _ = Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!FocusOpenTab(activeId)) OptionsButton.Focus();
            }), DispatcherPriority.ContextIdle);
            return;
        }
        currentAuditId = "";
        HomeView.Visibility = Visibility.Visible; SetupView.Visibility = Visibility.Collapsed; WorkspaceView.Visibility = Visibility.Collapsed; UpdateProjectListState();
        if (openTabs.Count > 0) _ = ResumeAuditAsync(openTabs[Math.Min(index, openTabs.Count - 1)].Id, focusBrowser: false);
        else ProjectListButton.Focus();
    }
    private bool FocusOpenTab(string id)
    {
        OpenTabs.UpdateLayout();
        var tab = openTabs.FirstOrDefault(candidate => candidate.Id == id);
        if (tab is null) return false;
        var container = OpenTabs.ItemContainerGenerator.ContainerFromItem(tab);
        if (container is null) return false;
        var button = FindVisualChildren<Button>(container).FirstOrDefault(candidate => (candidate.Tag as string) == id && AutomationProperties.GetName(candidate) == tab.Title);
        return button?.Focus() == true;
    }
    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) yield return match;
            foreach (var descendant in FindVisualChildren<T>(child)) yield return descendant;
        }
    }
    private async Task ResumeAuditAsync(string id, bool focusBrowser = true)
    {
        currentAuditId = id;
        SyntheticAuditManifest? manifest;
        try { manifest = JsonSerializer.Deserialize<SyntheticAuditManifest>(File.ReadAllText(Path.Combine(CurrentDossierPath, "manifest.json"))); }
        catch (Exception) { return; }
        if (manifest is null || manifest.Id != id) return;
        AddOrActivateTab(manifest.Id, TabTitle(manifest));
        CurrentAuditHeader.Text = $"{manifest.Title} · {manifest.Version}{(string.IsNullOrEmpty(manifest.Level) ? "" : $" · {manifest.Level}")}";
        CurrentPageHeader.Text = "Recherche — démonstration";
        HomeView.Visibility = Visibility.Collapsed; SetupView.Visibility = Visibility.Collapsed; WorkspaceView.Visibility = Visibility.Visible; UpdateProjectListState();
        RestoreReviewRecord(); LoadLatestProof(); await EnsureBrowserAsync();
        if (focusBrowser) Browser.Focus();
        else if (!FocusOpenTab(id)) ProjectListButton.Focus();
    }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public sealed record SyntheticProofRecord(string Id, string Application, string AuditId, string Fixture, string Url, string Page, string SelectedElementTag, string SelectedElementText, DateTimeOffset CapturedAt, string CaptureMethod, string ScreenshotFile, string DataHandling, string Context);
public sealed record SyntheticReviewRecord(string Id, string AuditId, string Page, string RuleSuggestion, string HumanDecision, string Justification, bool ReviewComplete, DateTimeOffset UpdatedAt);
public sealed record SyntheticAuditManifest(string Id, string SiteUrl, string Reference, string Version, string Level, string Title, DateTimeOffset UpdatedAt, string? FaviconFile = null);
public sealed class AuditListItem
{
    public string Id { get; }
    public string Title { get; }
    public string Summary { get; }
    public string Updated { get; }
    public string IconPath { get; }
    private readonly System.Windows.Media.ImageSource? faviconImage;
    public string? FaviconUri => IconPath.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ? IconPath : null;
    public System.Windows.Media.ImageSource? FaviconImage => faviconImage;
    public bool HasFavicon => faviconImage is not null;
    public AuditListItem(string id, string title, string summary, string updated, string iconPath)
    {
        Id = id; Title = title; Summary = summary; Updated = updated; IconPath = iconPath;
        faviconImage = FaviconImageLoader.Load(FaviconUri);
    }
}
public sealed class OpenAuditTab : INotifyPropertyChanged
{
    private bool isActive;
    public string Id { get; }
    public string Title { get; }
    private string iconPath;
    private System.Windows.Media.ImageSource? faviconImage;
    public string IconPath
    {
        get => iconPath;
        set
        {
            if (iconPath == value) return;
            iconPath = value;
            faviconImage = FaviconImageLoader.Load(FaviconUri);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconPath)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FaviconUri)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FaviconImage)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasFavicon)));
        }
    }
    public string? FaviconUri => IconPath.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ? IconPath : null;
    public System.Windows.Media.ImageSource? FaviconImage => faviconImage;
    public bool HasFavicon => faviconImage is not null;
    public string CloseName => $"Fermer le dossier {Title}";
    public bool IsActive
    {
        get => isActive;
        set
        {
            if (isActive == value) return;
            isActive = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        }
    }
    public string Status => IsActive ? "Dossier actif" : "Dossier inactif";
    public event PropertyChangedEventHandler? PropertyChanged;
    public OpenAuditTab(string id, string title, bool isActive, string iconPath) { Id = id; Title = title; IsActive = isActive; this.iconPath = iconPath; faviconImage = FaviconImageLoader.Load(FaviconUri); }
}
