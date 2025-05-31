using GM4ManagerWPF.Models;
using System.Data;
using System.Diagnostics;
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
        public static void AddPermission(string folderPath, string identity, FileSystemRights rights, AccessControlType controlType)
        {
            var dirInfo = new DirectoryInfo(folderPath);
            var dirSecurity = dirInfo.GetAccessControl();
            var ntAccount = new NTAccount(identity);
            Debug.WriteLine($"Adding permission for {identity} with rights {rights} and control type {controlType} to {folderPath}");
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

        public static void RemovePermission(string folderPath, string identity)
        {
            var dirInfo = new DirectoryInfo(folderPath);
            var dirSecurity = dirInfo.GetAccessControl();

            var ntAccount = new NTAccount(identity);

            // Remove all access rules for the specified identity
            dirSecurity.PurgeAccessRules(ntAccount);

            dirInfo.SetAccessControl(dirSecurity);
        }


        /// <summary>
        /// Removes NTFS permissions for a specific identity and rights from a folder.
        /// </summary>
        /// <param name="folderPath">The full path to the folder.</param>
        /// <param name="identity">The NTAccount identity (e.g., DOMAIN\User).</param>
        /// <param name="rights">The specific rights to remove (e.g., Modify).</param>
        public static void RemovePermission(string folderPath, string identity, FileSystemRights rights)
        {
            var dirInfo = new DirectoryInfo(folderPath);
            var dirSecurity = dirInfo.GetAccessControl();

            var rules = dirSecurity.GetAccessRules(true, true, typeof(NTAccount));

            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.IdentityReference.Value.Equals(identity, StringComparison.OrdinalIgnoreCase) &&
                    rule.AccessControlType == AccessControlType.Allow &&
                    rule.FileSystemRights.HasFlag(rights))
                {
                    dirSecurity.RemoveAccessRuleSpecific(rule);
                }
                else
                {
                    Debug.WriteLine($"No matching rule found for identity: {identity} with rights: {rights}. HAS: {rule.FileSystemRights.ToString()}");
                }
            }

            dirInfo.SetAccessControl(dirSecurity);
        }
        public static bool CurrentUserCanEditPermissions(string folderPath)
        {
            try
            {
                var currentUser = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(currentUser);

                var dirInfo = new DirectoryInfo(folderPath);
                var acl = dirInfo.GetAccessControl(AccessControlSections.Access);

                AuthorizationRuleCollection rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier));

                foreach (FileSystemAccessRule rule in rules)
                {
                    if (currentUser.User != null && rule.IdentityReference == currentUser.User)
                    {
                        if (rule.AccessControlType == AccessControlType.Allow &&
                            (rule.FileSystemRights.HasFlag(FileSystemRights.ChangePermissions) ||
                             rule.FileSystemRights.HasFlag(FileSystemRights.FullControl)))
                        {
                            Debug.WriteLine("Current user has permission to edit NTFS permissions.");
                            return true;
                        }
                    }

                    // check user’s groups
                    if (currentUser.Groups != null)
                    {
                        foreach (var group in currentUser.Groups)
                        {
                            if (rule.IdentityReference == group &&
                                rule.AccessControlType == AccessControlType.Allow &&
                                (rule.FileSystemRights.HasFlag(FileSystemRights.ChangePermissions) ||
                                 rule.FileSystemRights.HasFlag(FileSystemRights.FullControl)))
                            {
                                Debug.WriteLine("Current user has permission to edit NTFS permissions through group membership.");
                                return true;
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Current user has no groups assigned.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error checking edit permission: " + ex.Message);
            }

            return false;
        }
    }
}
