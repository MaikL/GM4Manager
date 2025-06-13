using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Helpers;
using GM4ManagerWPF.Interfaces;
using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Models;
using GM4ManagerWPF.Properties;
using GM4ManagerWPF.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GM4ManagerWPF.ViewModels
{
    public class ProgressDialogViewModel : INotifyPropertyChanged
    {
        public static ResourceService Res => ResourceService.Instance;
        private int _currentProgress;
        private int _totalProgress;
        private string _message = string.Empty; 
        private double _progressPercentage;
        private string _currentPath = string.Empty;

        public ProgressDialogViewModel(string message)
        {
            Message = message;
        }

        public int CurrentProgress
        {
            get { return _currentProgress; }
            set
            {
                if (_currentProgress != value)
                {
                    _currentProgress = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        public int TotalProgress
        {
            get { return _totalProgress; }
            set
            {
                if (_totalProgress != value)
                {
                    _totalProgress = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        public string Message
        {
            get { return _message; }
            set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ProgressPercentage
        {
            get { return _progressPercentage; }
            set
            {
                if (_progressPercentage != value)
                {
                    _progressPercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        public void ReportProgress(int current, int total)
        {
            ProgressPercentage = (double)current / total * 100;
        }
        
        public string CurrentPath
        {
            get { return _currentPath; }
            set
            {
                if (_currentPath != value)
                {
                    _currentPath = value;
                    OnPropertyChanged();
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
              PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
