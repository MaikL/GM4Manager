using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.ActiveDirectory;

namespace GM4ManagerWPF.Classes
{
    public static class PermissionService
    {
        public static IEnumerable<FileSystemAccessRule> GetUserRelevantPermissions(string folderPath)
        {
            var identity = WindowsIdentity.GetCurrent();
            if (identity == null || identity.Groups == null)
            {
                throw new InvalidOperationException("Unable to retrieve the current Windows identity or its groups.");
            }

            var userSids = identity.Groups.Select(g => g.Value).ToHashSet();

            // Fix: Use DirectoryInfo to get access control
            var directoryInfo = new DirectoryInfo(folderPath);
            var security = directoryInfo.GetAccessControl();
            var accessRules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));

            return accessRules
                .Cast<FileSystemAccessRule>()
                .Where(rule => userSids.Contains(rule.IdentityReference.Value));
        }

        public static bool HasWriteAccess(FileSystemAccessRule rule)
        {
            return rule.AccessControlType == AccessControlType.Allow &&
                   rule.FileSystemRights.HasFlag(FileSystemRights.Write);
        }

        // Methoden zum Hinzufügen/Entfernen folgen...
    }

}
