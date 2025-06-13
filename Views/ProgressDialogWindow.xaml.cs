using System.Windows;

namespace GM4ManagerWPF.Views
{
    /// <summary>
    /// Interaction logic for ProgressDialogWindow.xaml
    /// </summary>
    public partial class ProgressDialogWindow : Window
    {
        public ProgressDialogWindow()
        {
            InitializeComponent();
            Application.Current.MainWindow.Closed += (s, e) =>
            {
                if (this.IsVisible)
                {
                    this.Close();
                }
            };

        }
    }

}
