using GM4ManagerWPF.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace GM4ManagerWPF.Views
{
    /// <summary>
    /// Interaction logic for Manager.xaml
    /// </summary>
    public partial class ManagerUC : UserControl
    {
        public ManagerUC()
        {
            InitializeComponent();

            // Initialize LvGroupsCollection to avoid nullability issues
            LvGroupsCollection = [];

            LvGroups.Loaded += LvGroups_Loaded;
            LvGroups.SizeChanged += (s, e) => ApplyColumnWidths();
        }
        public ObservableCollection<LvGroupsClass> LvGroupsCollection { get; set; }

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
                    gridView.Columns[0].Width = totalWidth * 0.59;
                    gridView.Columns[1].Width = totalWidth * 0.39;
                }
            }
        }
    }
}
