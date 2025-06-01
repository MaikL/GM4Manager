using System.Collections.ObjectModel;

namespace GM4ManagerWPF.Models
{
    public class TreeViewItemModel
    {
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public bool InheritanceDisabled { get; set; } // Overlay
        public string InheritanceStatusDisplay => InheritanceDisabled ? "inheritance deactivated" : "";
        public ObservableCollection<TreeViewItemModel> Children { get; set; }

        public TreeViewItemModel()
        {
            Children = [];
        }
    }

}
