using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Helpers;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Models;
using GM4ManagerWPF.Properties;
using Ookii.Dialogs.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Input;

namespace GM4ManagerWPF.ViewModels
{
    public class ExplorerUCViewModel : INotifyPropertyChanged
    {
        public static ResourceService Res => ResourceService.Instance;
        public ObservableCollection<DirectoryNodeViewModel> RootItems { get; } = [];

        private string _selectedPath = string.Empty;
        public string SelectedPath
        {
            get => _selectedPath;
            set { _selectedPath = value; OnPropertyChanged(); }
        }

        private ObservableCollection<PermissionInfo> _currentPermissions = [];
        public ObservableCollection<PermissionInfo> CurrentPermissions
        {
            get => _currentPermissions;
            set
            {
                _currentPermissions = value;
                OnPropertyChanged();
            }
        }

        public List<string> CurrentUserGroups { get; private set; } = [];
        public async Task InitializeAsync(Action<string>? reportStatus = null)
        {
            reportStatus?.Invoke("Lade Benutzergruppen...");
            CurrentUserGroups = await GroupHelper.GetUserGroupsForCurrentUserAsync(reportStatus);
        }

        public ICommand OpenFolderDialogCommand { get; }
        public ICommand EditPermissionCommand { get; }
        public ICommand RemovePermissionCommand { get; }

        private void OpenFolderDialog(object? obj)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = Resources.txtPlaseSelectNetworkDirectory,
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false,
                SelectedPath = SelectedPath
            };
            Debug.WriteLine($"Selected path: {dialog.SelectedPath}");
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                SelectedPath = dialog.SelectedPath;
                AppSettingsManager.Settings.StartShare = SelectedPath;
                Debug.WriteLine($"Selected path: {SelectedPath}");
                AppSettingsManager.Save(); // Save the selected path to settings
                // Clearing RootItems for new loading
                RootItems.Clear();

