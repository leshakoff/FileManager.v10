using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileManager.v10.Models
{
    /// <summary>
    /// Это простая болванка для свойства Children класса FileSystemObjectInfo
    /// </summary>
    internal class BranchFileSystemObjectInfo : FileSystemObjectInfo
    {
        public BranchFileSystemObjectInfo()
            : base(new DirectoryInfo("BranchFileSystemObjectInfo"))
        {
        }
    }
}
