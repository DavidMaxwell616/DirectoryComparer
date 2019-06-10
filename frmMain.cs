using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace DirectoryComparer
{
    public partial class frmMain : Form
    {
        private Preferences prefs { get; set; }

        private string sourceDirectory { get; set; }
        private string targetDirectory { get; set; }
  
        public frmMain()
        {
            InitializeComponent();
            prefs = GetPreferences();
            RefreshPrefs();
        }

        #region CompareTab
        private void btnCompare_Click(object sender, EventArgs e)
        {

            //var count = Directory.EnumerateFiles(textBox1.Text, "*.*", SearchOption.AllDirectories).Count();
            List<List<FileInfo>> sourceList = new List<List<FileInfo>>();
            List<List<FileInfo>> targetList = new List<List<FileInfo>>();

            foreach (var path in prefs.DirectoryPairs)
            {
                if (path.Enabled)
                { 
                sourceDirectory = path.SourcePath;
                targetDirectory = path.TargetPath;
                List<FileInfo> srclist = RecursiveFolderScan(sourceDirectory);
                sourceList.Add(srclist);
                List<FileInfo> targlist = RecursiveFolderScan(targetDirectory);
                targetList.Add(targlist);
                }
            }
            List<ComparisonResults> results = compareLists(sourceList, targetList);
        }

        private List<ComparisonResults> compareLists(List<List<FileInfo>> sourceList, List<List<FileInfo>> targetList)
        {
            var results = new List<ComparisonResults>();
            foreach (var item in sourceList)
            {
           //      var matches = targetList.Where(x => Path.Combine(x.FileEndPath,x.FileName)== Path.Combine(item.FileEndPath, item.FileName));
                    // this code is executed on each item in firstList but not in secondList
            }
            return results;
        }

        private List<FileInfo> RecursiveFolderScan(string path)
        {
            var info = new List<FileInfo>();
            var dirInfo = new DirectoryInfo(path);
            foreach (var entry in dirInfo.GetFileSystemInfos())
            {
                bool isDir = (entry.Attributes & FileAttributes.Directory) != 0;
                if (isDir)
                {
                    info.AddRange(RecursiveFolderScan(entry.FullName));
                }
                info.Add(new FileInfo()
                {
                    IsDirectory = isDir,
                    CreatedDate = entry.CreationTimeUtc,
                    ModifiedDate = entry.LastWriteTimeUtc,
                    FilePath = Path.GetDirectoryName(entry.FullName),
                    FileEndPath = Path.GetDirectoryName(entry.FullName).Replace(sourceDirectory, ""),
                    HashCode = entry.GetHashCode(),
                    FileName = Path.GetFileName(entry.FullName),
                    isSourceFile = path.Contains(sourceDirectory)
            }); 
               toolStripStatusLabel1.Text = entry.FullName;
                Application.DoEvents();
            }
            return info;
        }
        #endregion

        #region PreferencesTab
        private void RefreshPrefs()
        {
            prefsGrid.DataSource = null;
            prefsGrid.DataSource = prefs.DirectoryPairs;
            prefsGrid.Update();
            prefsGrid.Refresh();
            prefsGrid.Columns[1].Width = 200;
            prefsGrid.Columns[2].Width = 200;
        }
    
        private void SavePrefs(object sender, System.EventArgs e)
        {
            SavePreferences();
        }

        private void SavePreferences()
        {
            XmlDocument myXmlDocument = new XmlDocument();
            string currentPath = Directory.GetCurrentDirectory() + @"\preferences.xml";
            myXmlDocument.Load(currentPath);
            XmlNode node;
            node = myXmlDocument.DocumentElement;
            node = node.FirstChild;
            foreach (XmlNode node1 in node.ChildNodes)
            {
                if (node1.Name == "DirectoryPairs")
                {
                    node1.RemoveAll();
                    foreach (var item in prefs.DirectoryPairs)
                    {
                        string pair = "<DirectoryPair>" + item.SourcePath + "," + item.TargetPath + "</DirectoryPair>";
                        var node2 = XElement.Parse(pair);
                        XmlElement element = myXmlDocument.CreateElement("DirectoryPair");
                        element.InnerText = item.SourcePath + "," + item.TargetPath;
                        node1.AppendChild(element);
                    }
                }
            }
            myXmlDocument.Save(currentPath);

        }


        private string ValidateInputs(string sourcePath, string targetPath)
        {
            string temp = string.Empty;

            if (string.IsNullOrEmpty(sourcePath))
                temp += "Please choose source folder\r\n";

            if (string.IsNullOrEmpty(targetPath))
                temp += "Please choose target folder\r\n";

            if (File.Exists(sourcePath))
                temp += string.Format("{0} is a file, please choose a folder.\r\n", sourcePath);

            if (File.Exists(targetPath))
                temp += string.Format("{0} is a file, please choose a folder.\r\n", targetPath);

            if (!string.IsNullOrEmpty(sourcePath) && !Directory.Exists(sourcePath))
                temp += string.Format("{0} does not exist\r\n", sourcePath);

            if (!string.IsNullOrEmpty(targetPath) && !Directory.Exists(targetPath))
                temp += string.Format("{0} does not exist\r\n", targetPath);

            return temp;
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (resultsGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a row to edit");
                return;
            }
            Rowindex = resultsGrid.SelectedRows[0].Index;
            string newSourcePath = string.Empty;
            string newTargetPath = string.Empty;
            FolderBrowserDialog folderChooser = new FolderBrowserDialog();
            DialogResult result = folderChooser.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                newSourcePath = folderChooser.SelectedPath;
            }

            result = folderChooser.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                newTargetPath = folderChooser.SelectedPath;
            }

            DirectoryPair newDirectoryPair = new DirectoryPair
            {
                SourcePath = newSourcePath,
                TargetPath = newTargetPath
            };
            string status = ValidateInputs(newDirectoryPair.SourcePath, newDirectoryPair.TargetPath);
            if (status != string.Empty)
            {
                MessageBox.Show("Invalid Path values!\r\n" + status);
                return;
            }
            prefs.DirectoryPairs.RemoveAt(Rowindex);
            prefs.DirectoryPairs.Add(newDirectoryPair);
            RefreshPrefs();

        }

        public int Rowindex { get; set; }

        private void Selection_Changed(object sender, EventArgs e)
        {
            Rowindex = resultsGrid.SelectedRows[0].Index;
            //btnEdit.Enabled = true;
            //btnEdit.Image = DirectoryComparerIcons.edit_24;
            btnRemove.Enabled = true;
            //btnRemove.Image = DirectoryComparerIcons.Delete24;

        }
        private Preferences GetPreferences()
        {
            try
            {

                string currentPath = Directory.GetCurrentDirectory() + @"\preferences.xml";
                if (!File.Exists(currentPath))
                {
                    XDocument doc = new XDocument(new XElement("DirectoryComparerPreferences",
                        new XElement("Preferences",
                             new XElement("DirectoryPairs",
                                new XElement("DirectoryPair", "C:/,D:/")))));
                    doc.Save(currentPath);
                }

                XmlTextReader reader = new XmlTextReader(currentPath);
                prefs = new Preferences();
                prefs.DirectoryPairs = new List<DirectoryPair>();

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        while (reader.LocalName == "DirectoryPair")
                        {
                            string dirPair = reader.ReadElementString("DirectoryPair");
                            DirectoryPair pair = new DirectoryPair();
                            pair.Enabled = Convert.ToBoolean(dirPair.Split(',')[0]);
                            pair.SourcePath = dirPair.Split(',')[1];
                            pair.TargetPath = dirPair.Split(',')[2];
                            prefs.DirectoryPairs.Add(pair);
                        }
                    }

                }
                reader.Close();
                return prefs;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private void btnAdd_Click(object sender, EventArgs e)
        {
            string newSourcePath = string.Empty;
            string newTargetPath = string.Empty;
            FolderBrowserDialog folderChooser = new FolderBrowserDialog();
            DialogResult result = folderChooser.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                newSourcePath = folderChooser.SelectedPath;
            }

            result = folderChooser.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                newTargetPath = folderChooser.SelectedPath;
            }
            DirectoryPair newDirectoryPair = new DirectoryPair
            {
                SourcePath = newSourcePath,
                TargetPath = newTargetPath
            };
            prefs.DirectoryPairs.Add(newDirectoryPair);
            RefreshPrefs();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (resultsGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a row to edit");
                return;
            }
            int Rowindex = resultsGrid.SelectedRows[0].Index;
            prefs.DirectoryPairs.RemoveAt(Rowindex);
            RefreshPrefs();
        }

        #endregion

 
    }
}