                Task.Run(() => LoadRootDirectoriesAsync());

            }
        }
        private DirectoryNodeViewModel? _selectedNode;
        public DirectoryNodeViewModel? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    _selectedNode = value;
                    OnPropertyChanged();
                    _ = LoadPermissionsForSelectedNodeAsync(); // fire & forget
                }
            }
        }
        private CancellationTokenSource? _cts;
        

        private async Task LoadPermissionsForSelectedNodeAsync()
        {
            var sw2 = Stopwatch.StartNew();

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                await Task.Delay(150, token); // debounce
                if (SelectedNode != null && Directory.Exists(SelectedNode.FullPath))
                {
                    await LoadPermissionsAsync(SelectedNode.FullPath);
                }
            }
            catch (TaskCanceledException)
            {
                // Ignored
            }

            sw2.Stop();
            Debug.WriteLine($"LoadPermissionsForSelectedNodeAsync took: {sw2.ElapsedMilliseconds} ms");
        }
        public string? SelectedFolderPath { get; set; }
        public async Task LoadPermissionsAsync(string folderPath, Action<string>? reportStatus = null)
        {
            SelectedFolderPath = folderPath;
            IsLoadingPermissions = true;

            try
            {
                // Stelle sicher, dass CurrentUserGroups geladen ist
                if (CurrentUserGroups == null || !CurrentUserGroups.Any())
                {
                    reportStatus?.Invoke("Lade Benutzergruppen...");
                    CurrentUserGroups = await GroupHelper.GetUserGroupsForCurrentUserAsync(reportStatus);
                }

                var result = await Task.Run(() =>
                {
                    var dirInfo = new DirectoryInfo(folderPath);
                    var acl = dirInfo.GetAccessControl();
                    var accessRules = acl.GetAccessRules(true, true, typeof(NTAccount));

                    var list = new List<PermissionInfo>();
                    foreach (FileSystemAccessRule rule in accessRules)
                    {
                        string identity = rule.IdentityReference.Value;

                        if (CurrentUserGroups.Any(g => identity.EndsWith(g, StringComparison.OrdinalIgnoreCase)))
                        {
                            list.Add(new PermissionInfo
                            {
                                IdentityReference = identity,
                                CanModify = rule.FileSystemRights.HasFlag(FileSystemRights.Modify),
                                CanReadExecute = rule.FileSystemRights.HasFlag(FileSystemRights.ReadAndExecute),
                                Rights = rule.FileSystemRights,
                            });
                        }
                    }
                    return list;
                });

                CurrentPermissions = new ObservableCollection<PermissionInfo>(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to load permissions: " + ex.Message);
                CurrentPermissions = new ObservableCollection<PermissionInfo>();
            }
            finally
            {
                IsLoadingPermissions = false;
            }
        }



        private string? _headerMember;
        public string HeaderMember
        {
            get => _headerMember ?? string.Empty;
            set
            {
                _headerMember = value;
                OnPropertyChanged();
            }
        }
        private string? _headerModify;

        public string HeaderModify
        {
            get => _headerModify ?? string.Empty;
            set
            {
                _headerModify = value;
                OnPropertyChanged();
            }
        }
        private string? _headerReadAndExecute;
        public string HeaderReadAndExecute
        {
            get => _headerReadAndExecute ?? string.Empty; // Ensure a non-null value is returned
            set
            {
                _headerReadAndExecute = value;
                OnPropertyChanged();
            }
        }

        public ExplorerUCViewModel()
        {
            var sw1 = Stopwatch.StartNew();
            var sw2 = Stopwatch.StartNew();

            SelectedPath = AppSettingsManager.Settings.StartShare;
            UpdateLocalizedHeaders();

            OpenFolderDialogCommand = new RelayCommand(OpenFolderDialog);

            Task.Run(() => LoadRootDirectoriesAsync());
            sw1.Stop();
            Debug.WriteLine($"MainWindowViewModel S1 Konstruktor dauerte: {sw1.ElapsedMilliseconds} ms");

            ResourceService.Instance.PropertyChanged += (_, __) => UpdateLocalizedHeaders();

            Debug.WriteLine($"ExplorerUCViewModel {SelectedPath}");
            EditPermissionCommand = new RelayCommand<PermissionInfo>(EditPermission, CanEditPermission);
            RemovePermissionCommand = new RelayCommand<PermissionInfo>(RemovePermission, CanRemovePermission);

            sw2.Stop();
            Debug.WriteLine($"MainWindowViewModel S2 Konstruktor dauerte: {sw2.ElapsedMilliseconds} ms");
        }
        private bool CanEditPermission(PermissionInfo? info) => info != null && info.Rights.HasFlag(FileSystemRights.Modify);
        private void EditPermission(PermissionInfo? info)
        {
            if (info == null)
            {
                return;
            }
            // TODO: Use NtfsPermissionHelper.RemovePermission(...)
            if (SelectedFolderPath == null)
            {
                return;
            }
            NtfsPermissionHelper.SetPermission(
                folderPath: SelectedFolderPath,
                identity: info.IdentityReference,
                rights: FileSystemRights.Modify, // oder ReadAndExecute etc.
                controlType: AccessControlType.Allow
            );
        }

        private bool CanRemovePermission(PermissionInfo? info) => info != null && info.Rights.HasFlag(FileSystemRights.Modify);
        private void RemovePermission(PermissionInfo? info)
        {
            if (info == null)
            {
                return;
            }


            CurrentPermissions.Remove(info);
        }

        private async Task LoadRootDirectoriesAsync()
        {
            string rootPath = SelectedPath;
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                return;
            }

            await Task.Delay(10);
            var rootNode = new DirectoryNodeViewModel(rootPath, this)
            {
                IsExpanded = true
            };

            App.Current.Dispatcher.Invoke(() =>
            {
                RootItems.Add(rootNode);
            });
        }
        private bool _isLoadingPermissions;
        public bool IsLoadingPermissions
        {
            get => _isLoadingPermissions;
            set
            {
                _isLoadingPermissions = value;
                OnPropertyChanged();
            }
        }

        private void UpdateLocalizedHeaders()
        {
            HeaderMember = ResourceService.Instance["colMember"];
            HeaderModify = ResourceService.Instance["colModify"];
            HeaderReadAndExecute = ResourceService.Instance["colReadAndExecute"];
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
              PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
