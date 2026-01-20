using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Models;
using GM4ManagerWPF.Properties;
using Microsoft.VisualBasic.ApplicationServices;
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
        public static ResourceService Res => ResourceService.Instance;

        public static string GetSamAccountName () {
            return WindowsIdentity.GetCurrent().Name.Split('\\')[1];
        }
                
        public static ObservableCollection<LvGroupsClass> GetMembersForGroupFromLdap(string filter, ObservableCollection<LvGroupsClass> LvGroupsCollection)
        {            
            string domain = GetCurrentDomain();
            Debug.WriteLine($"selected domain: {domain}");
            // Search for all OUs managed by the user
            DirectoryEntry rootDSE = new("LDAP://" + domain);
            // Search for all OUs managed by the user
            DirectorySearcher searcher = new(rootDSE)
            {
                Filter = filter
            };
            searcher.PropertiesToLoad.Add("cn");
            searcher.PropertiesToLoad.Add("distinguishedname");
            searcher.PropertiesToLoad.Add("description");
            searcher.PropertiesToLoad.Add("member");
            searcher.PropertiesToLoad.Add("objectClass");

            Debug.WriteLine($"Number of managed roles found: {searcher.FindAll().Count}");
            foreach (SearchResult result in searcher.FindAll())
            {
                DirectoryEntry groupEntry = result.GetDirectoryEntry();

                var memberDns = groupEntry.Properties["member"]
                                    .Cast<string>()
                                    .ToList();

                var members = GetObjectClassesForMembers(memberDns);               
                Debug.WriteLine($"Found group: {groupEntry.Properties["cn"].Value} ({groupEntry.Properties["distinguishedname"].Value}) - Members: {members.Count}");
                var lvGroupsClass = new LvGroupsClass
                {
                    Cn = groupEntry.Properties["cn"].Value as string ?? string.Empty,
                    DistinguishedName = groupEntry.Properties["distinguishedname"].Value as string ?? string.Empty,
                    Description = groupEntry.Properties["description"].Value as string ?? string.Empty,
                    Members = members
                };
                Debug.WriteLine($"Adding group: {lvGroupsClass.Cn} ({lvGroupsClass.DistinguishedName}) with {lvGroupsClass.Members.Count} members");

                LvGroupsCollection.Add(lvGroupsClass);                
            }

            if (LvGroupsCollection.Count == 0)
            {                
                LvGroupsClass emptylvGroupsClass = new()
                {
                    Cn = "no Manager Role found",
                    DistinguishedName = "",
                    Description = "",
                    Members = [] // Wrap the single LdapMember in a List
                };
                LvGroupsCollection.Add(emptylvGroupsClass);
            }

            // sort the collection by Cn
            LvGroupsCollection = new ObservableCollection<LvGroupsClass>(
                LvGroupsCollection
                    .OrderBy(g => g.Cn, StringComparer.OrdinalIgnoreCase)
            );

            return LvGroupsCollection;
        }

        public static List<LdapSearchResult> GetObjectClassesForMembers(List<string> memberDns)
        {
            var result = new List<LdapSearchResult>();

            if (memberDns == null || memberDns.Count == 0)
            {
                return result;
            }

            // LDAP-Filter with (|(...)(...)) for all DNs in one query
            string filter = "(|";
            foreach (var dn in memberDns)
            {
                string cnValue = GetNameFromCN(dn);
                filter += $"(cn={EscapeLdapFilterValue(cnValue)})";
            }
            filter += ")";
            Debug.WriteLine($"LDAP Filter: {filter}");

            using (DirectorySearcher searcher = new())
            {
                searcher.Filter = filter;
                searcher.PropertiesToLoad.Add("distinguishedName");
                searcher.PropertiesToLoad.Add("cn");
                searcher.PropertiesToLoad.Add("description");
                searcher.PropertiesToLoad.Add("sAMAccountName");
                searcher.PropertiesToLoad.Add("objectClass");

                foreach (SearchResult sr in searcher.FindAll())
                {
                    Debug.WriteLine($"Processing SearchResult: {sr.Path} {sr.Properties["cn"]?[0]?.ToString()}");
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
                    Debug.WriteLine($"ADS - ObjectClass found: {type}");

                    var dn = sr.Properties["distinguishedName"]?.Count > 0 ? sr.Properties["distinguishedName"][0]?.ToString() : string.Empty;
                    var cn = sr.Properties["cn"]?.Count > 0 ? sr.Properties["cn"][0]?.ToString() : string.Empty;
                    var description = sr.Properties["description"]?.Count > 0 ? sr.Properties["description"][0]?.ToString() : string.Empty;
                    var samaccountname = sr.Properties["sAMAccountName"]?.Count > 0 ? sr.Properties["sAMAccountName"][0]?.ToString() : string.Empty;
                    try
                    {
                        var ldap = new LdapSearchResult
                        {
                            DistinguishedName = dn ?? string.Empty,
                            CommonName = cn ?? string.Empty,
                            DisplayName = description ?? string.Empty,
                            SamAccountName = samaccountname ?? string.Empty,
                            ObjectClass = type
                        };
                        result.Add(ldap);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error creating LdapSearchResult: {ex.Message}");
                        Debug.WriteLine($"DN: {dn}, CN: {cn}, Description: {description}, SAM: {samaccountname}, Type: {type}");
                    }
                }
                Debug.WriteLine($"Number of members found: {result.Count}");
                Debug.WriteLine($"Members: {string.Join(", ", result.Select(m => m.CommonName))}");                
            }

            return result.OrderBy(m => m.CommonName, StringComparer.OrdinalIgnoreCase).ToList();
        }


        // Method to load managed groups via membership into a collection
        public static ObservableCollection<LvGroupsClass> LoadManagedGroupsViaMembership(ObservableCollection<LvGroupsClass> LvGroupsCollection)
        {
            Debug.WriteLine("Loading managed groups via membership...");
            if (_cachedManagedGroups != null && _cachedManagedGroups.Count > 0)
            {
                foreach (var item in _cachedManagedGroups)
                {
                    LvGroupsCollection.Add(item);
                }
                Debug.WriteLine($"Returning cached managed groups. Count: {_cachedManagedGroups.Count}");
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

                        Debug.WriteLine($"ActiveDirectoryService - Member of Group DN: {groupDn} - LoadManagedGroupsViaMembership");
                    }
                }

                // Step 2: For each group-DN, search for groups where managedBy = group-DN
                // Build an OR filter for all managerDns
                string combinedFilter = $"(|{string.Join("", userGroupDns.Select(dn => $"(managedBy={EscapeLdapFilterValue(dn)})"))})";
                Debug.WriteLine($"Combined LDAP Filter: {combinedFilter}");
                using DirectorySearcher managerSearcher = new(rootDse);
                managerSearcher.PropertiesToLoad.Clear();
                managerSearcher.Filter = $"(&(objectCategory=group){combinedFilter})";

                managerSearcher.PropertiesToLoad.Add("cn");
                managerSearcher.PropertiesToLoad.Add("distinguishedname");
                managerSearcher.PropertiesToLoad.Add("description");
                //managerSearcher.PropertiesToLoad.Add("member");

                var searcherResult = managerSearcher.FindAll();
                Debug.WriteLine($"Number of managed groups found: {searcherResult.Count}");
                foreach (SearchResult sr in searcherResult)
                {
                    //DirectoryEntry groupEntry = result.GetDirectoryEntry();
                    
                    //string groupDn = groupEntry.Properties["distinguishedname"]?.ToString() ?? "";
                    string? groupDn = sr.Properties["distinguishedName"]?.Count > 0 ? sr.Properties["distinguishedName"][0]?.ToString() ?? string.Empty : string.Empty;

                    // Avoid duplicates
                    if (addedDns.Contains(groupDn))
                    {
                        Debug.WriteLine($"Skipping already added group: {groupDn}");
                        continue;
                    }
                    
                    LvGroupsClass group = new()
                    {
                        Cn = sr.Properties["cn"]?.Count > 0 ? sr.Properties["cn"][0]?.ToString() ?? string.Empty : string.Empty,
                        DistinguishedName = groupDn,
                        Description = sr.Properties["description"]?.Count > 0 ? sr.Properties["description"][0]?.ToString() : string.Empty,
                        Members = []
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
                        Members = [],
                        IsPlaceholder = true
                    };
                    LvGroupsCollection.Add(fallback);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
            }
            Debug.WriteLine($"Number of managed groups found: {LvGroupsCollection.Count}");

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
                Filter = $"(&" +
         "(!objectCategory=computer)" + // Exclude computers
         "(|" +
             "(objectCategory=person)" +
             "(objectCategory=group)" +
         ")" +
         "(|" +
             $"(cn=*{searchTerm}*)" +
             $"(displayName=*{searchTerm}*)" +
             $"(sAMAccountName=*{searchTerm}*)" +
         // $"(description=*{searchTerm}*)" + possibility to also search through description
         ")" +
         "(!(userAccountControl:1.2.840.113556.1.4.803:=2))" + // Exclude disabled accounts
         ")",

                PageSize = 50
            };

            searcher.PropertiesToLoad.Add("distinguishedName");
            searcher.PropertiesToLoad.Add("cn");
            searcher.PropertiesToLoad.Add("sAMAccountName");
            searcher.PropertiesToLoad.Add("displayName");
            searcher.PropertiesToLoad.Add("objectClass");
            searcher.PropertiesToLoad.Add("Description");

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
                        Description = result.Properties.Contains("Description") && result.Properties["Description"].Count > 0
                                         ? result.Properties["Description"][0]?.ToString() ?? ""
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


            return results
                .OrderBy(r => r.CommonName, StringComparer.OrdinalIgnoreCase)
                .ToList();
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
                Debug.WriteLine($"Found group: {ldapResult.CommonName} ({ldapResult.DistinguishedName})");
                groups.Add(ldapResult);
            }

            return groups;
        }

        public static async Task<List<string>> GetUserGroupsForCurrentUserAsync(Action<string>? reportStatus = null)
        {
            return await Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                var identity = WindowsIdentity.GetCurrent();
                if (identity == null || identity.Groups == null)
                {
                    return new List<string>();
                }

                var sidList = identity.Groups.Select(g => g.Value).ToList();
                reportStatus?.Invoke($"Lade {sidList.Count} Gruppen-SIDs...");

                // LDAP-Filter bauen
                string filter = "(|" + string.Join("", sidList.Select(sid => $"(objectSid={BuildOctetString(sid)})")) + ")";

                var groupNames = new List<string>();
                try
                {
                    string domain = GetCurrentDomain();
                    using var entry = new DirectoryEntry("LDAP://" + domain);
                    //string domainPath = "LDAP://" + entry.Properties["defaultNamingContext"].Value;
                    //using var searchRoot = new DirectoryEntry(entry);
                    using var searcher = new DirectorySearcher(entry, filter);
                    searcher.PageSize = 1000; // Set page size for large result sets
                    searcher.PropertiesToLoad.Add("name");
                    var results = searcher.FindAll();
                    foreach (SearchResult result in results)
                    {
                        if (result.Properties["name"].Count > 0)
                        {
                            var name = result.Properties["name"][0].ToString();
                            groupNames.Add(name!);
                            reportStatus?.Invoke(Resources.loadGroup.Replace("{name}", name));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Laden der Gruppen: {ex.Message}");
                    reportStatus?.Invoke($"Fehler beim Laden der Gruppen: {ex.Message}");
                }

                sw.Stop();
                Debug.WriteLine($"Gruppenauflösung dauerte: {sw.ElapsedMilliseconds} ms");
                return groupNames;
            });
        }       

        // Hilfsfunktion: SID in LDAP-kompatibles Octet-String-Format umwandeln
        private static string BuildOctetString(string sid)
        {
            // Create a SecurityIdentifier object from the SID string
            var securityIdentifier = new SecurityIdentifier(sid);

            // Allocate a byte array to hold the binary form of the SID
            byte[] sidBytes = new byte[securityIdentifier.BinaryLength];

            // Populate the byte array with the binary form of the SID
            securityIdentifier.GetBinaryForm(sidBytes, 0);

            // Convert the byte array to an LDAP-compatible octet string
            return "\\" + BitConverter.ToString(sidBytes).Replace("-", "\\");
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