//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.DirectoryServices.AccountManagement;
//using System.Security.Principal;
//using System.Diagnostics;
//using System.DirectoryServices;

//namespace GM4Manager.Classes
//{
//    public static class UserContext
//    {
//        // List of all groups the current user is a member of
//        public static List<string> UserGroups { get; private set; } = new();

//        /// <summary>
//        /// Loads all domain groups the current user is a member of.
//        /// </summary>        
//        public async static void LoadCurrentUserGroups()
//        {                                    
//            try
//            {
//                UserGroups.Clear();

//                WindowsIdentity identity = WindowsIdentity.GetCurrent();

//                using PrincipalContext context = new(ContextType.Domain);
//                using UserPrincipal user = UserPrincipal.FindByIdentity(context, identity.Name);

//                if (user != null)
//                {
//                    await Task.Run(() =>
//                    {                        
//                        // Use DirectoryEntry to access TokenGroups attribute (fast)
//                        DirectoryEntry de = (DirectoryEntry)user.GetUnderlyingObject();
//                        de.RefreshCache(new[] { "tokenGroups" });

//                        foreach (byte[] sid in (System.Collections.IEnumerable)de.Properties["tokenGroups"])
//                        {
//                            try
//                            {
//                                SecurityIdentifier sidObj = new(sid, 0);
//                                NTAccount account = (NTAccount)sidObj.Translate(typeof(NTAccount));
//                                string groupName = account.Value;

//                                // Filter: only domain groups (no local or BUILTIN)
//                                if (!groupName.StartsWith("BUILTIN\\") && !groupName.StartsWith(Environment.MachineName + "\\"))
//                                {
//                                    UserGroups.Add(groupName);
//                                }
//                            }
//                            catch
//                            {
//                                // Ignore unmapped SIDs
//                            }
//                        }
//                        UserGroups.Add(identity.Name);
//                    });
//                }

//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error loading user groups: {ex.Message}");
//            }
//        }

//    }
//}

