using GM4ManagerWPF.Properties;
using System.Diagnostics;
using System.Security.Principal;

namespace GM4ManagerWPF.Helpers
{
    public static class GroupHelper
    {
        private static List<string>? _cachedUserGroups;

        public static async Task<List<string>> GetUserGroupsForCurrentUserAsync(Action<string>? reportStatus = null)
        {
            Debug.WriteLine("GroupHelper.GetUserGroupsForCurrentUserAsync called.");
            if (_cachedUserGroups != null)
            {
                Debug.WriteLine("Returning cached user groups.");
                return _cachedUserGroups;
            }

            return await Task.Run(() =>
            {
                var identity = WindowsIdentity.GetCurrent();
                if (identity == null || identity.Groups == null)
                {
                    return []; // Return an empty list if identity or groups are null
                }

                var result = new List<string>();

                foreach (var group in identity.Groups)
                {
                    try
                    {
                        var name = group.Translate(typeof(NTAccount)).Value;
                        result.Add(name);

                        string reportText = Resources.loadGroup.Replace("{name}", name);
                        reportStatus?.Invoke(reportText);
                    }
                    catch
                    {
                        // Ignore Grupps, that can´t be resolved
                    }
                }

                _cachedUserGroups = result;
                return result;
            });
        }
    }

}
