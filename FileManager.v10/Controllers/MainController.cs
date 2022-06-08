using FileManager.v10.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace FileManager.v10
{
    public class MainController
    {
        /// <summary>
        /// Метод, открывающий файл/папку в проводнике.
        /// Если передаём папку - папка открывается, 
        /// если файл - то файл просто выделяется в
        /// папке, в которой он расположен. 
        /// </summary>
        /// <param name="file">Путь до файла/папки</param>
        public static void OpenInWinExplorer(string file)
        {
            try
            {
                Process PrFolder = new Process();
                ProcessStartInfo psi = new ProcessStartInfo();

                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Normal;
                psi.FileName = "explorer";

                if (Directory.Exists(file))
                {
                    psi.Arguments = @"/n, /open, " + file;
                }
                else if (File.Exists(file))
                {
                    psi.Arguments = @"/n, /select, " + file;
                }

                PrFolder.StartInfo = psi;
                PrFolder.Start();
            }
            catch
            {
                MessageBox.Show("Доступ к папке защищён правами администратора!", 
                    "Ошибка", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }


        public static void OpenFileWithStandartProgram(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    var p = new Process();
                    p.StartInfo = new ProcessStartInfo(file)
                    {
                        UseShellExecute = true
                    };
                    p.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Открыть файл программой по умолчанию не удалось:: {ex.Message}",
                    "Ошибка открытия файла",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Перегруженная версия метода GetList, которая используется для поиска файлов/папок
        /// </summary>
        /// <param name="dirs">Список папок</param>
        /// <param name="files">Список файлов</param>
        /// <returns>Возвращает список файлов и папок, преобразованных в FileAbout</returns>
        public static IEnumerable<FileAbout> GetList(List<DirectoryInfo> dirs, List<FileInfo> files)
        {
            List<FileAbout> allFilesAndDirectories = new List<FileAbout>();

            try
            {
                foreach (var d in dirs)
                {
                    // img.Freeze мы используем для того, чтобы не возникало исключений. 
                    // исключения ругаются на то, что мы получаем картинку в одном потоке, 
                    // и пытаемся её использовать в другом.
                    ImageSource img = FolderManager.GetImageSource(d.FullName, ShellManager.ItemState.Close);
                    img.Freeze();

                    FileAbout fi = new FileAbout
                    {
                        Name = d.Name,
                        CreationTime = d.CreationTime,
                        Extension = "Папка",
                        LastWrite = d.LastWriteTime,
                        FullPath = d.FullName,
                        Image = img
                    };
                    allFilesAndDirectories.Add(fi);
                }

                foreach (var f in files)
                {
                    ImageSource img = FileManager.v10.Models.FileManager.GetImageSource(f.FullName);
                    img.Freeze();
                    FileAbout fiAbout = new FileAbout
                    {
                        Name = f.Name,
                        CreationTime = f.CreationTime,
                        Extension = f.Extension,
                        LastWrite = f.LastWriteTime,
                        FullPath = f.FullName,
                        Image = img
                    };
                    allFilesAndDirectories.Add(fiAbout);

                }
            }
            catch
            {

            }

            return allFilesAndDirectories;
        }



        private static bool TryGetDirectoryData(string d, out FileAbout data)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(d);
                if (!object.Equals((di.Attributes & FileAttributes.System), FileAttributes.System) &&
                    !object.Equals((di.Attributes & FileAttributes.Hidden), FileAttributes.Hidden))
                {

                    FileAbout fi = new FileAbout
                    {
                        Name = di.Name,
                        CreationTime = di.CreationTime,
                        Extension = "Папка",
                        Size = GetSize(DirSize(di)),
                        LastWrite = di.LastWriteTime,
                        FullPath = di.FullName,
                        Image = FolderManager.GetImageSource(di.FullName, ShellManager.ItemState.Close)
                    };
                    //allFilesAndDirectories.Add(fi);
                    data = fi;
                    fi.Image.Freeze();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR to get directory info for {d}. Exception: {ex}");
            }

            data = null;
            return false;
        }


        public static IEnumerable<FileAbout> GetList(string path)
        {
            if (!File.Exists(path))
            {
                IEnumerable<string> dirs = Directory.EnumerateDirectories(path);
                IEnumerable<string> files = Directory.EnumerateFiles(path);

                foreach (var d in dirs)
                {

                    if (TryGetDirectoryData(d, out var data))
                        yield return data;
                }

                foreach (var f in files)
                {
                    FileInfo fi = new FileInfo(f);
                    if (!object.Equals((fi.Attributes & FileAttributes.System), FileAttributes.System) &&
                        !object.Equals((fi.Attributes & FileAttributes.Hidden), FileAttributes.Hidden))
                    {

                        FileAbout fiAbout = new FileAbout
                        {
                            Name = fi.Name,
                            CreationTime = fi.CreationTime,
                            Extension = fi.Extension,
                            LastWrite = fi.LastWriteTime,
                            Size = GetSize(fi.Length),
                            FullPath = fi.FullName,
                            Image = FileManager.v10.Models.FileManager.GetImageSource(fi.FullName)
                        };
                        fiAbout.Image.Freeze();
                        yield return fiAbout;
                    }

                }

            }
            else
            {
                FileInfo fi = new FileInfo(path);
                if (!object.Equals((fi.Attributes & FileAttributes.System), FileAttributes.System) &&
                    !object.Equals((fi.Attributes & FileAttributes.Hidden), FileAttributes.Hidden))
                {

                    FileAbout ab = new FileAbout
                    {
                        Name = fi.Name,
                        CreationTime = fi.CreationTime,
                        Extension = fi.Extension,
                        LastWrite = fi.LastWriteTime,
                        FullPath = fi.FullName,
                        Size = GetSize(fi.Length),
                        Image = FileManager.v10.Models.FileManager.GetImageSource(fi.FullName)
                    };

                    ab.Image.Freeze();
                    yield return ab;
                }

            }

        }


        // когда натыкаемся на папки, к которым доступ закрыт, размер не рассчитывается. 
        public static long DirSize(DirectoryInfo dir)
        {
            long size = 0;
            try
            {
                size = 0;
            }
            catch
            {

            }
            return size;
        }


        /// <summary>
        /// Метод, преобразовывающий размер в размер, описанный в кило-, мега-, гигабайтах и тд.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string GetSize(long size)
        {
            var unit = 1024;
            if (size < unit) { return $"{size} B"; }

            var exp = (int)(Math.Log(size) / Math.Log(unit));
            return $"{size / Math.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
        }


        /// <summary>
        /// Метод, копирующий директорию со всем её содержимым.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        public static void CopyDirectory(string sourcePath, string targetPath)
        {
            Directory.CreateDirectory(targetPath);
            foreach (string s1 in Directory.GetFiles(sourcePath))
            {
                string s2 = targetPath + "\\" + Path.GetFileName(s1);
                File.Copy(s1, s2);
            }
            foreach (string s in Directory.GetDirectories(sourcePath))
            {
                CopyDirectory(s, targetPath + "\\" + Path.GetFileName(s));
            }
        }
    }
}
