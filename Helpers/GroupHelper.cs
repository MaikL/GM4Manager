using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace GM4ManagerWPF.Helpers
{
    public static class GroupHelper
    {
        private static List<string>? _cachedUserGroups;

        public static async Task<List<string>> GetUserGroupsForCurrentUserAsync(Action<string>? reportStatus = null)
        {
            if (_cachedUserGroups != null)
            {
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

                        reportStatus?.Invoke($"Lade Gruppe: {name}");
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
