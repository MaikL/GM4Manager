//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Diagnostics;
//using System.DirectoryServices;
//using System.Linq;
//using System.Reflection.Emit;
//using System.Security.AccessControl;
//using System.Security.Principal;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using System.Xml.Linq;

//namespace GM4Manager.Classes
//{
//    internal class NetworkService
//    {
//        public static bool CheckUserGroups { get; set; } = false;
//        /// <summary>
//        /// Asynchronously loads the network drive and populates the TreeView with directories.
//        /// </summary>
//        /// <param name="rootPath">The root path of the network drive.</param>
//        /// <param name="treeViewFolders">The TreeView control to populate with directories.</param>
//        /// <param name="progressBar">The ProgressBar control to indicate loading progress.</param>
//        public static void LoadNetworkDrive(string rootPath, TreeView treeViewFolders)
//        {
//            // Clear existing nodes in the TreeView
//            treeViewFolders.Nodes.Clear();

//            // Check if the root path exists
//            if (!Directory.Exists(rootPath))
//            {
//                MessageBox.Show(Ressources.txtPathNotFound);
//                return;
//            }

//            // Create the root node for the TreeView
//            TreeNode rootNode = new(Path.GetFileName(rootPath))
//            {
//                Tag = rootPath
//            };

//            // Add a dummy node for lazy loading
//            //rootNode.Nodes.Add("Loading...");
//            treeViewFolders.Nodes.Add(rootNode);

//            // Jetzt echte Subverzeichnisse laden
//            rootNode.Nodes.Clear();
//            LoadSubDirectoriesLazy(rootPath, rootNode);

//            // Automatisch ausklappen
//            rootNode.Expand();
//            treeViewFolders.SelectedNode = rootNode;
//            rootNode.EnsureVisible();        
//        }

//        /// <summary>
//        /// Lazily loads subdirectories for the given path and adds them to the parent node.
//        /// </summary>
//        /// <param name="path">The path of the directory to load subdirectories from.</param>
//        /// <param name="parentNode">The parent TreeNode to add subdirectories to.</param>
//        public static void LoadSubDirectoriesLazy(string path, TreeNode parentNode)
//        {
//            try
//            {
//                // Get all subdirectories for the given path
//                foreach (string dir in Directory.GetDirectories(path))
//                {
//                    //TreeNode subNode = new(Path.GetFileName(dir));
//                    TreeNode subNode = CheckForPermissions(dir);
//                    if (subNode != null)
//                    {
//                        parentNode.Nodes.Add(subNode);
//                    }
//                }
//            }
//            catch (UnauthorizedAccessException)
//            {
//                // Ignore unauthorized access exceptions
//            }
//            catch (Exception ex)
//            {
//                // Show an error message if an exception occurs
//                MessageBox.Show($"Error loading subdirectories: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Handles the BeforeExpand event of the TreeView to load subdirectories lazily.
//        /// </summary>
//        /// <param name="sender">The source of the event.</param>
//        /// <param name="e">A TreeViewCancelEventArgs that contains the event data.</param>
//        public static void TreeViewFolders_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
//        {
//            if (e.Node == null)
//            { return; }
//            TreeNode node = e.Node;

//            // If the node has a dummy child node, load the subdirectories
//            if (node.Nodes.Count == 1 && node.Nodes[0].Text == "Loading...")
//            {
//                node.Nodes.Clear();


//                try
//                {
//                    // Get all subdirectories for the node's path
//                    if (node.Tag is string path)
//                    {
//                        foreach (string dir in Directory.GetDirectories(path))
//                        {
//                            TreeNode subNode = CheckForPermissions(dir);
//                            if (subNode != null)
//                            {
//                                node.Nodes.Add(subNode);
//                            }
//                        }
//                    }
//                }

