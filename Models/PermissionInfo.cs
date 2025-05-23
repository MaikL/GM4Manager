using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace GM4ManagerWPF.Models
{
    public class PermissionInfo
    {
        public string IdentityReference { get; set; } = string.Empty;
        public bool CanModify { get; set; }
        public bool CanReadExecute { get; set; }
        public FileSystemRights Rights { get; set; }

        //public event PropertyChangedEventHandler? PropertyChanged;
    }
}
