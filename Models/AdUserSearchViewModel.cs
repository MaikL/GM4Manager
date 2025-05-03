using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GM4ManagerWPF.Models;
using GM4ManagerWPF.Classes;

namespace GM4ManagerWPF.ViewModels
{
    public class AdUserSearchViewModel : INotifyPropertyChanged
    {
        private string _searchTerm = String.Empty;
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
                SearchUsers(); // optional: automatische Suche bei jeder Eingabe
            }
        }

        public ObservableCollection<LdapSearchResult> SearchResults { get; } = new();

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
