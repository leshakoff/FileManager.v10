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
            //worker.ProgressChanged += new ProgressChangedEventHandler(Bgworker_ProgressChanged);
            worker.WorkerReportsProgress = true;

            

        }

        private void Bgworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //Boop.Text = e.ProgressPercentage.ToString();
            pbStatus.Value = e.ProgressPercentage;
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
                            /*FileManager.v10.Models.FileManager.GetImageSource(fi.FullName).Freeze()*/;
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

                dataGrid.ItemsSource = allFilesAndDirectories;



            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WorkerParam wp = new WorkerParam(BoopIsto.Text);
            worker.RunWorkerAsync(wp);
            Boop.Text = $"Выполняется поиск по запросу: {BoopIsto.Text}...";
            pbStatus.IsIndeterminate = true;
        }

        private void SerchCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dataGrid.ItemsSource = aboutAll;
            Boop.Text = "Поиск завершён!";
            pbStatus.IsIndeterminate = false;
        }

        private void Search(object sender, DoWorkEventArgs e)
        {
            
            BackgroundWorker bg = sender as BackgroundWorker;
            WorkerParam wp = (WorkerParam)e.Argument;
            string search = $"*{wp.param}*";

            List<FileAbout> allFilesAndDirectories = new List<FileAbout>();

            Task<List<FileInfo>> task = FileSearcher.GetFilesFastAsync(@"C:\", search);
            
            Task<List<DirectoryInfo>> dirs = DirectorySearcher.GetDirectoriesFastAsync(@"C:\", search);


            foreach (var d in dirs.Result)
            {
                ImageSource img = FolderManager.GetImageSource(d.FullName, ShellManager.ItemState.Close);
                img.Freeze();

                FileAbout fi = new FileAbout
                {
                    Name = d.Name,
                    CreationTime = d.CreationTime,
                    Extension = d.Extension,
                    LastWrite = d.LastWriteTime,
                    FullPath = d.FullName,
                    Image = img
                };
                allFilesAndDirectories.Add(fi);
            }

            foreach (var f in task.Result)
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
            bg.ReportProgress(99);

            aboutAll = allFilesAndDirectories;
            bg.ReportProgress(100);

        }


        // когда натыкаемся на папки, к которым доступ закрыт, 
        // вылетает исключение. исправить 
        public static long DirSize(DirectoryInfo dir)
        {

                return dir.GetFiles().Sum(fi => fi.Length) +
                       dir.GetDirectories().Sum(di => DirSize(di));
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
