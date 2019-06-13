using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Windows.Forms.DataVisualization.Charting;

namespace DirectoryComparer
{
    public partial class frmMain : Form
    {
        private Preferences prefs { get; set; }
        private List<ComparisonResults> results { get; set; }

        private string sourceDirectory { get; set; }
        private string targetDirectory { get; set; }
  
        private int fileMatches { get; set; }

        private int onSource { get; set; }

        private int onTarget { get; set; }

        private int unMatched { get; set; }
        private int totalFiles { get; set; }
        public frmMain()
        {
            InitializeComponent();
            prefs = GetPreferences();
            RefreshPrefs();
            btnSync.Enabled = false;
        }

        #region CompareTab
        private void btnCompare_Click(object sender, EventArgs e)
        {
            results = new List<ComparisonResults>();

            Cursor = Cursors.WaitCursor;
            foreach (var path in prefs.DirectoryPairs)
            {
                if (path.Enabled)
                { 
                sourceDirectory = path.SourcePath;
                targetDirectory = path.TargetPath;
                List<FileInfo> sourceList = RecursiveFolderScan(sourceDirectory);
                List<FileInfo> targetList = RecursiveFolderScan(targetDirectory);
                results = compareLists(sourceList, targetList,path.ComparisonSet);
                }
            }
            toolStripStatusLabel1.Text = string.Empty;
            resultsGrid.DataSource = null;
            resultsGrid.DataSource = results;
            resultsGrid.Update();
            resultsGrid.Refresh();
            resultsGrid.Columns[1].Width = 300;
            Cursor = Cursors.Default;
            updateResults();
            if (resultsGrid.RowCount>0)
                btnSync.Enabled = true;
        }

        private List<ComparisonResults> compareLists(List<FileInfo> sourceList, List<FileInfo> targetList, int set)
        {
            var results = new List<ComparisonResults>();

            var matches = (from x in sourceList
                          join y in targetList
                         on new { x.FileEndPath, x.ModifiedDate } 
                         equals new { y.FileEndPath, y.ModifiedDate }
                         select (new ComparisonResults
                         {
                             ComparisonSet = set,
                             FilePath = x.FileEndPath,
                             CompareStatus = ComparisonStatus.FileMatches
                         })).ToList();

            results.AddRange(matches);
            fileMatches = matches.Count();

            var existsOnSource = (from x in sourceList
                                  join y in targetList on x.FileEndPath equals y.FileEndPath into gj
                                  from outliers in gj.DefaultIfEmpty()
                                  where outliers == null
                                  select (new ComparisonResults
                                  {
                                      ComparisonSet = set,
                                      FilePath = x.FileEndPath,
                                      CompareStatus = ComparisonStatus.FileExistsOnSource
                                  })).ToList();

            results.AddRange(existsOnSource);
            onSource = existsOnSource.Count();

            var existsOnTarget = (from x in targetList
                                  join y in sourceList on x.FileEndPath equals y.FileEndPath into gj
                                  from outliers in gj.DefaultIfEmpty()
                                  where outliers == null
                                  select (new ComparisonResults
                                  {
                                      ComparisonSet = set,
                                      FilePath = x.FileEndPath,
                                      CompareStatus = ComparisonStatus.FileExistsOnTarget
                                  })).ToList();

            results.AddRange(existsOnTarget);
            onTarget = existsOnTarget.Count();

            var nonMatches = from x in sourceList
                          join y in targetList
                         on x.FileEndPath equals y.FileEndPath
                         where x.ModifiedDate> y.ModifiedDate
                          select (new ComparisonResults
                          {
                              ComparisonSet = set,
                              FilePath = x.FileEndPath,
                              CompareStatus = ComparisonStatus.FileDoesNotMatch
                          });

            results.AddRange(nonMatches);
            unMatched = nonMatches.Count();

            totalFiles = results.Count();
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
                    FileEndPath = Path.GetFullPath(entry.FullName).Replace(sourceDirectory, "").Replace(targetDirectory, ""),
                    HashCode = entry.GetHashCode(),
                    FileName = Path.GetFileName(entry.FullName),
                    isSourceFile = path.Contains(sourceDirectory)
            }); 
               toolStripStatusLabel1.Text = entry.FullName;
                Application.DoEvents();
                Cursor = Cursors.WaitCursor;
            }
            return info;
        }

        private void BtnSync_Click(object sender, EventArgs e)
        {
            foreach (var path in prefs.DirectoryPairs)
            {
                string srcPath = string.Empty;
                string destPath = string.Empty;

                if (path.Enabled)
                {
                    foreach (var item in results)
                    {
                        if (item.ComparisonSet== path.ComparisonSet)
                        {
                            switch (item.CompareStatus)
                            {
                                case ComparisonStatus.FileExistsOnSource:
                                    srcPath = String.Concat(path.SourcePath, item.FilePath);
                                    destPath = String.Concat(path.TargetPath, item.FilePath);
                                    File.Copy(srcPath, destPath);
                                    item.CompareStatus = ComparisonStatus.FileMatches;
                                    break;
                                case ComparisonStatus.FileExistsOnTarget:
                                    destPath = string.Concat(path.TargetPath, item.FilePath);
                                    File.Delete(destPath);
                                    results.Remove(item);
                                    break;
                                case ComparisonStatus.FileDoesNotMatch:
                                    srcPath = String.Concat(path.SourcePath, item.FilePath);
                                    destPath = String.Concat(path.TargetPath, item.FilePath);
                                    File.Copy(srcPath, destPath, true);
                                    item.CompareStatus = ComparisonStatus.FileMatches;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
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
                int i = 0;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        while (reader.LocalName == "DirectoryPair")
                        {
                            string dirPair = reader.ReadElementString("DirectoryPair");
                            DirectoryPair pair = new DirectoryPair();
                            pair.Enabled = Convert.ToBoolean(dirPair.Split(',')[0]);
                            pair.ComparisonSet = i;
                            pair.SourcePath = dirPair.Split(',')[1];
                            pair.TargetPath = dirPair.Split(',')[2];
                            prefs.DirectoryPairs.Add(pair);
                            i++;
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

        #region ResultsTab
        private void updateResults()
        {
            txtMatches.Text = String.Format("{0:n0}", fileMatches);
            txtNonMatches.Text = String.Format("{0:n0}", unMatched);
            txtSource.Text = String.Format("{0:n0}", onSource);
            txtTarget.Text = String.Format("{0:n0}", onTarget);
            txtTotal.Text = String.Format("{0:n0}", totalFiles);

            chart1.Titles.Add("Comparison Results");

            string[] seriesArray = { "Matches", "Non-Matches", "File Exists On Source Only", "File Exists On Target Only" };
            int[] pointsArray = { fileMatches, unMatched, onSource, onTarget };

            for (int i = 0; i < seriesArray.Length; i++)
            {
                chart1.Series["s1"].Points.AddXY(seriesArray[i], pointsArray[i]);
            }

    }
    #endregion
}
}
