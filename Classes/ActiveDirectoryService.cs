using GM4ManagerWPF.Models;
using GM4ManagerWPF.Properties;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Windows;

namespace GM4ManagerWPF.Classes
{
    internal class ActiveDirectoryService
    {
        private static ObservableCollection<LvGroupsClass>? _cachedManagedGroups;
        private static ObservableCollection<LvGroupsClass>? _cachedManagedOUs;

        public static string GetSamAccountName () {
            return WindowsIdentity.GetCurrent().Name.Split('\\')[1];
        }
        
        /// <summary>
        /// Loads the managed organizational units (OUs) into a ListView.
        /// </summary>
        /// <param name="lvGroups">The ListView to populate with OUs.</param>        
        // Method to load managed organizational units (OUs) into a collection
        public static ObservableCollection<LvGroupsClass> LoadManagedOUsToCollection(ObservableCollection<LvGroupsClass> LvGroupsCollection)
        {
            // Check if the cached managed OUs are already populated
            if (_cachedManagedOUs != null && _cachedManagedOUs.Count > 0)
            {
                foreach (var item in _cachedManagedOUs)
                {
                    LvGroupsCollection.Add(item);
                }
                return LvGroupsCollection;
            }

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                throw new Exception("Debug Mode");
            }

            try
            {                
                string userDn = GetUserDistinguishedName(GetSamAccountName());

                if (string.IsNullOrEmpty(userDn))
                {
                    MessageBox.Show(Resources.msgUserCoulNotBeFound);
                    throw new Exception(Resources.msgUserCoulNotBeFound);
                }

                string domain = GetCurrentDomain();
                Debug.WriteLine($"selected domain: {domain}");
                // Search for all OUs managed by the user
                DirectoryEntry rootDSE = new("LDAP://" + domain);
                // Search for all OUs managed by the user
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
                    DirectoryEntry groupEntry = result.GetDirectoryEntry();

                    var memberDns = groupEntry.Properties["member"]
                                        .Cast<string>()
                                        .ToList();

                    var memberTypes = GetObjectClassesForMembers(memberDns);

                    var members = memberTypes.Select(kvp => new LdapMember
                    {
                        DistinguishedName = kvp.Key,
                        ObjectClass = kvp.Value
                    }).ToList();
                    members = members.OrderBy(m => m.DistinguishedName, StringComparer.OrdinalIgnoreCase).ToList();

                    var lvGroupsClass = new LvGroupsClass
                    {
                        Cn = groupEntry.Properties["cn"].Value as string ?? string.Empty,
                        DistinguishedName = groupEntry.Properties["distinguishedname"].Value as string ?? string.Empty,
                        Description = groupEntry.Properties["description"].Value as string ?? string.Empty,
                        Members = members
                    };

                    LvGroupsCollection.Add(lvGroupsClass);
                }

