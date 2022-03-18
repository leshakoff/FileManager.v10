using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FileManager.v10.Models;
using FastSearchLibrary;
using System.Threading;

namespace FileManager.v10
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<FileAbout> aboutAll = new List<FileAbout>();
        public class WorkerParam
        {
            public string param;

            public WorkerParam(string p)
            {
                param = p;
            }
        }

        public BackgroundWorker worker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                this.treeView.Items.Add(new FileSystemObjectInfo(drive));
            }
            worker = new BackgroundWorker();

            worker.DoWork += new DoWorkEventHandler(Search);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SerchCompleted);

        }

        private void ItemsControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process PrFolder = new Process();
                ProcessStartInfo psi = new ProcessStartInfo();
                string file = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
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

        private void treeView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            List<FileAbout> allFilesAndDirectories = new List<FileAbout>();

            if (treeView.SelectedItem != null)
            {
                string path = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
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
                            FileAbout fi = new FileAbout
                            {
                                Name = di.Name,
                                CreationTime = di.CreationTime,
                                Extension = di.Extension,
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
                            FileAbout fiAbout = new FileAbout
                            {
                                Name = fi.Name,
                                CreationTime = fi.CreationTime,
                                Extension = fi.Extension,
                                LastWrite = fi.LastWriteTime,
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
                            FullPath = fi.FullName
                        };

                        allFilesAndDirectories.Add(ab);
                    }
                     
                }

                dataGrid.ItemsSource = allFilesAndDirectories;



            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //string search = $"*{BoopIsto.Text}*";
            //Boop.Text = $"Выполняется поиск по запросу: {BoopIsto.Text}...";
            WorkerParam wp = new WorkerParam(BoopIsto.Text);
            worker.RunWorkerAsync(wp);
            Boop.Text = $"Выполняется поиск по запросу: {BoopIsto.Text}...";
        }

        private void SerchCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dataGrid.ItemsSource = aboutAll;
            Boop.Text = "Поиск завершён!";
        }

        private void Search(object sender, DoWorkEventArgs e)
        {
            WorkerParam wp = (WorkerParam)e.Argument;
            string search = $"*{wp.param}*";

            List<FileAbout> allFilesAndDirectories = new List<FileAbout>();

            Task<List<FileInfo>> task = FileSearcher.GetFilesFastAsync(@"C:\", search);
            Task<List<DirectoryInfo>> dirs = DirectorySearcher.GetDirectoriesFastAsync(@"C:\", search);

            foreach (var d in dirs.Result)
            {


                FileAbout fi = new FileAbout
                {
                    Name = d.Name,
                    CreationTime = d.CreationTime,
                    Extension = d.Extension,
                    LastWrite = d.LastWriteTime,
                    FullPath = d.FullName,
                    Image = FolderManager.GetImageSource(d.FullName, ShellManager.ItemState.Close)
                };
                allFilesAndDirectories.Add(fi);
            }

            foreach (var f in task.Result)
            {


                FileAbout fiAbout = new FileAbout
                {
                    Name = f.Name,
                    CreationTime = f.CreationTime,
                    Extension = f.Extension,
                    LastWrite = f.LastWriteTime,
                    FullPath = f.FullName,
                    Image = FileManager.v10.Models.FileManager.GetImageSource(f.FullName)
                };
                allFilesAndDirectories.Add(fiAbout);

            }

            aboutAll = allFilesAndDirectories;

        }
    }






}
