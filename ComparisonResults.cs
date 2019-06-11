using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryComparer
{
    public enum ComparisonStatus
    {
        FileExistsOnSource,
        FileExistsOnTarget,
        FileMatches,
        FileDoesNotMatch
    }

    class ComparisonResults
    {
        public int ComparisonSet { get; set; }

        public string FilePath { get; set; }

         public ComparisonStatus CompareStatus {get; set;}

    }

}