                if (LvGroupsCollection.Count == 0)
                {
                    var ldapMember = new LdapMember
                    {
                        DistinguishedName = "",
                        ObjectClass = "user"
                    };

                    LvGroupsClass emptylvGroupsClass = new()
                    {
                        Cn = "no Manager Role found",
                        DistinguishedName = "",
                        Description = "",
                        Members = [ldapMember] // Wrap the single LdapMember in a List
                    };
                    LvGroupsCollection.Add(emptylvGroupsClass);
                }                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Concat(Properties.Resources.txtError, ": ", ex.Message));
            }            

            //_cachedManagedOUs = new ObservableCollection<LvGroupsClass>(LvGroupsCollection);
            _cachedManagedOUs = new ObservableCollection<LvGroupsClass>(LvGroupsCollection.OrderBy(m => m.DistinguishedName, StringComparer.OrdinalIgnoreCase));
            return LvGroupsCollection;
        }

        public static Dictionary<string, string> GetObjectClassesForMembers(List<string> memberDns)
        {
            var result = new Dictionary<string, string>();

            if (memberDns == null || memberDns.Count == 0)
            {
                return result;
            }

            // LDAP-Filter mit (|(...)(...)) für alle DNs
            string filter = "(|";
            foreach (var dn in memberDns)
            {
                filter += $"(distinguishedName={EscapeLdapFilterValue(dn)})";
            }
            filter += ")";

            using (DirectorySearcher searcher = new())
            {
                searcher.Filter = filter;
                searcher.PropertiesToLoad.Add("distinguishedName");
                searcher.PropertiesToLoad.Add("objectClass");

                foreach (SearchResult sr in searcher.FindAll())
                {
                    //var entry = sr.GetDirectoryEntry();
                    var objectClasses = sr.Properties["objectClass"];
                    string type = "";

                    foreach (var cls in objectClasses)
                    {
                        var clsStr = cls?.ToString()?.ToLower();
                        if (clsStr == "user" || clsStr == "group")
                        {
                            type = clsStr;
                            break;
                        }
                    }

                    var dn = sr.Properties["distinguishedName"]?[0]?.ToString() ?? string.Empty;
                    result[dn] = type;
                }
            }

            return result;
        }


        // Method to load managed groups via membership into a collection
        public static ObservableCollection<LvGroupsClass> LoadManagedGroupsViaMembership(ObservableCollection<LvGroupsClass> LvGroupsCollection)
        {
            if (_cachedManagedGroups != null && _cachedManagedGroups.Count > 0)
            {
                foreach (var item in _cachedManagedGroups)
                {
                    LvGroupsCollection.Add(item);
                }
                return LvGroupsCollection;
            }

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
                DirectoryEntry rootDse = new("LDAP://" + domain)
                {
                    AuthenticationType = AuthenticationTypes.Secure
                };
                HashSet<string> addedDns = [];

                // Step 1: Get groups where user is a member
                List<string> userGroupDns = [];

                using DirectorySearcher memberSearcher = new(rootDse);
                memberSearcher.PropertiesToLoad.Clear();

                memberSearcher.Filter = $"(&(objectCategory=group)(member={userDn}))";
                memberSearcher.PropertiesToLoad.Add("distinguishedName");

                foreach (SearchResult result in memberSearcher.FindAll())
                {
                    if (result.Properties["distinguishedName"]?.Count > 0)
                    {
                        string groupDn = result.Properties.Contains("distinguishedName") && result.Properties["distinguishedName"].Count > 0
                                 ? result.Properties["distinguishedName"][0]?.ToString() ?? ""
                                 : "";
                        userGroupDns.Add(groupDn);

                        Debug.WriteLine($"Member of Group DN: {groupDn}");
                    }
                }


                // Step 2: For each group-DN, search for groups where managedBy = group-DN
                // Build an OR filter for all managerDns
                string combinedFilter = $"(|{string.Join("", userGroupDns.Select(dn => $"(managedBy={EscapeLdapFilterValue(dn)})"))})";

                using DirectorySearcher managerSearcher = new(rootDse);
                managerSearcher.PropertiesToLoad.Clear();
                managerSearcher.Filter = $"(&(objectCategory=group){combinedFilter})";

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

                    var memberDns = groupEntry.Properties["member"]
                        .Cast<string>()
                        .ToList();

                    var memberTypes = GetObjectClassesForMembers(memberDns);

                    var members = memberTypes.Select(kvp => new LdapMember
                    {
                        DistinguishedName = kvp.Key,
                        ObjectClass = kvp.Value
                    }).ToList();

                    LvGroupsClass group = new()
                    {
                        Cn = groupEntry.Properties["cn"].Value?.ToString() ?? "",
                        DistinguishedName = groupDn,
                        Description = groupEntry.Properties["description"].Value?.ToString() ?? "",
                        Members = members
                    };

                    Debug.WriteLine($"Adding group: {group.Cn} ({group.DistinguishedName})");

                    LvGroupsCollection.Add(group);
                    addedDns.Add(groupDn);
                }


                // Fallback if no groups found
                if (LvGroupsCollection.Count == 0)
                {
                    LvGroupsClass fallback = new()
                    {
                        Cn = Resources.msgNoIndirectlyManagedGroupsFound,
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

            var sorted = LvGroupsCollection.OrderBy(g => g.Cn).ToList();
            LvGroupsCollection = new ObservableCollection<LvGroupsClass>(sorted);
            _cachedManagedGroups = LvGroupsCollection;

            return LvGroupsCollection;
        }

        // Method to get the current domain
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
                    domain = $"{Domain.GetComputerDomain()}";
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    MessageBox.Show(Resources.txtNoConnectionToDomain, Resources.msgHeaderError, MessageBoxButton.OK, MessageBoxImage.Error);
                    return string.Empty;
                }
                catch (COMException)
                {
                    MessageBox.Show(Resources.msgNoPartOfADomain);
                    System.Windows.Application.Current.Shutdown();
                    return string.Empty;
                }
            }
            return domain;
        }
        public static string GetNetbiosDomain()
        {
            try
            {
                using var rootDseEntry = new DirectoryEntry("LDAP://RootDSE");

                if (rootDseEntry.Properties["configurationNamingContext"]?.Value is not string configNamingContext ||
                    rootDseEntry.Properties["defaultNamingContext"]?.Value is not string defaultNamingContext)
                {
                    return string.Empty;
                }

                using var partitionEntry = new DirectoryEntry($"LDAP://CN=Partitions,{configNamingContext}");
                using var searcher = new DirectorySearcher(partitionEntry)
                {
                    Filter = $"(nCName={defaultNamingContext})"
                };

                searcher.PropertiesToLoad.Add("nETBIOSName");

                var result = searcher.FindOne();
                if (result?.Properties["nETBIOSName"]?.Count > 0)
                {
                    return result.Properties["nETBIOSName"][0] as string ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error retrieving NetBIOS domain name: " + ex.Message);
            }

            return string.Empty;
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

        // Method to resolve the SAM account name from a distinguished name (DN)
        public static string ResolveSamAccountNameFromDn(string dn)
        {
            using DirectoryEntry userEntry = new($"LDAP://{dn}");
            string? samAccount = userEntry.Properties["sAMAccountName"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(samAccount))
            {
                throw new Exception("sAMAccountName not found.");
            }

            string domain = GetCurrentDomain(); // should return the domain name
            return $"{domain}\\{samAccount}";
        }

        /// <summary>
        /// Gets the distinguished name of a user from Active Directory.
        /// </summary>
        /// <param name="samAccountName">The SAM account name of the user.</param>
        /// <param name="settings">The application settings.</param>
        /// <returns>The distinguished name of the user, or null if not found.</returns>
        // Method to get the distinguished name of a user from Active Directory
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
        /// <returns>Name</returns>
        // Method to extract the name from a common name (CN)
        public static string GetNameFromCN(string CN)
        {
            if (string.IsNullOrWhiteSpace(CN))
            {
                return "";
            }

            var match = Regex.Match(CN, @"CN=([^,]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : CN;
        }
        public static string GetNameFromCN(LdapMember member)
        {
            // Extrahiere den CN aus dem Distinguished Name
            var dn = member.DistinguishedName;
            if (string.IsNullOrWhiteSpace(dn))
            {
                return "";
            }

            return GetNameFromCN(dn);
        }

        public static string GetObjectClass(string distinguishedName)
        {
            using (var entry = new DirectoryEntry($"LDAP://{distinguishedName}"))
            {
                var objectClasses = entry.Properties["objectClass"];
                foreach (var cls in objectClasses)
                {
                    var clsStr = cls?.ToString()?.ToLower();
                    if (clsStr == "user" || clsStr == "group")
                    {
                        return clsStr;
                    }
                }
            }
            return "unknown";
        }

        public static string GetCNFromUsername()
        {
            var samAccountName = GetSamAccountName(); // Ensure the current user's SAM account name is set
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
                    return result.Properties["cn"][0].ToString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving CN for {samAccountName}: {ex.Message}");
                return string.Empty;
            }
            return string.Empty;
        }

        // Method to search for users in Active Directory
        public static List<LdapSearchResult> SearchUsers(string searchTerm)
        {
            searchTerm = EscapeLdapFilterValue(searchTerm); // Escape special characters in the search term
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
            searcher.PropertiesToLoad.Add("sAMAccountName");
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
                        SamAccountName = result.Properties.Contains("sAMAccountName") && result.Properties["sAMAccountName"].Count > 0
                                         ? result.Properties["sAMAccountName"][0]?.ToString() ?? ""
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
        // Method to get the groups a user belongs to from Active Directory
        public static List<LdapSearchResult> GetMyGroupsFromLdap(string userDn)
        {
            userDn = EscapeLdapFilterValue(userDn); // Escape special characters in the DN
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
                    SamAccountName = result.Properties.Contains("sAMAccountName")
                        ? result.Properties["sAMAccountName"]?[0]?.ToString() ?? ""
                        : "",
                    DisplayName = result.Properties.Contains("description")
                        ? result.Properties["description"]?[0]?.ToString() ?? ""
                        : "",
                    ObjectClass = "group"
                };

                groups.Add(ldapResult);
            }

            return groups;
        }

        public static string EscapeLdapFilterValue(string value)
        {
            return value
                .Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("\0", "\\00");
        }

    }
}