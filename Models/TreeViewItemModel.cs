using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GM4ManagerWPF.Models
{
    public class TreeViewItemModel
    {
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public ObservableCollection<TreeViewItemModel> Children { get; set; }

        public TreeViewItemModel()
        {
            Children = [];
        }
    }

}
