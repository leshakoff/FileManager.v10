using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FileManager.v10
{
    public class FileAbout
    {
        public string Name { get; set; }
        public DateTime CreationTime { get; set; }
        public string Extension { get; set; }
        public DateTime LastWrite { get; set; }
        public string Size { get; set; }
        public ImageSource Image { get; set; }
        public string FullPath { get; set; }
    }
}
