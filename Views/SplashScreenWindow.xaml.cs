using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GM4ManagerWPF
{
    /// <summary>
    /// Interaction logic for SplashScreenWindow.xaml
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
        }
        public void BeginFadeOut(Action onComplete)
        {
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
            fadeOut.Completed += (s, e) => onComplete?.Invoke();
            this.BeginAnimation(Window.OpacityProperty, fadeOut);
        }
    }
}
