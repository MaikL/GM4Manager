using GM4ManagerWPF.Models;
using GM4ManagerWPF.ViewModels;
using ModernWpf;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace GM4ManagerWPF.Views
{
    /// <summary>
    /// Interaction logic for ExplorerUC.xaml
    /// </summary>
    public partial class ExplorerUC : UserControl
    {
        private ExplorerUCViewModel ViewModel => (ExplorerUCViewModel)DataContext;
        public ExplorerUC()
        {
            this.Cursor = System.Windows.Input.Cursors.Wait;
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            this.Cursor = null;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ExplorerUCViewModel vm)
            {
                vm.PropertyChanged += ViewModel_PropertyChanged;
                UpdateGridHeaders();
            }
        }

        private void DirectoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is DirectoryNodeViewModel selectedNode)
            {
                Debug.WriteLine($"Tree selection changed: {selectedNode.FullPath}");
                ViewModel.SelectedNode = selectedNode;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ViewModel.HeaderMember) or
                                  nameof(ViewModel.HeaderModify)
                                  )
            {
                UpdateGridHeaders();
            }
        }

        private void UpdateGridHeaders()
        {
            if (PermissionsGrid.Columns.Count < 2)
            {
                return;
            }

            PermissionsGrid.Columns[0].Header = ViewModel.HeaderMember;
            PermissionsGrid.Columns[1].Header = ViewModel.HeaderModify;
        }
    }
}
