using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryComparer
{
    public class Preferences
    {
        public List<DirectoryPair> DirectoryPairs { get; set; }
    }
    public class DirectoryPair
    {
         public bool Enabled { get; set; }
        public int ComparisonSet { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
    }
}
