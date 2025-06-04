using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Helpers;
using System.Diagnostics;
using System.Windows;

namespace GM4ManagerWPF
{
    /// <summary>  
    /// Interaction logic for App.xaml  
    /// </summary>  
    public partial class App : Application
    {
        private SplashScreenWindow splashScreen = null!; // Use null-forgiving operator to suppress CS8618  

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var sw = Stopwatch.StartNew();
            
            // Show splash  
            splashScreen = new SplashScreenWindow();
            splashScreen.Show();

            var groups = await GroupHelper.GetUserGroupsForCurrentUserAsync(status =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    splashScreen.UpdateStatus(status);
                });
            });

            try
            {
                // Simulate config loading  
                AppSettingsManager.Load(); // or await if async  
                await Task.Delay(800); // Simulate loading time  
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load settings:\n" + ex.Message);
                Shutdown();
                return;
            }

            // Show main window  
            var mainWindow = new MainWindow();
            mainWindow.Loaded += (s, ev) =>
            {
                // Fade out splash screen after main window is shown  
                splashScreen.BeginFadeOut(() =>
                {
                    splashScreen.Close();
                });
            };
            
            sw.Stop();
            Debug.WriteLine($"Splash dauerte: {sw.ElapsedMilliseconds} ms");

            mainWindow.Show(); // Triggers Loaded event  
        }
    }
}
