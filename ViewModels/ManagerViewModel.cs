using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Helpers;
using GM4ManagerWPF.Properties;
using GM4ManagerWPF.Views;

namespace GM4ManagerWPF.ViewModels
{
    public class ManagerViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<LvGroupsClass> LvGroupsCollection { get; } =
            ActiveDirectoryService.LoadManagedGroupsViaMembership(new ObservableCollection<LvGroupsClass>());
        public ObservableCollection<string> LvMembersCollection { get; set; } = new();
        private ObservableCollection<string> _groupMembers = new();

        private LvGroupsClass _selectedGroup;
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
            GroupMembers = SelectedGroup != null
                ? new ObservableCollection<string>(
                    SelectedGroup.Members.Select(ActiveDirectoryService.GetNameFromCN))
                : new ObservableCollection<string>();
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
        private string _newMemberCN;
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
            string? memberDn = SelectedGroup.Members.FirstOrDefault(dn =>
                ActiveDirectoryService.GetNameFromCN(dn) == SelectedMember);

            if (memberDn == null)
            {
                MessageBox.Show(Resources.msgErrorCouldNotFindMember, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Entferne aus AD
            ActiveDirectoryService.RemoveUserFromGroup(SelectedGroup.DistinguishedName, memberDn, SelectedMember);

            // Entferne aus ViewModel
            SelectedGroup.Members.Remove(memberDn);
            GroupMembers.Remove(SelectedMember);
            SelectedMember = null;
        }

        // Fix for CS0079: The event 'ToggleButton.Checked' can only appear on the left-hand side of += or -=
        // The issue is that `cbAsAdmin.Checked` is being used as if it were a property, but it is an event.
        // To fix this, we need to use the `IsChecked` property of the CheckBox instead.

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

                if (!SelectedGroup.Members.Contains(dn))
                {
                    try
                    {
                        // Use IsChecked property instead of Checked event
                        if (adSearchWindow.cbAsAdmin.IsChecked == true)
                        {
                            // Add to AD
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

        public ManagerViewModel()
        {
            AddMemberCommand = new RelayCommand(AddSelectedMember, CanAddMember);
            RemoveMemberCommand = new RelayCommand(RemoveSelectedMember, CanRemoveMember);
            LvGroupsCollection = ActiveDirectoryService.LoadManagedGroupsViaMembership(new ObservableCollection<LvGroupsClass>());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ICommand RemoveMemberCommand { get; }
    }
}
