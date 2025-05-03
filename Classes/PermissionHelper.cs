//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace GM4Manager.Classes
//{
//    using System.Security.AccessControl;
//    using System.Security.Principal;
//    using System.IO;
//    using System.Linq;
//    using System.Windows.Forms;
//    using System.Collections.Generic;

//    public static class PermissionHelper
//    {
//        public static void LoadNtfsPermissionsIfReadable(string folderPath, DataGridView dgvPermissions)
//        {
//            try
//            {
//                // Set cursor to wait mode
//                Cursor.Current = Cursors.WaitCursor;
//                // Prüfen, ob Benutzer überhaupt Leserechte auf den Ordner hat
//                if (!UserHasReadAccess(folderPath)) return;

//                // DataGridView leeren
//                dgvPermissions.Rows.Clear();

//                // Benutzergruppen ermitteln
//                List<string> userGroups = GetCurrentUserGroups();

//                // Sicherheitsinformationen laden
//                DirectoryInfo dirInfo = new(folderPath);
//                DirectorySecurity dirSecurity = dirInfo.GetAccessControl();
//                AuthorizationRuleCollection rules = dirSecurity.GetAccessRules(true, true, typeof(NTAccount));

//                foreach (FileSystemAccessRule rule in rules)
//                {
//                    string identity = rule.IdentityReference.Value;

//                    // Nur Gruppen anzeigen, in denen der Benutzer Mitglied ist
//                    if (userGroups.Contains(identity, StringComparer.OrdinalIgnoreCase))
//                    {
//                        if (dgvPermissions.InvokeRequired)
//                        {
//                            dgvPermissions.Invoke((MethodInvoker)(() =>
//                            {
//                                int rowIndex = dgvPermissions.Rows.Add();
//                                dgvPermissions.Rows[rowIndex].Cells["User"].Value = identity;
//                                dgvPermissions.Rows[rowIndex].Cells["Permissions"].Value = rule.FileSystemRights.ToString();
//                                dgvPermissions.Rows[rowIndex].Cells["AccessControlType"].Value = rule.AccessControlType.ToString();
//                            }));
//                        }
//                        else
//                        {
//                            int rowIndex = dgvPermissions.Rows.Add();
//                            dgvPermissions.Rows[rowIndex].Cells["User"].Value = identity;
//                            dgvPermissions.Rows[rowIndex].Cells["Permissions"].Value = rule.FileSystemRights.ToString();
//                            dgvPermissions.Rows[rowIndex].Cells["AccessControlType"].Value = rule.AccessControlType.ToString();
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Fehler beim Laden der Berechtigungen: {ex.Message}");
//            }
//            finally
//            {
//                // Restore default cursor
//                Cursor.Current = Cursors.Default;
//            }        
//        }

//        private static bool UserHasReadAccess(string folderPath)
//        {
//            try
//            {
//                // Versuche, Inhalte zu lesen (z. B. ob es überhaupt Unterverzeichnisse gibt)
//                Directory.GetDirectories(folderPath);
//                return true;
//            }
//            catch (UnauthorizedAccessException)
//            {
//                return false;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        private static List<string> GetCurrentUserGroups()
//        {
//            WindowsIdentity identity = WindowsIdentity.GetCurrent();
//            List<string> groupNames = new();

//            foreach (IdentityReference group in identity.Groups)
//            {
//                try
//                {
//                    groupNames.Add(group.Translate(typeof(NTAccount)).Value);
//                }
//                catch
//                {
//                    // Ignore unresolved SIDs
//                }
//            }

//            // Eigener Benutzername zählt auch
//            groupNames.Add(identity.Name);

//            return groupNames;
//        }
//    }

//}
