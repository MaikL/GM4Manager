using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GM4ManagerWPF.ViewModels
{
    public class ShowGroupMembersViewModel : INotifyPropertyChanged
    {
        public static ResourceService Res => ResourceService.Instance;
        
        public ObservableCollection<LvGroupsClass>? _searchResults;
        public ObservableCollection<LvGroupsClass> SearchResults
        {
            get => _searchResults ??= new ObservableCollection<LvGroupsClass>();
            set
            {
                _searchResults = value;
                OnPropertyChanged();
            }
        }
        public LvGroupsClass? SelectedGroup => SearchResults.FirstOrDefault();

        public ShowGroupMembersViewModel(PermissionInfo SelectedPermission)
        {
            Debug.WriteLine($"ShowGroupMembersViewModel initialized. {SelectedPermission.IdentityReference}");
            SearchUsers(SelectedPermission.IdentityReference);
        }
        

        private void SearchUsers(string searchTerm)
        {
            SearchResults.Clear();
            Debug.WriteLine($"Searching for users with term: {searchTerm}");

            var filter = $"(&(objectCategory=group)(cn={searchTerm}))";
            SearchResults = ActiveDirectoryService.GetMembersForGroupFromLdap(filter, SearchResults);            
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
              PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
