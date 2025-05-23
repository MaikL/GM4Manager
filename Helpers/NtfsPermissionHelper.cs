using GM4ManagerWPF.Models;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace GM4ManagerWPF.Helpers
{
    public static class NtfsPermissionHelper
    {
        public static List<NtfsPermissionEntry> GetFilteredPermissions(string folderPath, List<string> userGroups)
        {
            var result = new List<NtfsPermissionEntry>();

            try
            {
                var dirInfo = new DirectoryInfo(folderPath);
                var security = dirInfo.GetAccessControl();

                var accessRules = security.GetAccessRules(true, true, typeof(NTAccount));

                foreach (FileSystemAccessRule rule in accessRules)
                {
                    string identity = rule.IdentityReference.Value;

                    // Filter: only groups, that the user is a member of
                    if (!userGroups.Any(group => identity.EndsWith(group, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    string rights = rule.FileSystemRights.ToString();
                    string accessType = rule.AccessControlType.ToString();

                    result.Add(new NtfsPermissionEntry
                    {
                        IdentityReference = identity,
                        AccessType = rights,
                        AccessControlType = accessType
                    });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                result.Add(new NtfsPermissionEntry
                {
                    IdentityReference = "ERROR",
                    AccessType = $"Access denied: {ex.Message}",
                    AccessControlType = "N/A"
                });
            }

            return result;
        }

        public static void SetPermission(string folderPath, string identity, FileSystemRights rights, AccessControlType controlType)
        {
            var dirInfo = new DirectoryInfo(folderPath);
            var dirSecurity = dirInfo.GetAccessControl();

            var ntAccount = new NTAccount(identity);

            var rule = new FileSystemAccessRule(
                ntAccount,
                rights,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                controlType
            );

            dirSecurity.AddAccessRule(rule);
            dirInfo.SetAccessControl(dirSecurity);
        }

        public static void RemovePermission(string folderPath, string identity, FileSystemRights rights)
        {
            var dirInfo = new DirectoryInfo(folderPath);
            var dirSecurity = dirInfo.GetAccessControl();

            var ntAccount = new NTAccount(identity);

            var rule = new FileSystemAccessRule(
                ntAccount,
                rights,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow
            );

            dirSecurity.RemoveAccessRule(rule);
            dirInfo.SetAccessControl(dirSecurity);
        }
    }
}