//                catch (UnauthorizedAccessException)
//                {
//                    // Ignore unauthorized access exceptions
//                }
//                catch (Exception ex)
//                {
//                    // Show an error message if an exception occurs
//                    MessageBox.Show($"Error expanding node: {ex.Message}");
//                }
//            }
//        }

//        private static TreeNode CreateSubNode(string dir)
//        {
//            TreeNode subNode = new(Path.GetFileName(dir))
//            {
//                Tag = dir
//            };

//            // Add a dummy node for lazy loading
//            subNode.Nodes.Add("Loading...");
//            return subNode;
//        }

//        private static TreeNode CheckForPermissions(string dir)
//        {
//            try
//            {                               
//                if (CheckUserGroups)
//                {
//                    DirectoryInfo dirInfo = new(dir);
//                    DirectorySecurity dirSecurity = dirInfo.GetAccessControl();

//                    AuthorizationRuleCollection rules = dirSecurity.GetAccessRules(true, true, typeof(NTAccount));

//                    // Add Permissions
//                    foreach (FileSystemAccessRule rule in rules)
//                    {
//                        string identity = rule.IdentityReference.Value;

//                        // Check if current user is in the group if the flag is set

//                        if (UserContext.UserGroups.Any(group => string.Equals(group, identity, StringComparison.OrdinalIgnoreCase)))
//                        {
//                            Debug.WriteLine($"Matched Permission: {identity} => {rule.FileSystemRights} for path: {dir}");
//                            return CreateSubNode(dir);
//                        }
//                    }
//                }
//                else
//                {
//                    // If not checking user groups, just create the node
//                    return CreateSubNode(dir);
//                }
//            }
//            catch (System.UnauthorizedAccessException unauthorized)
//            {
//                Debug.WriteLine(unauthorized.Message);
//            }
//            catch (Exception ex)
//            {
//                // MessageBox.Show($"Fehler beim Laden der Berechtigungen: {ex.Message}");
//                // ignoring reading errors
//            }
//            return null; // Return null if no matching permissions are found
//        }

//        public static void LoadNtfsPermissionsToDataGridView(string folderPath, DataGridView dgvPermissions)
//        {
//            // Empty DataGridView on Startup
//            dgvPermissions.Rows.Clear();

//            try
//            {
//                DirectoryInfo dirInfo = new(folderPath);
//                DirectorySecurity dirSecurity = dirInfo.GetAccessControl();

//                AuthorizationRuleCollection rules = dirSecurity.GetAccessRules(true, true, typeof(NTAccount));

//                // Add Permissions
//                foreach (FileSystemAccessRule rule in rules)
//                {
//                    string identity = rule.IdentityReference.Value;

//                    // Check if current user is in the group
//                    if (!UserContext.UserGroups.Any(group => string.Equals(group, identity, StringComparison.OrdinalIgnoreCase)))
//                    {
//                        continue;
//                    }

//                    // secure UI-thread 
//                    if (dgvPermissions.InvokeRequired)
//                    {
//                        dgvPermissions.Invoke((MethodInvoker)(() =>
//                        {
//                            AddValuesToDataGridView(dgvPermissions, identity, rule);
//                        }));
//                    }
//                    else
//                    {
//                        AddValuesToDataGridView(dgvPermissions, identity, rule);
//                    }

//                    Debug.WriteLine($"Matched Permission: {identity} => {rule.FileSystemRights}");
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Fehler beim Laden der Berechtigungen: {ex.Message}");
//            }
//        }

//        private static void AddValuesToDataGridView(DataGridView dgvPermissions, string identity, FileSystemAccessRule rule)
//        {
//            int rowIndex = dgvPermissions.Rows.Add();
//            dgvPermissions.Rows[rowIndex].Cells["User"].Value = identity;
//            dgvPermissions.Rows[rowIndex].Cells["Permissions"].Value = rule.FileSystemRights.ToString();
//            dgvPermissions.Rows[rowIndex].Cells["AccessControlType"].Value = rule.AccessControlType.ToString();          
//        }
//    }
//}