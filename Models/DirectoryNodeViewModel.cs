using GM4ManagerWPF.Helpers;
using GM4ManagerWPF.Properties;
using GM4ManagerWPF.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace GM4ManagerWPF.Models
{
    public class DirectoryNodeViewModel : INotifyPropertyChanged
    {
        private readonly ExplorerUCViewModel _parent;
        public string Name
        {
            get
            {
                if (FullPath == null)
                {
                    return string.Empty;
                }

                var name = Path.GetFileName(FullPath.TrimEnd(Path.DirectorySeparatorChar));
                return string.IsNullOrEmpty(name) ? FullPath : name;
            }
            private set { } // Add a private setter to allow assignment within the constructor
        }

        public string FullPath { get; }

        public ICommand SelectCommand { get; }
        public ObservableCollection<DirectoryNodeViewModel> Children { get; set; } = [];

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
            Name = fullPath;
            
            Children.Add(null!);

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

        public void Refresh()
        {
            try
            {
                Children.Clear();

                if (Directory.Exists(FullPath))
                {
                    foreach (var dir in Directory.GetDirectories(FullPath))
                    {
                        var child = new DirectoryNodeViewModel(dir, this._parent);
                        Children.Add(child);
                    }
                    this.IsExpanded = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing node: {ex.Message}");
            }
        }


        public void RefreshParentBranch()
        {
            try
            {
                // Refresh the current node's children
                Refresh();

                // Refresh the parent node if it exists
                var parentNode = _parent.RootItems.FirstOrDefault(node => node.Children.Contains(this));
                if (parentNode != null)
                {
                    parentNode.Refresh();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to refresh parent branch: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged; // Declared as nullable to fix CS8618
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
