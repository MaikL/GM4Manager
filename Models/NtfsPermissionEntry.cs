using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GM4ManagerWPF.Models
{
    public class NtfsPermissionEntry
    {
        public string IdentityReference { get; set; } = string.Empty;
        public string AccessType { get; set; } = string.Empty; // Read, Write, etc.
        public string AccessControlType { get; set; } = string.Empty; // Allow, Deny
    }

}
