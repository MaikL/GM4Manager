using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using GM4ManagerWPF.Models;
using GM4ManagerWPF.Properties;

namespace GM4ManagerWPF.Classes
{
    internal class ActiveDirectoryService
    {        
        /// <summary>
        /// Loads the managed organizational units (OUs) into a ListView.
        /// </summary>
        /// <param name="lvGroups">The ListView to populate with OUs.</param>        
        public static ObservableCollection<LvGroupsClass> LoadManagedOUsToCollection(ObservableCollection<LvGroupsClass> LvGroupsCollection)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                throw new Exception("Debug Mode");
            }
            int counter = 0;

            try
            {
                // Get the current user's distinguished name (DN)
                string currentUserName = WindowsIdentity.GetCurrent().Name; // DOMAIN\Username
                string samAccountName = currentUserName.Split('\\')[1];

                string userDn = GetUserDistinguishedName(samAccountName);

                if (string.IsNullOrEmpty(userDn))
                {
                    MessageBox.Show(Resources.msgUserCoulNotBeFound);
                    throw new Exception(Resources.msgUserCoulNotBeFound);
                }

                string domain = GetCurrentDomain();
                Debug.WriteLine($"selected domain: {domain}");
                // Search for all OUs managed by the user
                DirectoryEntry rootDSE = new("LDAP://" + domain);

                DirectorySearcher searcher = new(rootDSE)
                {
                    Filter = $"(&(objectCategory=group)(ManagedBy={userDn}))"
                };
                searcher.PropertiesToLoad.Add("cn");
                searcher.PropertiesToLoad.Add("distinguishedname");
                searcher.PropertiesToLoad.Add("description");
                searcher.PropertiesToLoad.Add("member");

                Debug.WriteLine($"Number of managed roles found: {searcher.FindAll().Count}");
                foreach (SearchResult result in searcher.FindAll())
                {
                    DirectoryEntry myDirectoryEntry = result.GetDirectoryEntry();

                    List<string> members = [];
                    if (myDirectoryEntry.Properties["member"].Count > 0)
                    {
                        foreach (var member in myDirectoryEntry.Properties["member"])
                        {
                            members.Add(item: member.ToString() ?? string.Empty);
                        }
                    }
                    members.Sort();

                    LvGroupsClass lvGroupsClass = new()
                    {
                        Cn = myDirectoryEntry.Properties["cn"].Value as string ?? string.Empty,
                        DistinguishedName = myDirectoryEntry.Properties["distinguishedname"].Value as string ?? string.Empty,
                        Description = myDirectoryEntry.Properties["description"].Value as string ?? string.Empty,
                        Members = members, // Store as array
                    };
                    Debug.WriteLine($"Cn: {lvGroupsClass.Cn}, DistinguishedName: {lvGroupsClass.DistinguishedName}");

                    LvGroupsCollection.Add(lvGroupsClass);
                    counter++;
                }

                if (counter == 0)
                {
                    LvGroupsClass emptylvGroupsClass = new()
                    {
                        Cn = "no Manager Role found",
                        DistinguishedName = "",
                        Description = "",
                        Members = { }, // Store as array
                    };
                    LvGroupsCollection.Add(emptylvGroupsClass);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Concat(Properties.Resources.txtError, ": ", ex.Message));
            }
            return LvGroupsCollection;
        }

        public static ObservableCollection<LvGroupsClass> LoadManagedGroupsViaMembership(ObservableCollection<LvGroupsClass> LvGroupsCollection)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                throw new Exception("Debug Mode");
            }

            try
            {
                string currentUserName = WindowsIdentity.GetCurrent().Name;
                string samAccountName = currentUserName.Split('\\')[1];
                string userDn = GetUserDistinguishedName(samAccountName);

                if (string.IsNullOrEmpty(userDn))
                {
                    MessageBox.Show(Resources.msgUserCoulNotBeFound, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                    return LvGroupsCollection;
                }

                string domain = GetCurrentDomain();
                DirectoryEntry rootDse = new("LDAP://" + domain);
                HashSet<string> addedDns = new();

                // Step 1: Get groups where user is a member
                List<string> userGroupDns = [];

                using (DirectorySearcher memberSearcher = new(rootDse))
                {
                    memberSearcher.Filter = $"(&(objectCategory=group)(member={userDn}))";
                    memberSearcher.PropertiesToLoad.Add("distinguishedName");

                    foreach (SearchResult result in memberSearcher.FindAll())
                    {
                        if (result.Properties["distinguishedName"]?.Count > 0)
                        {
                            //string groupDn = result.Properties["distinguishedName"][0].ToString();
                            string groupDn = result.Properties.Contains("distinguishedName") && result.Properties["distinguishedName"].Count > 0
                                     ? result.Properties["distinguishedName"][0]?.ToString() ?? ""
                                     : "";
                            userGroupDns.Add(groupDn);
                            Debug.WriteLine($"Member of Group DN: {groupDn}");
                        }
                    }
                }

                // Step 2: For each group-DN, search for groups where managedBy = group-DN
                foreach (string managerDn in userGroupDns)
                {
                    using (DirectorySearcher managerSearcher = new(rootDse))
                    {
                        managerSearcher.Filter = $"(&(objectCategory=group)(managedBy={managerDn}))";
                        managerSearcher.PropertiesToLoad.Add("cn");
                        managerSearcher.PropertiesToLoad.Add("distinguishedname");
                        managerSearcher.PropertiesToLoad.Add("description");
                        managerSearcher.PropertiesToLoad.Add("member");

                        foreach (SearchResult result in managerSearcher.FindAll())
                        {
                            DirectoryEntry groupEntry = result.GetDirectoryEntry();
                            string groupDn = groupEntry.Properties["distinguishedname"].Value?.ToString() ?? "";

                            // Avoid duplicates
                            if (addedDns.Contains(groupDn))
                            {
                                continue;
                            }

                            List<string> members = [];
                            foreach (var member in groupEntry.Properties["member"])
                            {
                                members.Add(member?.ToString() ?? "");
                            }

                            LvGroupsClass group = new()
                            {
                                Cn = groupEntry.Properties["cn"].Value?.ToString() ?? "",
                                DistinguishedName = groupDn,
                                Description = groupEntry.Properties["description"].Value?.ToString() ?? "",
                                Members = members
                            };

                            LvGroupsCollection.Add(group);
                            addedDns.Add(groupDn);
                        }
                    }
                }

                // Fallback if no groups found
                if (LvGroupsCollection.Count == 0)
                {
                    LvGroupsClass fallback = new()
                    {
                        Cn = "No indirectly managed groups found",
                        DistinguishedName = "",
                        Description = "",
                        Members = []
                    };
                    LvGroupsCollection.Add(fallback);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
            }

            return LvGroupsCollection;
        }


        private static string GetCurrentDomain()
        {
            string domain;
            if (true == AppSettingsManager.Settings.OverwriteCurrentDomain)
            {
                domain = AppSettingsManager.Settings.Domain;
            }
            else
            {

                try
                {
                    domain = $"{Domain.GetComputerDomain().ToString()}";
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    MessageBox.Show(Resources.txtNoConnectionToDomain, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                    return string.Empty;
                }
                catch (COMException)
                {
                    MessageBox.Show(Resources.msgNoPartOfADomain);
                    return string.Empty;
                }
            }
            return domain;
        }

        /// <summary>
        /// Removes a user from a specified group in Active Directory.
        /// </summary>
        /// <param name="groupDn">The distinguished name of the group.</param>
        /// <param name="userDn">The distinguished name of the user to remove.</param>
        public static void RemoveUserFromGroup(string groupDn, string userDn, string selectedMember)
        {
            try
            {
                using DirectoryEntry group = new($"LDAP://{groupDn}");
                group.Properties["member"].Remove(userDn); // Remove the user
                group.CommitChanges(); // Save changes
                string message = Resources.msgUserSucessfullyRemoved.Replace("{member}", GetNameFromCN(userDn));
                MessageBox.Show(message, Resources.msgHeaderSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine("Permission error: " + ex.Message);
                // Additional error handling or logging
            }
            catch (Exception ex)
            {
                string msgBoxText = Resources.msgUserRemoveFailed.Replace("{message}", ex.Message);
                MessageBox.Show(Resources.msgUserRemoveFailed, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);                
            }
        }
        /// <summary>
        ///  adds Member to selected group
        /// </summary>
        /// <param name="groupDn"></param>
        /// <param name="userDn"></param>
        public static void AddUserToGroup(string groupDn, string userDn)
        {
            try
            {
                using DirectoryEntry group = new($"LDAP://{groupDn}");

                // Add user to group membership
                group.Properties["member"].Add(userDn);
                group.CommitChanges();
            }
            catch (Exception ex)
            {
                string msgBoxText = Resources.msgErrorAddingUser.Replace("{message}", ex.Message);
                MessageBox.Show(msgBoxText, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                throw new Exception(Resources.msgAskYourAdministrator);
            }
        }
        /// <summary>
        ///  adds Member to selected group
        /// </summary>
        /// <param name="groupDn"></param>
        /// <param name="userDn"></param>
        public static void AddUserToGroupAsAdmin(string groupDn, string userDn)
        {
            try
            {
                using DirectoryEntry group = new($"LDAP://{groupDn}");

                // Add user to group membership
                group.Properties["member"].Add(userDn);
                group.CommitChanges();

                // Get NTAccount from DN
                string samAccountName = ResolveSamAccountNameFromDn(userDn);
                NTAccount account = new(samAccountName);
                SecurityIdentifier sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));

                // Get directory security in correct type
                ActiveDirectorySecurity security = (ActiveDirectorySecurity)group.ObjectSecurity;

                // GUID of the "member" attribute
                Guid memberGuid = new("bf9679c0-0de6-11d0-a285-00aa003049e2");

                // Check if the rule already exists
                bool alreadyHasPermission = security
                    .GetAccessRules(true, true, typeof(SecurityIdentifier))
                    .OfType<ActiveDirectoryAccessRule>()
                    .Any(rule =>
                        rule.IdentityReference == sid &&
                        rule.ActiveDirectoryRights.HasFlag(ActiveDirectoryRights.WriteProperty) &&
                        rule.AccessControlType == AccessControlType.Allow &&
                        rule.ObjectType == memberGuid);

                if (!alreadyHasPermission)
                {
                    var accessRule = new ActiveDirectoryAccessRule(
                                            sid,
                                            ActiveDirectoryRights.WriteProperty,
                                            AccessControlType.Allow,
                                            Guid.Empty,
                                            ActiveDirectorySecurityInheritance.None);

                    security.AddAccessRule(accessRule);

                    // Commit the security changes
                    group.ObjectSecurity = security;
                    group.CommitChanges();
                    string username = GetNameFromCN(userDn);
                    string msgBoxText = Resources.msgUserAddedSucessfully.Replace("{username}", username);
                    MessageBox.Show(msgBoxText, Resources.msgHeaderSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    string username = GetNameFromCN(userDn);
                    string msgBoxText = Resources.msgUserAddedSucessfully.Replace("{username}", username);
                    MessageBox.Show(msgBoxText, Resources.msgHeaderSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                string msgBoxText = Resources.msgErrorAddingUser.Replace("{message}", ex.Message);
                MessageBox.Show(msgBoxText, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                throw new Exception(Resources.msgAskYourAdministrator);
            }
        }


        private static string ResolveSamAccountNameFromDn(string dn)
        {
            using DirectoryEntry userEntry = new($"LDAP://{dn}");
            string samAccount = userEntry.Properties["sAMAccountName"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(samAccount))
            {
                throw new Exception("sAMAccountName not found.");
            }

            string domain = GetCurrentDomain(); // Sollte deinen Domainnamen zurückgeben (z. B. "CONTOSO")
            return $"{domain}\\{samAccount}";
        }

        /// <summary>
        /// Gets the distinguished name of a user from Active Directory.
        /// </summary>
        /// <param name="samAccountName">The SAM account name of the user.</param>
        /// <param name="settings">The application settings.</param>
        /// <returns>The distinguished name of the user, or null if not found.</returns>
        public static string GetUserDistinguishedName(string samAccountName)
        {
            string domain = GetCurrentDomain();

            DirectoryEntry entry = new("LDAP://" + domain);
            DirectorySearcher searcher = new(entry)
            {
                Filter = $"(sAMAccountName={samAccountName})"
            };
            try
            {
                SearchResult? result = searcher.FindOne();
                if (result != null)
                {
                    return result.Properties["distinguishedName"][0].ToString() ?? string.Empty;
                }
                else
                {                    
                    string msgBoxText = Resources.msgUserAddedSucessfully.Replace("{username}", samAccountName);
                    msgBoxText = msgBoxText.Replace("{domain}", domain);
                    MessageBox.Show(msgBoxText, Resources.msgHeaderSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                return String.Concat(Resources.txtErrorOnLoad, " - ", ex.Message);
            }

            return string.Empty;
        }
        /// <summary>
        /// extracts the part before the first ",OU", usally the Name from the whole CN
        /// </summary>
        /// <param name="CN"></param>
        /// <returns></returns>
        public static string GetNameFromCN(string CN)
        {
            int indexOfOU = CN.IndexOf(",OU") - 3;
            string Name = String.Empty;
            if (indexOfOU > 0)
            {
                Name = CN.Substring(3, indexOfOU);
            }
            return Name;
        }

        public static List<LdapSearchResult> SearchUsers(string searchTerm)
        {
            var results = new List<LdapSearchResult>();

            string domain = GetCurrentDomain();
            DirectoryEntry entry = new("LDAP://" + domain);
            DirectorySearcher searcher = new(entry)
            {
                Filter = $"(&(|(objectClass=user)(objectClass=group))(|(cn=*{searchTerm}*)(displayName=*{searchTerm}*)(sAMAccountName=*{searchTerm}*))(!(userAccountControl:1.2.840.113556.1.4.803:=2)))",
                PageSize = 50
            };

            searcher.PropertiesToLoad.Add("distinguishedName");
            searcher.PropertiesToLoad.Add("cn");
            searcher.PropertiesToLoad.Add("displayName");
            searcher.PropertiesToLoad.Add("objecTClass");

            SearchResultCollection searchResults = searcher.FindAll();

            foreach (SearchResult result in searchResults)
            {
                Debug.WriteLine($"Search result path: {result.Path}");

                if (result != null && result.Properties.Contains("distinguishedName") && result.Properties["distinguishedName"].Count > 0)
                {
                    Debug.WriteLine(result.Properties["distinguishedName"][0].ToString());
                    var ldapResult = new LdapSearchResult
                    {
                        DistinguishedName = result.Properties["distinguishedName"][0]?.ToString() ?? "",
                        CommonName = result.Properties.Contains("cn") && result.Properties["cn"].Count > 0
                                     ? result.Properties["cn"][0]?.ToString() ?? ""
                                     : "",
                        DisplayName = result.Properties.Contains("displayName") && result.Properties["displayName"].Count > 0
                                      ? result.Properties["displayName"][0]?.ToString() ?? ""
                                      : "",
                        ObjectClass = result.Properties.Contains("objectClass") && result.Properties["objectClass"].Count > 0
                          ? result.Properties["objectClass"][0]?.ToString() ?? ""
                          : ""
                    };

                    results.Add(ldapResult);
                }
                else
                {
                    string msgBoxText = Resources.msgNoResultsForDomain.Replace("{domain}", domain);
                    msgBoxText = msgBoxText.Replace("{username}", searchTerm);
                    MessageBox.Show(msgBoxText, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return results;
        }
        public static List<LdapSearchResult> GetMyGroupsFromLdap(string userDn)
        {
            var groups = new List<LdapSearchResult>();
            string domain = GetCurrentDomain();

            using var entry = new DirectoryEntry("LDAP://" + domain);
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = $"(&(objectClass=group)(member={userDn}))",
                PageSize = 1000
            };

            searcher.PropertiesToLoad.Add("cn");
            searcher.PropertiesToLoad.Add("distinguishedName");
            searcher.PropertiesToLoad.Add("description");

            foreach (SearchResult result in searcher.FindAll())
            {
                var ldapResult = new LdapSearchResult
                {
                    DistinguishedName = result.Properties["distinguishedName"]?[0]?.ToString() ?? "",
                    CommonName = result.Properties["cn"]?[0]?.ToString() ?? "",
                    DisplayName = result.Properties.Contains("description")
                        ? result.Properties["description"]?[0]?.ToString() ?? ""
                        : "",
                    ObjectClass = "group"
                };

                groups.Add(ldapResult);
            }

            return groups;
        }


    }
}
