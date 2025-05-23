using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Models;
using GM4ManagerWPF.Properties;
using GM4ManagerWPF.ViewModels;
using Ookii.Dialogs.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
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
                                  nameof(ViewModel.HeaderModify) or
                                  nameof(ViewModel.HeaderReadAndExecute))
            {
                UpdateGridHeaders();
            }
        }

        private void UpdateGridHeaders()
        {
            if (PermissionsGrid.Columns.Count < 3)
            {
                return;
            }

            PermissionsGrid.Columns[0].Header = ViewModel.HeaderMember;
            PermissionsGrid.Columns[1].Header = ViewModel.HeaderModify;
            PermissionsGrid.Columns[2].Header = ViewModel.HeaderReadAndExecute;
        }
    }
}
