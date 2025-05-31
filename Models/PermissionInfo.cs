using System.Security.AccessControl;

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
