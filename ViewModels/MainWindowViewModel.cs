using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Properties;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GM4ManagerWPF.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public static ResourceService Res => ResourceService.Instance;
        public event PropertyChangedEventHandler? PropertyChanged;

        private ManagerUCViewModel _manager = null!;
        public ManagerUCViewModel Manager
        {
            get => _manager;
            private set
            {
                _manager = value;
                OnPropertyChanged();
            }
        }

        private ExplorerUCViewModel? _explorer;
        public ExplorerUCViewModel? Explorer
        {
            get => _explorer;
            private set
            {
                _explorer = value;
                OnPropertyChanged();
            }
        }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged();
                    _ = OnTabChangedAsync(value); // async tab handling
                }
            }
        }

        private string? _managerName = string.Empty;
        public string ManagerName
        {
            get => _managerName ?? string.Empty;
            set
            {
                _managerName = value;
                OnPropertyChanged();
            }
        }

        public MainWindowViewModel()
        {
            var sw = Stopwatch.StartNew();            
            Debug.WriteLine("MainWindowViewModel constructor called");
            Manager = new ManagerUCViewModel();
            _managerName = "👤 " + Resources.headerManager.Replace("{manager}", ActiveDirectoryService.GetCNFromUsername());
            SelectedTabIndex = 0; // Manager tab by default
            
            sw.Stop();
            Debug.WriteLine($"Main Window View Model took: {sw.ElapsedMilliseconds} ms");
        }

        public async Task InitializeAsync(Action<string>? reportStatus = null)
        {           
            // Only preload Explorer if Explorer tab is selected on startup (not required)
            if (SelectedTabIndex == 0)
            {
                await LoadExplorerAsync(reportStatus);
            }
        }

        private async Task OnTabChangedAsync(int index)
        {
            if (index == 0 && Explorer == null)
            {
                await LoadExplorerAsync();
            }
        }

        private async Task LoadExplorerAsync(Action<string>? reportStatus = null)
        {
            Explorer = new ExplorerUCViewModel();
            await Explorer.InitializeAsync(reportStatus);
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
