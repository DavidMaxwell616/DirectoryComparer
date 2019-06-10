using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryComparer
{
    class ComparisonResults
    {
        public int ComparisonSet { get; set; }
        public bool FileExistsOnTarget { get; set; }
        public bool FileExistsOnSource { get; set; }
       public bool FileMatches { get; set; }
       public bool FileDoesNotMatch { get; set; }
    }
}
