using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GM4ManagerWPF.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace GM4ManagerWPF.Models
{
    public partial class DirectoryNodeViewModel : ObservableObject
    {
        private readonly ExplorerUCViewModel _parent;

        public string FullPath { get; }

        public string Name => Path.GetFileName(FullPath.TrimEnd(Path.DirectorySeparatorChar)) ?? FullPath;

        public ObservableCollection<DirectoryNodeViewModel> Children { get; } = new ObservableCollection<DirectoryNodeViewModel>();

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private bool isSelected;

        public IAsyncRelayCommand OnNodeSelectedAsyncCommand { get; }

        public DirectoryNodeViewModel(string fullPath, ExplorerUCViewModel parent)
        {
            _parent = parent;
            FullPath = fullPath;
            Children.Add(null!); // Lazy loading placeholder

            // Initialize OnNodeSelectedAsyncCommand to avoid nullability issues
            OnNodeSelectedAsyncCommand = new AsyncRelayCommand(OnNodeSelectedAsync);
        }

        partial void OnIsExpandedChanged(bool value)
        {
            if (value)
                LoadChildren();
            else
            {
                Children.Clear();
                Children.Add(null!);
            }
        }

        partial void OnIsSelectedChanged(bool value)
        {
            if (value)
            {
                if (_parent.SelectedNode != this)
                {
                    _parent.SelectedNode = this;
                }
                _parent.LoadPermissions(FullPath);
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
                        var child = new DirectoryNodeViewModel(dir, _parent);
                        Children.Add(child);
                    }
                    IsExpanded = true;
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
                Refresh();
                var parentNode = _parent.RootItems.FirstOrDefault(node => node.Children.Contains(this));
                parentNode?.Refresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to refresh parent branch: {ex.Message}");
            }
        }

        private async Task OnNodeSelectedAsync()
        {
            // Placeholder for async logic when a node is selected
            await Task.CompletedTask;
        }
    }
}
