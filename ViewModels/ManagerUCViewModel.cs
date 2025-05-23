using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Helpers;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Properties;
using GM4ManagerWPF.Views;

namespace GM4ManagerWPF.ViewModels
{
    public class ManagerUCViewModel : INotifyPropertyChanged
    {
        public static ResourceService Res => ResourceService.Instance;
        public ObservableCollection<LvGroupsClass> LvGroupsCollection { get; } =
            ActiveDirectoryService.LoadManagedGroupsViaMembership(new ObservableCollection<LvGroupsClass>());
        public ObservableCollection<string> LvMembersCollection { get; set; } = new();
        private ObservableCollection<string> _groupMembers = new();

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
            GroupMembers = SelectedGroup?.Members != null
                ? [.. SelectedGroup.Members.Select(ActiveDirectoryService.GetNameFromCN)]
                : [];
        }


        public ICommand AddMemberCommand { get; }
        private string? _selectedMember;
        public string? SelectedMember
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
            return SelectedGroup != null && SelectedMember != null;
        }

        private void RemoveSelectedMember(object? parameter)
        {
            if (SelectedGroup == null || SelectedMember == null)
            {
                return;
            }

            // get DN of original list
            string? memberDn = SelectedGroup.Members?.FirstOrDefault(dn =>
                ActiveDirectoryService.GetNameFromCN(dn) == SelectedMember);

            if (memberDn == null)
            {
                MessageBox.Show(Resources.msgErrorCouldNotFindMember, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Remove from AD
            ActiveDirectoryService.RemoveUserFromGroup(SelectedGroup.DistinguishedName, memberDn, SelectedMember);

            // Remove from ViewModel
            SelectedGroup.Members?.Remove(memberDn);
            GroupMembers.Remove(SelectedMember);
            SelectedMember = null;
        }

        private void AddSelectedMember(object? parameter)
        {
            if (SelectedGroup == null)
            {
                MessageBox.Show(Resources.msgSelectGroupFirst, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Open AD user search window
            var adSearchWindow = new AdUserSearchWindow();
            bool? result = adSearchWindow.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(adSearchWindow.SelectedUserDn))
            {
                string dn = adSearchWindow.SelectedUserDn;
                string cn = ActiveDirectoryService.GetNameFromCN(dn);

                if (SelectedGroup.Members != null && !SelectedGroup.Members.Contains(dn))
                {
                    try
                    {
                        // Use IsChecked property instead of Checked event
                        if (adSearchWindow.cbAsAdmin.IsChecked == true)
                        {
                            // Add to AD as Admin
                            ActiveDirectoryService.AddUserToGroupAsAdmin(SelectedGroup.DistinguishedName, dn);
                        }
                        else
                        {
                            // Add to AD
                            ActiveDirectoryService.AddUserToGroup(SelectedGroup.DistinguishedName, dn);
                        }

                        // Update ViewModel
                        SelectedGroup.Members.Add(dn);
                        GroupMembers.Add(cn);
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

        public ObservableCollection<string> GroupMembers
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
