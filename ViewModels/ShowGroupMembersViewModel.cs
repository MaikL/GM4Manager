using CommunityToolkit.Mvvm.ComponentModel;
using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.DirectoryServices;

namespace GM4ManagerWPF.ViewModels
{
    public partial class ShowGroupMembersViewModel : ObservableObject
    {
        public static ResourceService Res => ResourceService.Instance;

        [ObservableProperty]
        private ObservableCollection<LvGroupsClass> searchResults = [];

        public LvGroupsClass? SelectedGroup => SearchResults.FirstOrDefault();

        public ShowGroupMembersViewModel(PermissionInfo selectedPermission)
        {
            Debug.WriteLine($"ShowGroupMembersViewModel initialized. {selectedPermission.IdentityReference}");
            SearchUsers(selectedPermission.IdentityReference);
        }

        private void SearchUsers(string searchTerm)
        {
            SearchResults.Clear();
            Debug.WriteLine($"Searching for users with term: {searchTerm}");
            var filter = $"(&(objectCategory=group)(cn={searchTerm}))";
            SearchResults = ActiveDirectoryService.GetMembersForGroupFromLdap(filter, SearchResults);
        }
    }
}