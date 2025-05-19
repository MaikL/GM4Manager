using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Models;

namespace GM4ManagerWPF.ViewModels
{
    public class AdUserSearchWindowViewModel : INotifyPropertyChanged
    {
        public static ResourceService Res => ResourceService.Instance;

        private string _searchTerm = String.Empty;
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
                SearchUsers();
            }
        }        

        public ObservableCollection<LdapSearchResult> SearchResults { get; } = [];

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

        public AdUserSearchWindowViewModel()
        {
            
        }                

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
              PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
