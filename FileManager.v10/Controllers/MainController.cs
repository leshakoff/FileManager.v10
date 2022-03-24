﻿using FileManager.v10.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace FileManager.v10
{
    public class MainController
    {
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

                //psi.Arguments = @"/n, /select, " + file;
                PrFolder.StartInfo = psi;
                PrFolder.Start();
            }
            catch
            {
                MessageBox.Show("Доступ к папке защищён правами администратора!");
            }
        }

        public static List<FileAbout> GetList(string path)
        {
            List<FileAbout> allFilesAndDirectories = new List<FileAbout>();

            if (!File.Exists(path))
            {
                List<string> dirs = Directory.GetDirectories(path).ToList();
                List<string> files = Directory.GetFiles(path).ToList();

                foreach (var d in dirs)
                {

                    DirectoryInfo di = new DirectoryInfo(d);
                    if (!object.Equals((di.Attributes & FileAttributes.System), FileAttributes.System) &&
                        !object.Equals((di.Attributes & FileAttributes.Hidden), FileAttributes.Hidden))
                    {

                        //ImageSource img = FolderManager.GetImageSource(di.FullName, ShellManager.ItemState.Close).Clone();

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
                        allFilesAndDirectories.Add(fi);
                    }


                }

                foreach (var f in files)
                {
                    FileInfo fi = new FileInfo(f);
                    if (!object.Equals((fi.Attributes & FileAttributes.System), FileAttributes.System) &&
                        !object.Equals((fi.Attributes & FileAttributes.Hidden), FileAttributes.Hidden))
                    {
                        /*FileManager.v10.Models.FileManager.GetImageSource(fi.FullName).Freeze()*/
                        ;
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
                        allFilesAndDirectories.Add(fiAbout);
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

                    allFilesAndDirectories.Add(ab);
                }

            }

            return allFilesAndDirectories;
        }


        // когда натыкаемся на папки, к которым доступ закрыт, размер не рассчитывается. 
        public static long DirSize(DirectoryInfo dir)
        {
            long size = 0;
            try
            {
                size = dir.GetFiles().Sum(fi => fi.Length) +
                   dir.GetDirectories().Sum(di => DirSize(di));
            }
            catch
            {
                
            }
            return size;
        }

        public static string GetSize(long size)
        {
            var unit = 1024;
            if (size < unit) { return $"{size} B"; }

            var exp = (int)(Math.Log(size) / Math.Log(unit));
            return $"{size / Math.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
        }
    }
}
