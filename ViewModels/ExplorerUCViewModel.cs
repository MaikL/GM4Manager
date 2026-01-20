using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Helpers;
using GM4ManagerWPF.Interfaces;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Models;
using GM4ManagerWPF.Properties;
using GM4ManagerWPF.Views;
using Ookii.Dialogs.Wpf;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;

namespace GM4ManagerWPF.ViewModels
{
    public partial class ExplorerUCViewModel : ObservableObject
    {
        public static ResourceService Res => ResourceService.Instance;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DisableInheritanceCommand))]
        [NotifyCanExecuteChangedFor(nameof(EnableInheritanceRecursivlyCommand))]
        [NotifyCanExecuteChangedFor(nameof(SetReadAccessCommand))]
        [NotifyCanExecuteChangedFor(nameof(SetModifyAccessCommand))]
        private bool isInheritedFromParent;


        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddSelectedMemberCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveSelectedMemberCommand))]
        [NotifyCanExecuteChangedFor(nameof(DisableInheritanceCommand))]
        [NotifyCanExecuteChangedFor(nameof(EnableInheritanceRecursivlyCommand))]
        [NotifyCanExecuteChangedFor(nameof(SetReadAccessCommand))]
        [NotifyCanExecuteChangedFor(nameof(SetModifyAccessCommand))]
        private bool canEditPermissions;

        [ObservableProperty]
        private string selectedPath;

        [ObservableProperty]
        private bool disableInheritanceForNewFolder;

        public ObservableCollection<PermissionInfo> CurrentPermissions { get; } = [];

        [ObservableProperty]
        public string? selectedFolderPath;

        [ObservableProperty]
        private string? headerMember;
        [ObservableProperty]
        private string? headerModify;
        [ObservableProperty]
        private string? headerReadAndExecute;
        [ObservableProperty]
        private bool isLoadingPermissions;        

        [ObservableProperty]
        private string? newFolderName;
        partial void OnNewFolderNameChanged(string? value)
        {
            OnPropertyChanged(nameof(CanCreateFolder));
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddSelectedMemberCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveSelectedMemberCommand))]
        [NotifyCanExecuteChangedFor(nameof(DisableInheritanceCommand))]
        [NotifyCanExecuteChangedFor(nameof(EnableInheritanceRecursivlyCommand))]
        [NotifyCanExecuteChangedFor(nameof(SetReadAccessCommand))]
        [NotifyCanExecuteChangedFor(nameof(SetModifyAccessCommand))]
        private DirectoryNodeViewModel? selectedNode;

        private readonly ICursorService _cursorService;
        public ExplorerUCViewModel(ICursorService cursorService)
        {            
            _cursorService = cursorService ?? throw new ArgumentNullException(nameof(cursorService));
            selectedPath = AppSettingsManager.Settings.StartShare;
            UpdateLocalizedHeaders();

            Task.Run(() => LoadRootDirectoriesAsync());

            ResourceService.Instance.PropertyChanged += (_, __) => UpdateLocalizedHeaders();

            Debug.WriteLine($"ExplorerUCViewModel {selectedPath}");           
        }

        private bool CanDisableInheritance() => IsInheritedFromParent && CanEditPermissions;
        private bool CanEnableInheritance() => !IsInheritedFromParent && CanEditPermissions;
        partial void OnSelectedNodeChanged(DirectoryNodeViewModel? value)
        {
            if (value == null || value.FullPath == SelectedFolderPath || !Directory.Exists(value.FullPath))
            {
                return;
            }

            Debug.WriteLine($"SelectedNode changed: {value.FullPath}");
            LoadPermissions(value.FullPath); 
            CanEditPermissions = UserCanEditPermissions(value.FullPath);
        }
        
        [RelayCommand(CanExecute = nameof(CanDisableInheritance))]
        private void DisableInheritance()
        {
            if (SelectedNode == null || string.IsNullOrWhiteSpace(SelectedNode.FullPath) || !CanEditPermissions)
            {
                return;
            }

            var progressDialog = new ProgressDialogViewModel(Resources.txtDisableInheritance);
            var progressWindow = new ProgressDialogWindow { DataContext = progressDialog };

            _cursorService.SetBusyCursor();

            try
            {
                ApplyExplicitPermissionsOnlyToRoot(SelectedNode.FullPath, progressDialog);
                IsInheritedFromParent = false;
                CanEditPermissions = !CanEditPermissions;
                CanEditPermissions = UserCanEditPermissions(SelectedNode.FullPath);

                LoadPermissions(SelectedNode.FullPath);                
            }
            catch (Exception ex)
            {
                MessageBox.Show($": {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _cursorService.ResetCursor();
            }
        }

        private void ApplyExplicitPermissionsOnlyToRoot(string path, ProgressDialogViewModel progress)
        {
            try
            {
                progress.CurrentPath = path;
                progress.ReportProgress(1, 1);

                var dirInfo = new DirectoryInfo(path);
                var security = dirInfo.GetAccessControl();

                // Disable inheritance and copy existing rules
                security.SetAccessRuleProtection(true, true);
                dirInfo.SetAccessControl(security);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting permissions on {path}: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanEnableInheritance))]
        private async Task EnableInheritanceRecursivly()
        {
            if (SelectedNode == null || string.IsNullOrWhiteSpace(SelectedNode.FullPath))
            {
                return;
            }

            var progressDialog = new ProgressDialogViewModel(Resources.btnEnableInheritance);
            var progressWindow = new ProgressDialogWindow { DataContext = progressDialog };
            progressWindow.Show();
            _cursorService.SetBusyCursor();

            try
            {
                await Task.Run(() =>
                {
                    EnableInheritanceRecurse(SelectedNode.FullPath, progressDialog);
                });

                IsInheritedFromParent = true;
                LoadPermissions(SelectedNode.FullPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error enabling inheritance: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _cursorService.ResetCursor();
                progressWindow.Close();
                CanEditPermissions = !CanEditPermissions;
                CanEditPermissions = UserCanEditPermissions(SelectedNode.FullPath);
            }
        }

        private void EnableInheritanceRecurse(string path, ProgressDialogViewModel progress)
        {
            var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

            var allPaths = dirs.Concat(files).Prepend(path).ToList();
            int total = allPaths.Count;
            int current = 0;

            /**
                // TODO: perhaps offer to remove explicit rules => time consuming
                var rules = security.GetAccessRules(true, false, typeof(NTAccount));
                foreach (FileSystemAccessRule rule in rules)
                {
                    security.RemoveAccessRule(rule);
                }
             */

            foreach (var item in allPaths)
            {
                try
                {
                    FileSystemSecurity security;

                    if (Directory.Exists(item))
                    {
                        var dirInfo = new DirectoryInfo(item);
                        security = dirInfo.GetAccessControl();
                        security.SetAccessRuleProtection(false, false); // enable inheritance, remove explicit
                        dirInfo.SetAccessControl((DirectorySecurity)security);
                    }
                    else if (File.Exists(item))
                    {
                        var fileInfo = new FileInfo(item);
                        security = fileInfo.GetAccessControl();
                        security.SetAccessRuleProtection(false, false); // enable inheritance, remove explicit
                        fileInfo.SetAccessControl((FileSecurity)security);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error enabling inheritance on {item}: {ex.Message}");
                }

                current++;
                if (current % 5 == 0)
                {
                    progress.CurrentPath = item;
                }

                progress.CurrentPath = item;
                progress.ReportProgress(current, total);
            }
            CanEditPermissions = UserCanEditPermissions(path);
        }

        public ObservableCollection<DirectoryNodeViewModel> RootItems { get; } = [];
                       
        public bool CanCreateFolder =>
            !string.IsNullOrWhiteSpace(NewFolderName) && SelectedPath != null;

        public List<string> CurrentUserGroups { get; private set; } = [];

        public bool CanShowMembers => SelectedPermission != null;

        [RelayCommand]
        public void ShowGroupMembers()
        {
            try
            {
                _cursorService.SetBusyCursor();

                if (SelectedPermission == null || string.IsNullOrWhiteSpace(SelectedFolderPath))
                {
                    MessageBox.Show(Resources.msgSelectPermissionFirst, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Debug.WriteLine($"ShowGroupMembers called for: {SelectedPermission.IdentityReference}");
                var showGroupMembersView = new ShowGroupMembers
                {
                    DataContext = new ShowGroupMembersViewModel(SelectedPermission)
                };

                bool? result = showGroupMembersView.ShowDialog();
            }
            finally
            {
                _cursorService.ResetCursor();
            }
        }

        /// <summary>
        /// Initializes the ViewModel by loading the current user's group memberships.
        /// </summary>
        public async Task InitializeAsync(Action<string>? reportStatus = null)
        {
            try
            {
                _cursorService.SetBusyCursor();
                reportStatus?.Invoke(Resources.loadingPermissions);
                CurrentUserGroups = await ActiveDirectoryService.GetUserGroupsForCurrentUserAsync(reportStatus);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to initialize ExplorerUCViewModel: " + ex.Message);
            }
            finally
            {
                _cursorService.ResetCursor();
            }

        }
        /// <summary>
        /// Loads NTFS permissions for a given folder path and updates the UI.
        /// </summary>        
        public void LoadPermissions(string folderPath, Action<string>? reportStatus = null)
        {
            SelectedFolderPath = folderPath;
            IsLoadingPermissions = true;
            string currentDomain = ActiveDirectoryService.GetNetbiosDomain();

            try
            {
                _cursorService.SetBusyCursor();

                // make sure, that CurrentUserGroups is loaded
                if (CurrentUserGroups == null || CurrentUserGroups.Count == 0)
                {
                    reportStatus?.Invoke(Resources.loadingPermissions);
                    CurrentUserGroups = ActiveDirectoryService.GetUserGroupsForCurrentUserAsync(reportStatus).Result;
                }

                var dirInfo = new DirectoryInfo(folderPath);
                DirectorySecurity acl = dirInfo.GetAccessControl();
                bool isProtected = acl.AreAccessRulesProtected;

                IsInheritedFromParent = !IsInheritedFromParent; // flip
                IsInheritedFromParent = !isProtected;           // set actual value

                var accessRules = acl.GetAccessRules(true, true, typeof(NTAccount));
                var list = new List<PermissionInfo>();

                foreach (FileSystemAccessRule rule in accessRules)
                {
                    string identity = rule.IdentityReference.Value;
                    Debug.WriteLine($"Processing identity in LoadPermissions: {identity}");

                    if (identity.StartsWith(currentDomain + "\\", StringComparison.OrdinalIgnoreCase))
                    {
                        string displayName = identity.Split('\\').Last();
                        list.Add(new PermissionInfo
                        {
                            IdentityReference = displayName,
                            CanModify = rule.FileSystemRights.HasFlag(FileSystemRights.Modify),
                            CanReadExecute = rule.FileSystemRights.HasFlag(FileSystemRights.ReadAndExecute),
                            Rights = rule.FileSystemRights,
                        });
                    }
                }

                CurrentPermissions.Clear();
                foreach (var item in list)
                {
                    CurrentPermissions.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to load permissions: " + ex.Message);
                string message = Resources.msgFailedToLoadPermissions.Replace("{message}", ex.Message);
                MessageBox.Show(message, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);

                var emptyItem = new PermissionInfo
                {
                    IdentityReference = Resources.msgFailedToLoadPermissions,
                    CanModify = false,
                    CanReadExecute = false,
                    Rights = FileSystemRights.Read
                };
                CurrentPermissions.Add(emptyItem);
            }
            finally
            {
                IsLoadingPermissions = false;
                _cursorService.ResetCursor();
            }
        }

        /// <summary>
        /// Opens a folder browser dialog and sets the selected path.
        /// Also triggers loading of root directories.
        /// </summary>
        [RelayCommand]
        private void OpenFolderDialog()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = Resources.txtPleaseSelectNetworkDirectory,
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

        [RelayCommand(CanExecute = nameof(CanRemoveMember))]
        private void RemoveSelectedMember(object? parameter)
        {
            if (SelectedPermission == null || string.IsNullOrWhiteSpace(SelectedFolderPath))
            {
                return;
            }

            NtfsPermissionHelper.RemovePermission(SelectedFolderPath, SelectedPermission.IdentityReference);
            CurrentPermissions.Remove(SelectedPermission);
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveSelectedMemberCommand))]
        [NotifyCanExecuteChangedFor(nameof(SetReadAccessCommand))]
        [NotifyCanExecuteChangedFor(nameof(SetModifyAccessCommand))]
        private PermissionInfo? selectedPermission;

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
        [RelayCommand]
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
                string newPath = Path.Combine(parentPath, NewFolderName ?? string.Empty);
                Debug.WriteLine($"Creating new folder at: {newPath}");

                if (Directory.Exists(newPath))
                {
                    MessageBox.Show(Resources.msgFolderAlreadyExists, Resources.msgHeaderWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Directory.CreateDirectory(newPath);

                if (DisableInheritanceForNewFolder)
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
                string message = Resources.msgErrorAtCreatingNewFolder.Replace("{message}", ex.Message);
                MessageBox.Show($"{message} {ex.Message}", Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens a separate Window to search for and add a member to the selected group.
        /// </summary>
        /// <param name="parameter"></param>
        [RelayCommand(CanExecute = nameof(CanAddSelectedMember))]
        private void AddSelectedMember()
        {
            if (SelectedNode == null)
            {
                MessageBox.Show(Resources.msgSelectFolderFirst, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Open the Active Directory user search window
            var adSearchWindow = new AdUserSearchWindow(false);
            bool? result = adSearchWindow.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(adSearchWindow.SelectedUserDn))
            {
                string samAccountName = adSearchWindow.SamAccountName ?? string.Empty;

                if (CurrentPermissions.Count == 0)
                {
                    // Initialize CurrentPermissions if it's null or empty
                    var tempPermission = new PermissionInfo
                    {
                        IdentityReference = string.Empty,
                        CanModify = false,
                        CanReadExecute = false,
                        Rights = FileSystemRights.Read
                    };
                    CurrentPermissions.Add(tempPermission);
                }
                // Check if the user is already in the group
                bool alreadyInGroup = CurrentPermissions?.Any(m => m?.IdentityReference.Equals(samAccountName, StringComparison.OrdinalIgnoreCase) == true) == true;
                Debug.WriteLine($"Adding user: {samAccountName} to group: {SelectedNode.FullPath}, already in group: {alreadyInGroup}");
                if (!alreadyInGroup)
                {
                    try
                    {
                        //var rights = FileSystemRights.ChangePermissions | FileSystemRights.Modify | FileSystemRights.ReadAndExecute | FileSystemRights.FullControl;
                        var rights = FileSystemRights.Modify | FileSystemRights.ReadAndExecute;

                        NtfsPermissionHelper.AddPermission(SelectedNode.FullPath, samAccountName, rights, AccessControlType.Allow);
                        var newPermission = new PermissionInfo
                        {
                            IdentityReference = samAccountName,
                            CanModify = rights.HasFlag(FileSystemRights.Modify),
                            CanReadExecute = rights.HasFlag(FileSystemRights.ReadAndExecute),
                            Rights = rights
                        };
                        CurrentPermissions?.Add(newPermission);
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

        private bool UserCanEditPermissions(string path)
        {
            try
            {
                var dirInfo = new DirectoryInfo(path);
                var acl = dirInfo.GetAccessControl();
                var rules = acl.GetAccessRules(true, true, typeof(NTAccount));

                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new(identity);

                foreach (FileSystemAccessRule rule in rules)
                {
                    if (principal.IsInRole(rule.IdentityReference.Value) &&
                        rule.AccessControlType == AccessControlType.Allow &&
                        rule.FileSystemRights.HasFlag(FileSystemRights.ChangePermissions))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking permission: {ex.Message}");
            }
            return false;
        }


        private bool CanAddSelectedMember()
        {
            Debug.WriteLine($"CanAddMember: SelectedNode: {SelectedNode?.FullPath}");
            return SelectedNode != null && !IsInheritedFromParent && CanEditPermissions;
        }

        private bool CanRemoveMember()
        {
            Debug.WriteLine($"CanRemoveMember: SelectedNode: {SelectedNode?.FullPath}, {SelectedPermission?.IdentityReference} - {CanEditPermissions}");
            return SelectedNode != null && SelectedPermission != null && CanEditPermissions;
        }


        /// <summary>
        /// Sets Read & Execute permissions for the specified PermissionInfo item.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanChangePermission))]
        private void SetReadAccess(PermissionInfo? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(SelectedFolderPath))
                return;

            _cursorService.SetBusyCursor();
            try
            {
                // remove existing explicit ACEs for this item
                NtfsPermissionHelper.RemovePermission(SelectedFolderPath, item.IdentityReference);

                // allow Read & Execute to user or group
                NtfsPermissionHelper.AddPermission(SelectedFolderPath,
                                                   item.IdentityReference,
                                                   FileSystemRights.ReadAndExecute,
                                                   AccessControlType.Allow);

                // reload permissions
                LoadPermissions(SelectedFolderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Resources.msgFailedToLoadPermissions, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);                
            }
            finally
            {
                _cursorService.ResetCursor();
            }
        }

        /// <summary>
        /// sets Modify permissions for the specified path
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanChangePermission))]
        private void SetModifyAccess(PermissionInfo? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(SelectedFolderPath))
                return;

            _cursorService.SetBusyCursor();
            try
            {
                // remove existing explicit ACEs for this item
                NtfsPermissionHelper.RemovePermission(SelectedFolderPath, item.IdentityReference);

                // allow Modify to user or group
                NtfsPermissionHelper.AddPermission(SelectedFolderPath,
                                                   item.IdentityReference,
                                                   FileSystemRights.Modify,
                                                   AccessControlType.Allow);

                LoadPermissions(SelectedFolderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Resources.msgFailedToLoadPermissions, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _cursorService.ResetCursor();
            }
        }

        private bool CanChangePermission(PermissionInfo? item)
        {
            // only if there is a selected node, item is not null, inheritance is disabled and user can edit permissions
            return SelectedNode != null
                && item != null
                && !IsInheritedFromParent
                && CanEditPermissions;
        }

    }
} // End of namespace
