using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Helpers;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Models;
using GM4ManagerWPF.Properties;
using GM4ManagerWPF.Views;
using Ookii.Dialogs.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;
//using System.Windows.Forms;
using System.Windows.Input;

namespace GM4ManagerWPF.ViewModels
{
    public class ExplorerUCViewModel : INotifyPropertyChanged
    {        
        public string SelectedPath
        {
            get => _selectedPath;
            set { _selectedPath = value; OnPropertyChanged(); }
        }


        public ObservableCollection<PermissionInfo> CurrentPermissions
        {
            get => _currentPermissions;
            set
            {
                _currentPermissions = value;
                OnPropertyChanged();
            }
        }
        public ICommand AddMemberCommand { get; }
        public string NewFolderName
        {
            get => _newFolderName;
            set
            {
                _newFolderName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanCreateFolder));
            }
        }

        private bool _disableInheritance;
        public bool DisableInheritance
        {
            get => _disableInheritance;
            set => SetProperty(ref _disableInheritance, value);
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

        public string? SelectedFolderPath { get; set; }
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

        public event PropertyChangedEventHandler? PropertyChanged;

        public static ResourceService Res => ResourceService.Instance;
        public ObservableCollection<DirectoryNodeViewModel> RootItems { get; } = [];

        private string _selectedPath = string.Empty;
        private ObservableCollection<PermissionInfo> _currentPermissions = [];
        private string _newFolderName = string.Empty;
        public bool CanCreateFolder =>
            !string.IsNullOrWhiteSpace(NewFolderName) && SelectedPath != null;

        public List<string> CurrentUserGroups { get; private set; } = [];

        public ICommand CreateNewFolderCommand => new RelayCommand(CreateNewFolder, () => CanCreateFolder);

        public ICommand OpenFolderDialogCommand { get; }
        
        public RelayCommand RemoveUserCommand { get; }

        /// <summary>
        /// Initializes the ViewModel by loading the current user's group memberships.
        /// </summary>
        public async Task InitializeAsync(Action<string>? reportStatus = null)
        {
            reportStatus?.Invoke(Resources.loadingPermissions);
            CurrentUserGroups = await GroupHelper.GetUserGroupsForCurrentUserAsync(reportStatus);
        }
        /// <summary>
        /// Loads NTFS permissions for a given folder path and updates the UI.
        /// </summary>
        public async Task LoadPermissionsAsync(string folderPath, Action<string>? reportStatus = null)
        {
            SelectedFolderPath = folderPath;
            IsLoadingPermissions = true;
            string currentDomain = ActiveDirectoryService.GetNetbiosDomain();

            try
            {
                // make sure, that CurrentUserGroups is loaded
                if (CurrentUserGroups == null || CurrentUserGroups.Count == 0)
                {
                    reportStatus?.Invoke(Resources.loadingPermissions);
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
                        Debug.WriteLine($"Processing identity in LoadPermissionsAsync: {identity}");
                        if (identity.StartsWith(currentDomain + "\\", StringComparison.OrdinalIgnoreCase))
                        {
                            string DisplayName = identity.Split('\\').Last();
                            list.Add(new PermissionInfo
                            {
                                IdentityReference = DisplayName,
                                CanModify = rule.FileSystemRights.HasFlag(FileSystemRights.Modify),
                                CanReadExecute = rule.FileSystemRights.HasFlag(FileSystemRights.ReadAndExecute),
                                Rights = rule.FileSystemRights,
                            });
                        }
                    }
                    return list;
                });

                CurrentPermissions.Clear();
                foreach (var item in result)
                {
                    CurrentPermissions.Add(item);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to load permissions: " + ex.Message);
                string message = Resources.msgFailedToLoadPermissions.Replace("{message}", ex.Message);
                MessageBox.Show(message, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                CurrentPermissions = [];
            }
            finally
            {
                IsLoadingPermissions = false;
            }
        }

        public ExplorerUCViewModel()
        {            
            SelectedPath = AppSettingsManager.Settings.StartShare;
            UpdateLocalizedHeaders();

            OpenFolderDialogCommand = new RelayCommand(OpenFolderDialog);

            Task.Run(() => LoadRootDirectoriesAsync());

            ResourceService.Instance.PropertyChanged += (_, __) => UpdateLocalizedHeaders();

            Debug.WriteLine($"ExplorerUCViewModel {SelectedPath}");
            AddMemberCommand = new RelayCommand(AddSelectedMember, CanAddMember);
            RemoveUserCommand = new RelayCommand(_ => RemoveUser(), _ => SelectedPermission != null);
        }

        /// <summary>
        /// Opens a folder browser dialog and sets the selected path.
        /// Also triggers loading of root directories.
        /// </summary>
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
        /// <summary>
        /// Loads NTFS permissions for the currently selected directory node.
        /// Uses debouncing to avoid excessive calls.
        /// </summary>
        private async Task LoadPermissionsForSelectedNodeAsync()
        {
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
        }

        private void RemoveUser()
        {
            if (SelectedPermission == null || string.IsNullOrWhiteSpace(SelectedFolderPath))
            {
                return;
            }

            NtfsPermissionHelper.RemovePermission(SelectedFolderPath, SelectedPermission.IdentityReference);
            CurrentPermissions.Remove(SelectedPermission);
        }


        private PermissionInfo _selectedPermission;        

        public PermissionInfo SelectedPermission
        {
            get => _selectedPermission;
            set
            {
                _selectedPermission = value;
                OnPropertyChanged();

                RemoveUserCommand?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Loads the root directory node and adds it to the UI tree.
        /// </summary>
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
                RootItems.Clear(); // <<< important, so only one root node is present
                RootItems.Add(rootNode);
            });
        }        

        /// <summary>
        /// Updates the column headers based on the current localization.
        /// </summary>
        private void UpdateLocalizedHeaders()
        {
            HeaderMember = ResourceService.Instance["colMember"];
            HeaderModify = ResourceService.Instance["colModify"];
            HeaderReadAndExecute = ResourceService.Instance["colReadAndExecute"];
        }
        /// <summary>
        /// Creates a new folder in the selected path.
        /// Optionally disables inheritance of NTFS permissions.
        /// </summary>
        private void CreateNewFolder()
        {
            try
            {
                if (SelectedNode == null)
                {
                    MessageBox.Show(Resources.msgNoFolderSelected, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string fullPath = SelectedNode.FullPath;
                string parentPath = fullPath;
                string newPath = Path.Combine(parentPath, NewFolderName);
                Debug.WriteLine($"Creating new folder at: {newPath}");

                if (Directory.Exists(newPath))
                {
                    MessageBox.Show(Resources.msgFolderAlreadyExists, Resources.msgHeaderWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Directory.CreateDirectory(newPath);

                if (DisableInheritance)
                {
                    var dirInfo = new DirectoryInfo(newPath);
                    var security = dirInfo.GetAccessControl();

                    // Remove inheritance and copy rules
                    security.SetAccessRuleProtection(true, true); // disable inheritance, copy existing rules

                    dirInfo.SetAccessControl(security);
                }
                var message = Resources.msgFolderCreatedSuccessfully.Replace("{folder}", newPath);
                MessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                // Refresh the selected node to reflect the new folder
                Debug.WriteLine($"Refreshing node: {SelectedNode.FullPath}");


                var parentNode = SelectedNode;
                parentNode.Refresh();


                NewFolderName = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($" {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Opens a separate Window to search for and add a member to the selected group.
        /// </summary>
        /// <param name="parameter"></param>
        private void AddSelectedMember(object? parameter)
        {
            if (SelectedNode == null)
            {
                MessageBox.Show(Resources.msgSelectFolderFirst, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Open the Active Directory user search window
            var adSearchWindow = new AdUserSearchWindow();
            bool? result = adSearchWindow.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(adSearchWindow.SelectedUserDn))
            {
                string samAccountName = adSearchWindow.SamAccountName ?? string.Empty;

                if (CurrentPermissions.Count == 0)
                {
                    CurrentPermissions = [];
                }
                // Check if the user is already in the group
                bool alreadyInGroup = CurrentPermissions.Any(m => m?.IdentityReference.Equals(samAccountName, StringComparison.OrdinalIgnoreCase) == true);
                Debug.WriteLine($"Adding user: {samAccountName} to group: {SelectedNode.FullPath}, already in group: {alreadyInGroup}");
                if (!alreadyInGroup)
                {
                    try
                    {
                        //var rights = FileSystemRights.ChangePermissions | FileSystemRights.Modify | FileSystemRights.ReadAndExecute | FileSystemRights.FullControl;
                        var rights =  FileSystemRights.Modify | FileSystemRights.ReadAndExecute;

                        NtfsPermissionHelper.AddPermission(SelectedNode.FullPath, samAccountName, rights, AccessControlType.Allow);
                    }
                    catch (Exception ex)
                    {
                        string message = Resources.msgErrorAddingUser.Replace("{message}", ex.Message);
                        MessageBox.Show(message, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show(Resources.msgErrorUserAlreadyInGroup, Resources.msgHeaderSuccess, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private bool CanAddMember(object? parameter)
        {
            return SelectedNode != null;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
              PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value!;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }
    }
} // End of namespace
