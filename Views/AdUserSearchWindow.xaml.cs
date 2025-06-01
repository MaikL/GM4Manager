using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Models;
using GM4ManagerWPF.ViewModels;
using System.Windows;


namespace GM4ManagerWPF.Views
{
    /// <summary>
    /// Interaction logic for AdUserSearchWindow.xaml
    /// </summary>
    public partial class AdUserSearchWindow : Window
    {
        public string? SelectedUserDn { get; private set; }
        public string? SamAccountName { get; private set; }
        public AdUserSearchWindow()
        {
            InitializeComponent();
            SearchBox.KeyDown += SearchBox_KeyDown;
            this.DataContext = new AdUserSearchWindowViewModel();
        }

        private void OnSelectClick(object sender, RoutedEventArgs e)
        {
            if (ResultsList.SelectedItem is LdapSearchResult result)
            {
                SelectedUserDn = result.DistinguishedName;
                SamAccountName = result.SamAccountName;
                DialogResult = true;
            }
        }
        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Tab)
            {
                PerformSearch();
            }
        }

        private void PerformSearch()
        {
            this.Cursor = System.Windows.Input.Cursors.Wait;

            string searchTerm = SearchBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                List<LdapSearchResult> results = ActiveDirectoryService.SearchUsers(searchTerm);
                ResultsList.ItemsSource = results;
            }
            this.Cursor = null;
        }
    }

}

