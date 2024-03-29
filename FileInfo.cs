﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryComparer
{
    public class FileInfo
    {
        public bool IsDirectory { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string FileName { get; set; }
        public string FilePath {get; set;}
        public int HashCode { get; set; }
        public string FileEndPath { get; internal set; }
        public bool isSourceFile { get; internal set; }
    }
}
