using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.ViewModels;

namespace GM4ManagerWPF
{
    /// <summary>
    /// Interaction logic for Manager.xaml
    /// </summary>
    public partial class Manager : UserControl
    {
        public Manager()
        {
            InitializeComponent();
            
            this.DataContext = new ManagerViewModel();
            LanguageService.LanguageChanged += OnLanguageChanged;
            LvGroups.Loaded += LvGroups_Loaded;
            LvGroups.SizeChanged += (s, e) => ApplyColumnWidths();
        }
        public ObservableCollection<LvGroupsClass> LvGroupsCollection { get; set; }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            UpdateColumnHeaders();
        }
        private void UpdateColumnHeaders()
        {
            if (LvGroups.View is GridView groupsView)
            {
                groupsView.Columns[0].Header = ResourceService.Instance["colGroupName"];
                groupsView.Columns[1].Header = ResourceService.Instance["colDescription"];
            }

            if (LvMembers.View is GridView membersView)
            {
                membersView.Columns[0].Header = ResourceService.Instance["colMember"];
            }

            // Update button content if needed
            BtnAddMember.Content = ResourceService.Instance["btnAddMember"];
            BtnRemoveMember.Content = ResourceService.Instance["btnRemoveMember"];
        }

        private void LvGroups_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyColumnWidths();
        }
        private void ApplyColumnWidths()
        {
            if (LvGroups.View is GridView gridView)
            {
                double totalWidth = LvGroups.ActualWidth;

                if (gridView.Columns.Count >= 2)
                {
                    gridView.Columns[0].Width = totalWidth * 0.6;
                    gridView.Columns[1].Width = totalWidth * 0.4;
                }
            }
        }        
    }
}
