using CommunityToolkit.Mvvm.ComponentModel;
using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Models;
using System.Collections.ObjectModel;

namespace GM4ManagerWPF.ViewModels
{
    public partial class AdUserSearchWindowViewModel : ObservableObject
    {
        public static ResourceService Res => ResourceService.Instance;

        [ObservableProperty]
        private string searchTerm = string.Empty;
        [ObservableProperty]
        private bool isAdminOptionEnabled;

        public ObservableCollection<LdapSearchResult> SearchResults { get; } = new();

        public AdUserSearchWindowViewModel()
        {
        }
        public AdUserSearchWindowViewModel(bool enableAdminOption)
        {
            IsAdminOptionEnabled = enableAdminOption;
        }
        partial void OnSearchTermChanged(string value)
        {
            SearchUsers();
        }

        private void SearchUsers()
        {
            SearchResults.Clear();

            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                return;
            }

            var results = ActiveDirectoryService.SearchUsers(SearchTerm);
            foreach (var result in results)
            {
                SearchResults.Add(result);
            }
        }
    }
}
