using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DirectoryComparer
{
    public partial class Form1 : Form
    {

        private string sourceDirectory = @"C:\Users\maxxd\Music";
        private string targetDirectory = @"F:\Users\maxxd_000\Music";

        //<DirectoryPair>1,C:\Users\maxxd_000\Documents,G:\Users\Max\Documents</DirectoryPair>
        //<DirectoryPair>1,C:\Users\maxxd_000\Pictures,G:\Users\Max\Pictures</DirectoryPair>
        //<DirectoryPair>1,C:\Users\maxxd_000\Music,F:\Users\Max\Music</DirectoryPair>
        public Form1()
        {
            InitializeComponent();
            textBox1.Text = sourceDirectory;
            textBox2.Text = targetDirectory;
        }

        private void Button1_Click(object sender, EventArgs e)
        {

            var count = Directory.EnumerateFiles(textBox1.Text, "*.*", SearchOption.AllDirectories).Count();
            List<FileInfo> sourceList = RecursiveFolderScan(textBox1.Text);
            List<FileInfo> targetList = RecursiveFolderScan(textBox2.Text);
            List<ComparisonResults> results = compareLists(sourceList, targetList);
        }

        private List<ComparisonResults> compareLists(List<FileInfo> sourceList, List<FileInfo> targetList)
        {
            var results = new List<ComparisonResults>();
            foreach (var item in sourceList)
            {
                 var matches = targetList.Where(x => Path.Combine(x.FileEndPath,x.FileName)== Path.Combine(item.FileEndPath, item.FileName));
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
               status.Text = entry.FullName;
                Application.DoEvents();
            }
            return info;
        }
    }
}
