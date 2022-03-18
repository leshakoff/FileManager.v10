using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileManager.v10.Models
{
    internal class DummyFileSystemObjectInfo : FileSystemObjectInfo
    {
        public DummyFileSystemObjectInfo()
            : base(new DirectoryInfo("DummyFileSystemObjectInfo"))
        {
        }
    }
}
