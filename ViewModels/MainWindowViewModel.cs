
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Helpers;
using GM4ManagerWPF.Interfaces;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Properties;
using GM4ManagerWPF.ViewModels;
using GM4ManagerWPF.Views;
using System.Diagnostics;
using System.Windows;

public partial class MainWindowViewModel : ObservableRecipient
{
    private readonly UpdateHelper _updateHelper = new();
    public static ResourceService Res => ResourceService.Instance;

    [ObservableProperty]
    private string managerName = string.Empty;

    [ObservableProperty]
    private string currentVersion = "1.1.2"; 

    [ObservableProperty]
    private int selectedTabIndex;

    [ObservableProperty]
    private ManagerUCViewModel managerUC;

    [ObservableProperty]
    private ExplorerUCViewModel? explorerUC;

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private bool isUpdateAvailable = false;

    [ObservableProperty]
    private string? latestVersionText;


    private readonly ICursorService _cursorService;

    partial void OnSelectedTabIndexChanged(int value)
    {
        _ = OnTabChangedAsync(value);
    }

    public MainWindowViewModel(ICursorService cursorService)
    {
        title = Resources.headerTitle.Replace("{version}", currentVersion);
        var sw = Stopwatch.StartNew();
        IsActive = true;
        _cursorService = cursorService;
        _ = CheckForUpdatesAsync();

        managerUC = new ManagerUCViewModel(cursorService);        

        managerName = "👤 " + Resources.headerManager.Replace("{manager}", ActiveDirectoryService.GetCNFromUsername());
        sw.Stop();
        Debug.WriteLine($"Main Window View Model took: {sw.ElapsedMilliseconds} ms");
    }

    [RelayCommand]
    private void OpenHelp()
    {
        var helpUrl = "https://github.com/MaikL/GM4Manager/blob/main/README.md";
        Process.Start(new ProcessStartInfo
        {
            FileName = helpUrl,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void OpenUpdatePage()
    {
        if (!_updateHelper.IsCurrent && !string.IsNullOrEmpty(_updateHelper.LatestVersionUrl))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _updateHelper.LatestVersionUrl,
                UseShellExecute = true
            });
        }
    }
    
    private async Task OnTabChangedAsync(int index)
    {
        if (index == 1 && ExplorerUC == null)
        {
            await LoadExplorerAsync();
        }
    }

    public async Task CheckForUpdatesAsync()
    {
        await _updateHelper.CheckForUpdateAsync(CurrentVersion); 
        IsUpdateAvailable = !_updateHelper.IsCurrent;
        LatestVersionText = Resources.menuVersion.Replace("{LatestVersion}", _updateHelper.LatestVersion);
    }

    public async Task LoadExplorerAsync(Action<string>? reportStatus = null)
    {
        ExplorerUC = new ExplorerUCViewModel(_cursorService);
        await ExplorerUC.InitializeAsync(reportStatus);
    }
}
