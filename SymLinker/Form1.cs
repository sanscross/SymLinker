using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace SymLinker
{
    public partial class Form1 : Form
    {
        // Lists for first set of controls
        private List<string> selectedFilePaths1 = new List<string>();
        private List<string> selectedDirectoryPaths1 = new List<string>();

        // Single selected folder for second set of controls
        private string destinationFolder = null;

        public Form1()
        {
            InitializeComponent();
            InitializeFirstSet();
            PopulateTreeView(treeView2);

            // Initialize button as disabled
            button1.Enabled = false;
        }

        private void InitializeFirstSet()
        {
            PopulateTreeView(treeView1);
            this.treeView1.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
            this.treeView1.BeforeExpand += new TreeViewCancelEventHandler(this.treeView1_BeforeExpand);
            this.listView1.ItemChecked += new ItemCheckedEventHandler(this.listView1_ItemChecked);
        }
        private void PopulateTreeView(TreeView treeView)
        {
            // Disable checkboxes for the tree view (only use them in list view)
            treeView.CheckBoxes = false;

            // Get all drives on the system
            DriveInfo[] drives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in drives)
            {
                try
                {
                    // Only adds drives that are ready (avoid CD-ROM drives without discs, etc.)
                    if (drive.IsReady)
                    {
                        TreeNode driveNode = new TreeNode(drive.Name);
                        driveNode.Tag = drive;
                        driveNode.ImageKey = "folder";

                        //Placeholder
                        driveNode.Nodes.Add(new TreeNode("Loading..."));

                        treeView.Nodes.Add(driveNode);
                    }
                }
                catch (IOException)
                {
                }
            }
        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            ProcessTreeViewBeforeExpand(treeView1, listView1, e);
        }
        private void treeView2_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            ProcessTreeViewBeforeExpand(treeView2, null, e);
        }
        private void ProcessTreeViewBeforeExpand(TreeView treeView, ListView listView, TreeViewCancelEventArgs e)
        {
            TreeNode node = e.Node;

            // Checks if this node has the placeholder "Loading..."
            if (node.Nodes.Count == 1 && node.Nodes[0].Text == "Loading...")
            {
                // Removes the placeholder
                node.Nodes.Clear();

                try
                {
                    if (node.Tag is DriveInfo driveInfo)
                    {
                        // Load subdirectories for drive
                        DirectoryInfo rootDir = new DirectoryInfo(driveInfo.RootDirectory.FullName);
                        DirectoryInfo[] subDirs = rootDir.GetDirectories("*", SearchOption.TopDirectoryOnly);
                        AddDirectoriesToNode(subDirs, node);
                    }
                    else if (node.Tag is DirectoryInfo dirInfo)
                    {
                        // Load subdirectories for directory
                        DirectoryInfo[] subDirs = dirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);
                        AddDirectoriesToNode(subDirs, node);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // just leaves empty directory
                }
                catch (Exception)
                {
                }
            }
        }

        private void AddDirectoriesToNode(DirectoryInfo[] subDirs, TreeNode parentNode)
        {
            foreach (DirectoryInfo subDir in subDirs)
            {
                try
                {
                    // Show ALL directories including hidden and system ones
                    TreeNode childNode = new TreeNode(subDir.Name);
                    childNode.Tag = subDir;
                    childNode.ImageKey = "folder";

                    // Add a placeholder to make it expandable
                    childNode.Nodes.Add(new TreeNode("Loading..."));

                    parentNode.Nodes.Add(childNode);
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip directories that we don't have access to
                    continue;
                }
                catch (DirectoryNotFoundException)
                {
                    // Skip directories that no longer exist
                    continue;
                }
            }
        }

        void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            ProcessNodeMouseClick(treeView1, listView1, e);
        }
        private void treeView2_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
           
        }
        private void treeView2_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.FullPath is string path)
            {
                destinationFolder = path;
                UpdateLabels();

                UpdateButtonState();
            }

        }
        private void ProcessNodeMouseClick(TreeView treeView, ListView listView, TreeNodeMouseClickEventArgs e)
        {
            TreeNode newSelected = e.Node;

            if (listView != null) listView.Items.Clear();
           
            try
            {
                // Check if the node represents a drive
                if (newSelected.Tag is DriveInfo driveInfo)
                {
                    // If it's a drive, use its root directory
                    DirectoryInfo nodeDirInfo = new DirectoryInfo(driveInfo.RootDirectory.FullName);
                    PopulateListView(treeView, listView, nodeDirInfo);
                }
                else if (newSelected.Tag is DirectoryInfo dirInfo)
                {
                    // If it's a directory, use it directly
                    PopulateListView(treeView, listView, dirInfo);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Access denied to this directory.", "Permission Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateListView(TreeView treeView, ListView listView, DirectoryInfo nodeDirInfo)
        {
            ListViewItem.ListViewSubItem[] subItems;
            ListViewItem listViewItem = null;

            // Add ALL directories including hidden/system ones
            foreach (DirectoryInfo dir in nodeDirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    // Check if this is a symbolic link
                    if ((dir.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        continue; // Skip symbolic links
                    }

                    listViewItem = new ListViewItem(dir.Name, 0);

                    // Add visual indicator for hidden/system directories
                    string type = "Directory";
                    if ((dir.Attributes & FileAttributes.Hidden) != 0)
                        type += " (Hidden)";
                    if ((dir.Attributes & FileAttributes.System) != 0)
                        type += " (System)";

                    subItems = new ListViewItem.ListViewSubItem[]
                    {
                         new ListViewItem.ListViewSubItem(listViewItem, type),
                         new ListViewItem.ListViewSubItem(listViewItem, dir.LastAccessTime.ToShortDateString()),
                    };
                    listViewItem.SubItems.AddRange(subItems);
                    listViewItem.Tag = dir.FullName; // Store the full path

                    // Set checkbox state based on stored paths
                    if (listView == listView1)
                    {
                        listViewItem.Checked = selectedDirectoryPaths1.Contains(dir.FullName);
                    }

                    if(listView != null)listView.Items.Add(listViewItem);
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip directories we can't access
                    continue;
                }
            }

            // Add ALL files including hidden/system ones
            foreach (FileInfo file in nodeDirInfo.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    // Check if this is a symbolic link
                    if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        continue; // Skip symbolic links
                    }

                    listViewItem = new ListViewItem(file.Name, 1);

                    // Add visual indicator for hidden/system files
                    string type = "File";
                    if ((file.Attributes & FileAttributes.Hidden) != 0)
                        type += " (Hidden)";
                    if ((file.Attributes & FileAttributes.System) != 0)
                        type += " (System)";

                    subItems = new ListViewItem.ListViewSubItem[]
                    {
                new ListViewItem.ListViewSubItem(listViewItem, type),
                new ListViewItem.ListViewSubItem(listViewItem, file.LastAccessTime.ToShortDateString()),
                    };
                    listViewItem.SubItems.AddRange(subItems);
                    listViewItem.Tag = file.FullName; // Store the full path

                    // Set checkbox state based on stored paths
                    if (listView == listView1)
                    {
                        listViewItem.Checked = selectedFilePaths1.Contains(file.FullName);
                    }

                    if (listView != null) listView.Items.Add(listViewItem);
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip files we can't access
                    continue;
                }
            }

            if (listView != null) listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }
        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Tag is string path)
            {
                if (e.Item.Checked)
                {
                    // Check if it's a file or directory based on the image index
                    if (e.Item.ImageIndex == 1) // File
                    {
                        // Add file path if it's not already in the list
                        if (!selectedFilePaths1.Contains(path))
                        {
                            selectedFilePaths1.Add(path);
                        }
                    }
                    else // Directory (ImageIndex 0)
                    {
                        // Add directory path if it's not already in the list
                        if (!selectedDirectoryPaths1.Contains(path))
                        {
                            selectedDirectoryPaths1.Add(path);
                        }
                    }
                }
                else
                {
                    // Remove from the appropriate list
                    if (e.Item.ImageIndex == 1) // File
                    {
                        selectedFilePaths1.Remove(path);
                    }
                    else // Directory
                    {
                        selectedDirectoryPaths1.Remove(path);
                    }
                }
            }

            // Update rich text box and labels after selection changes
            UpdateRichTextBox();
            UpdateLabels();

            // Update button state
            UpdateButtonState();
        }

        // Checks if some files and destination folder selected, if that's true, then it activates the button
        void UpdateButtonState()
        {
            button1.Enabled = (selectedFilePaths1.Count > 0 || selectedDirectoryPaths1.Count > 0) &&
                 !string.IsNullOrEmpty(destinationFolder);
        }
        private void UpdateRichTextBox()
        {
            richTextBox1.Clear();

            // Adds selected files
            if (selectedFilePaths1.Count > 0)
            {
                richTextBox1.AppendText("Selected Files:\n");
                richTextBox1.SelectionFont = new System.Drawing.Font(richTextBox1.Font, System.Drawing.FontStyle.Bold);
                richTextBox1.AppendText("==================\n");
                richTextBox1.SelectionFont = richTextBox1.Font;

                foreach (string filePath in selectedFilePaths1)
                {
                    richTextBox1.AppendText(filePath + "\n");
                }
                richTextBox1.AppendText("\n");
            }

            // Adds selected directories
            if (selectedDirectoryPaths1.Count > 0)
            {
                richTextBox1.AppendText("Selected Directories:\n");
                richTextBox1.SelectionFont = new System.Drawing.Font(richTextBox1.Font, System.Drawing.FontStyle.Bold);
                richTextBox1.AppendText("==================\n");
                richTextBox1.SelectionFont = richTextBox1.Font;

                foreach (string dirPath in selectedDirectoryPaths1)
                {
                    richTextBox1.AppendText(dirPath + "\n");
                }
                richTextBox1.AppendText("\n");
            }

            // If nothing is selected
            if (selectedFilePaths1.Count == 0 && selectedDirectoryPaths1.Count == 0)
            {
                richTextBox1.AppendText("No files or directories selected.");
            }
        }

        private void UpdateLabels()
        {
            // Update pathLabel with the selected folder from listView2
            if (!string.IsNullOrEmpty(destinationFolder))
            {
                pathLabel.Text = "Path: " + destinationFolder;
            }
            else
            {
                pathLabel.Text = "Path: No folder selected";
            }

            // Update folderLabel with count of selected directories in listView1
            folderLabel.Text = "Folders selected: " + selectedDirectoryPaths1.Count;

            // Update fileLabel with count of selected files in listView1
            fileLabel.Text = "Files selected: " + selectedFilePaths1.Count;
        }
        //this is where the Form2 spawning happens
        private void button1_Click_1(object sender, EventArgs e)
        {
            if (button1.Enabled)
            {
                // Create Form2 and pass the selected paths
                Form2 form2 = new Form2(
                    new List<string>(selectedFilePaths1),
                    new List<string>(selectedDirectoryPaths1),
                    destinationFolder
                );

                // Clear selections in Form1 before showing Form2
                ClearAllSelections1();
                ClearSelection2();

                form2.Show(); // Show Form2 as dialog
            }
        }

        // Methods for first set of controls
        public List<string> GetSelectedFilePaths1()
        {
            return new List<string>(selectedFilePaths1);
        }

        public List<string> GetSelectedDirectoryPaths1()
        {
            return new List<string>(selectedDirectoryPaths1);
        }

        public List<string> GetAllSelectedPaths1()
        {
            var allPaths = new List<string>();
            allPaths.AddRange(selectedFilePaths1);
            allPaths.AddRange(selectedDirectoryPaths1);
            return allPaths;
        }

        public void ClearAllSelections1()
        {
            selectedFilePaths1.Clear();
            selectedDirectoryPaths1.Clear();

            foreach (ListViewItem item in listView1.Items)
            {
                if (item != null && item.Tag != null)
                {
                    item.Checked = false;
                }
            }

            UpdateRichTextBox();
            UpdateLabels();

            UpdateButtonState();
        }

        public string GetSelectedFolder2()
        {
            return destinationFolder;
        }

        public void ClearSelection2()
        {
            destinationFolder = null;

            UpdateLabels();

            UpdateButtonState();
        }
    }
}