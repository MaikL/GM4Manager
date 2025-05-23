using GM4ManagerWPF.Helpers;
using GM4ManagerWPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GM4ManagerWPF.Models
{
    public class DirectoryNodeViewModel : INotifyPropertyChanged
    {
        private readonly ExplorerUCViewModel _parent;
        public string Name { get; set; }
        public string FullPath { get; }

        public ICommand SelectCommand { get; }
        public ObservableCollection<DirectoryNodeViewModel> Children { get; } = [];

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
                if (_isExpanded)
                {
                    LoadChildren();
                }
            }
        }
        public DirectoryNodeViewModel(string fullPath, ExplorerUCViewModel parent)
        {
            _parent = parent;
            FullPath = fullPath;
            Name = Path.GetFileName(fullPath) ?? fullPath;
            
            Children.Add(null!);

//            SelectCommand = new RelayCommand(_ => _parent.LoadPermissionsAsync(FullPath));
            SelectCommand = new AsyncRelayCommand(() => _parent.LoadPermissionsAsync(FullPath));

        }
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
                if (_isSelected)
                {
                    _ = _parent.LoadPermissionsAsync(FullPath); // fire & forget
                }
            }
        }
        private void LoadChildren()
        {
            if (Children.Count == 1 && Children[0] == null)
            {
                Children.Clear();
                try
                {
                    foreach (var dir in Directory.GetDirectories(FullPath))
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        if ((dirInfo.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                        {
                            Debug.WriteLine($"Adding directory: {dir}");
                            Children.Add(new DirectoryNodeViewModel(dir, _parent));
                        }
                    }
                }
                catch { }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged; // Declared as nullable to fix CS8618
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
