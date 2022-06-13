using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FileManager.v10.Models
{
    internal class IOHelper
    {
        internal static bool TryEnumerateDiretory(string path, out IEnumerable<string> dirEnumerable)
        {
            try
            {
                dirEnumerable = Directory.EnumerateDirectories(path);
                return true;
            }
            catch (Exception ex)
            {
                //LOG
            }

            dirEnumerable = Array.Empty<string>();

            return false;
        }

        internal static bool TryEnumerateFiles(string path, out IEnumerable<string> filesEnumerable)
        {
            try
            {
                filesEnumerable = Directory.EnumerateFiles(path);
                return true;
            }
            catch (Exception ex)
            {
                //LOG
            }

            filesEnumerable = Array.Empty<string>();

            return false;
        }
    }
}
