using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Helpers;
using GM4ManagerWPF.Interfaces;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Models;
using GM4ManagerWPF.Properties;
using GM4ManagerWPF.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace GM4ManagerWPF.ViewModels
{
    public partial class ManagerUCViewModel : ObservableObject
    {
        public static ResourceService Res => ResourceService.Instance;

        [ObservableProperty]
        private ObservableCollection<LvGroupsClass> lvGroupsCollection =
                ActiveDirectoryService.LoadManagedGroupsViaMembership([]);

        public ObservableCollection<string> LvMembersCollection { get; set; } = [];

        [ObservableProperty]
        private ObservableCollection<GroupMemberDisplay> groupMembers = [];                

        [ObservableProperty]
        private LvGroupsClass? selectedGroup;
        partial void OnSelectedGroupChanged(LvGroupsClass? value)
        {
            UpdateGroupMembers();
        }        

        partial void OnSelectedMemberChanged(GroupMemberDisplay? value)
        {
            RemoveSelectedMemberCommand.NotifyCanExecuteChanged();
            AddSelectedMemberCommand.NotifyCanExecuteChanged();
        }

        private void UpdateGroupMembers()
        {
            try
            {
                _cursorService.SetBusyCursor();
                Debug.WriteLine($"Updating group members for group: {SelectedGroup?.Cn}");
                GroupMembers.Clear();

                if (SelectedGroup != null)
                {
                    SelectedGroup.Members?.Clear();
                    ObservableCollection<LvGroupsClass> LvGroupsCollection = [];
                    var filter = $"(&(objectCategory=group)(cn={SelectedGroup.Cn}))";
                    LvGroupsCollection = ActiveDirectoryService.GetMembersForGroupFromLdap(filter, LvGroupsCollection);
                    foreach (var group in LvGroupsCollection)
                    {
                        foreach (var member in group.Members)
                        {
                            GroupMembers?.Add(new GroupMemberDisplay
                            {
                                Name = $" {member.CommonName} ({member.DisplayName})",
                                SamAccountName = member.SamAccountName,
                                Cn = member.CommonName,
                                Icon = member.ObjectClass == "user" ? "👤" :
                                       member.ObjectClass == "group" ? "👥" : "❓"
                            });
                            Debug.WriteLine($"UpdateGroupMembers - Added member: {member.CommonName} ({member.DisplayName}) - {member.SamAccountName}");
                        }
                    }
                    SelectedGroup.Members = LvGroupsCollection.SelectMany(g => g.Members).ToList();
                }
            }
            finally
            {
                _cursorService.ResetCursor();
            }
        }

        [ObservableProperty]
        private GroupMemberDisplay? selectedMember;

        [ObservableProperty]
        private string? newMemberCN;
        
        private bool CanRemoveMember(object? parameter)
        {
            return SelectedGroup != null && SelectedMember != null;
        }

        [RelayCommand(CanExecute = nameof(CanRemoveMember))]
        private void RemoveSelectedMember(object? parameter)
        {
            if (SelectedGroup == null || SelectedMember == null || SelectedMember.Name == null)
            {
                return;
            }
            else
            {
                Debug.WriteLine($"SelectedMember.SamAccountName: {SelectedMember?.SamAccountName}");                

                // Find the original LdapMember object in the group based on the display name
                var memberToRemove = SelectedGroup.Members?
                    .FirstOrDefault(m => m.SamAccountName == SelectedMember.SamAccountName);
                Debug.WriteLine($"Removing member: {SelectedMember.Cn} {SelectedMember.SamAccountName} ");
                if (memberToRemove?.DistinguishedName == null)
                {
                    MessageBox.Show(Resources.msgErrorCouldNotFindMember, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Remove the member from Active Directory
                ActiveDirectoryService.RemoveUserFromGroup(
                    SelectedGroup.DistinguishedName,
                    memberToRemove.DistinguishedName,
                    SelectedMember.Name);

                // Remove from the internal group member list
                SelectedGroup.Members?.Remove(memberToRemove);

                // Remove from the UI-bound collection
                GroupMembers.Remove(SelectedMember);
            }
            // Clear the selection
            SelectedMember = null;
        }

        /// <summary>
        /// Opens a separate Window to search for and add a member to the selected group.
        /// </summary>
        /// <param name="parameter"></param>

        [RelayCommand(CanExecute = nameof(CanAddMember))]
        private void AddSelectedMember()
        {
            if (SelectedGroup == null)
            {
                MessageBox.Show(Resources.msgSelectGroupFirst, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Open the Active Directory user search window
            var adSearchWindow = new AdUserSearchWindow(false);
            bool? result = adSearchWindow.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(adSearchWindow.SelectedUserDn))
            {
                string dn = adSearchWindow.SelectedUserDn;
                string cn = ActiveDirectoryService.GetNameFromCN(dn);


                if (SelectedGroup.Members == null)
                {
                    SelectedGroup.Members = [];
                }
                // Check if the user is already in the group
                bool alreadyInGroup = SelectedGroup.Members.Any(m => m.DistinguishedName?.Equals(dn, StringComparison.OrdinalIgnoreCase) == true);

                if (!alreadyInGroup)
                {
                    try
                    {
                        // Add the user to the group in Active Directory
                        if (adSearchWindow.cbAsAdmin.IsChecked == true)
                        {
                            ActiveDirectoryService.AddUserToGroupAsAdmin(SelectedGroup.DistinguishedName, dn);
                        }
                        else
                        {
                            ActiveDirectoryService.AddUserToGroup(SelectedGroup.DistinguishedName, dn);
                        }

                        // Create a new LdapMember object
                        var newMember = adSearchWindow.SearchResult;

                        // Add to the internal group member list
                        SelectedGroup.Members.Add(newMember);

                        // Add to the UI-bound collection with icon
                        GroupMembers?.Add(new GroupMemberDisplay
                        {
                            Name = cn,
                            Icon = "👤"
                        });
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
        

        private bool CanAddMember()
        {
            return SelectedGroup != null;
        }        
        
        private readonly ICursorService _cursorService;

        public ManagerUCViewModel(ICursorService cursorService)
        {
            _cursorService = cursorService;
        }
        public async Task InitializeAsync(Action<string>? reportStatus = null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                reportStatus?.Invoke("Loading Active Directory groups...");

                // Load the managed groups and their members
                await LoadManagedGroupsViaMembership();
                reportStatus?.Invoke("Manager is ready.");
            }
            catch (Exception ex)
            {
                reportStatus?.Invoke("Failed to load Manager data.");
                Debug.WriteLine($"[ManagerUC] Initialization error: {ex.Message}");
            }

            sw.Stop();
            Debug.WriteLine($"Splash dauerte: {sw.ElapsedMilliseconds} ms");
        }
        private async Task LoadManagedGroupsViaMembership()
        {
            var sw = Stopwatch.StartNew();
            // Simulate async AD group loading
            await Task.Delay(100);

            var groups = ActiveDirectoryService.LoadManagedGroupsViaMembership(new ObservableCollection<LvGroupsClass>());

            // Replace or update your bound collection
            LvGroupsCollection.Clear();
            foreach (var group in groups)
            {
                LvGroupsCollection.Add(group);
            }

            if (LvGroupsCollection.Any(g => !g.IsPlaceholder))
            {
                SelectedGroup = LvGroupsCollection.First(g => !g.IsPlaceholder);
            }

            sw.Stop();
            Debug.WriteLine($"ManagerUCViewModel LoadManagedGroupsViaMembership : {sw.ElapsedMilliseconds} ms");
        }
    }
}
