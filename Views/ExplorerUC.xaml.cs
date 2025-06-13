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
        public ExplorerUC(ExplorerUCViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
            Debug.WriteLine($"[ExplorerUC] DataContext gesetzt: {this.DataContext?.GetType().Name}");
        }      
    }
}