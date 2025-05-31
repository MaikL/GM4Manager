using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Helpers;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Properties;
using GM4ManagerWPF.Views;
using GM4ManagerWPF.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace GM4ManagerWPF.ViewModels
{
    public class ManagerUCViewModel : INotifyPropertyChanged
    {
        public static ResourceService Res => ResourceService.Instance;
        public ObservableCollection<LvGroupsClass> LvGroupsCollection { get; } =
                ActiveDirectoryService.LoadManagedGroupsViaMembership(new ObservableCollection<LvGroupsClass>());
        public ObservableCollection<string> LvMembersCollection { get; set; } = [];
        private ObservableCollection<GroupMemberDisplay> _groupMembers = [];
        public ICommand AddMemberCommand { get; }
        private GroupMemberDisplay? _selectedMember;       

        private LvGroupsClass _selectedGroup = new()
        {
            Cn = string.Empty,
            DistinguishedName = string.Empty,
            Description = null,
            Members = []
        };
        public LvGroupsClass SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _selectedGroup = value;
                OnPropertyChanged(nameof(SelectedGroup));
                UpdateGroupMembers();
            }
        }
        private void UpdateGroupMembers()
        {
            GroupMembers.Clear();

            if (SelectedGroup?.Members != null)
            {
                foreach (var member in SelectedGroup.Members)
                {
                    GroupMembers.Add(new GroupMemberDisplay
                    {
                        Name = ActiveDirectoryService.GetNameFromCN(member),
                        Icon = member.ObjectClass == "user" ? "👤" :
                               member.ObjectClass == "group" ? "👥" : "❓"
                    });
                }
            }
        }

        public GroupMemberDisplay? SelectedMember
        {
            get => _selectedMember;
            set
            {
                _selectedMember = value;
                OnPropertyChanged(nameof(SelectedMember));
            }
        }
        private string _newMemberCN = string.Empty;
        public string NewMemberCN
        {
            get => _newMemberCN;
            set
            {
                _newMemberCN = value;
                OnPropertyChanged(nameof(NewMemberCN));
            }
        }

        private bool CanRemoveMember(object? parameter)
        {
            Debug.WriteLine($"CanRemoveMember called with SelectedGroup: {SelectedGroup?.Cn}, SelectedMember: {SelectedMember?.Name}");
            return SelectedGroup != null && SelectedMember != null;
        }

        private void RemoveSelectedMember(object? parameter)
        {
            if (SelectedGroup == null || SelectedMember == null || SelectedMember.Name == null)
            {
                return;
            }

            // Find the original LdapMember object in the group based on the display name
            var memberToRemove = SelectedGroup.Members?
                .FirstOrDefault(m => ActiveDirectoryService.GetNameFromCN(m) == SelectedMember.Name);

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

            // Clear the selection
            SelectedMember = null;
        }

        /// <summary>
        /// Opens a separate Window to search for and add a member to the selected group.
        /// </summary>
        /// <param name="parameter"></param>
        private void AddSelectedMember(object? parameter)
        {
            if (SelectedGroup == null)
            {
                MessageBox.Show(Resources.msgSelectGroupFirst, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Open the Active Directory user search window
            var adSearchWindow = new AdUserSearchWindow();
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
                        var newMember = new LdapMember
                        {
                            DistinguishedName = dn,
                            ObjectClass = "user" // Assuming only users are added via this dialog
                        };

                        // Add to the internal group member list
                        SelectedGroup.Members.Add(newMember);

                        // Add to the UI-bound collection with icon
                        GroupMembers.Add(new GroupMemberDisplay
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


        private bool CanAddMember(object? parameter)
        {
            return SelectedGroup != null;
        }

        public ObservableCollection<GroupMemberDisplay> GroupMembers
        {
            get => _groupMembers;
            set
            {
                _groupMembers = value;
                OnPropertyChanged(nameof(GroupMembers));
            }
        }

        public ManagerUCViewModel()
        {
            AddMemberCommand = new RelayCommand(AddSelectedMember, CanAddMember);
            RemoveMemberCommand = new RelayCommand(RemoveSelectedMember, CanRemoveMember);
            LvGroupsCollection = ActiveDirectoryService.LoadManagedGroupsViaMembership(new ObservableCollection<LvGroupsClass>());
            // get PropertyChanged-Event from ResourceService
            ResourceService.Instance.PropertyChanged += OnLanguageChanged;          
        }
        public async Task InitializeAsync(Action<string>? reportStatus = null)
        {
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
        }
        private async Task LoadManagedGroupsViaMembership()
        {
            // Simulate async AD group loading
            await Task.Delay(100); // Remove in production

            var groups = ActiveDirectoryService.LoadManagedGroupsViaMembership(new ObservableCollection<LvGroupsClass>());

            // Replace or update your bound collection
            LvGroupsCollection.Clear();
            foreach (var group in groups)
            {
                LvGroupsCollection.Add(group);
            }

            // Optionally auto-select the first group
            if (LvGroupsCollection.Count > 0)
                SelectedGroup = LvGroupsCollection[0];
        }



        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
              PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine("Updating Language");
                //UpdateDynamicProperties();
        }
        public ICommand RemoveMemberCommand { get; }      
    }
}
