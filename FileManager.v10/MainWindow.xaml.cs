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
                if (drive.IsReady) this.treeView.Items.Add(new FileSystemObjectInfo(drive));
            }
            worker = new BackgroundWorker();

            worker.DoWork += new DoWorkEventHandler(Search);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SerchCompleted);

        }


        private void ItemsControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (treeView.SelectedItem != null)
            {
                string file = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
                MainController.OpenInWinExplorer(file);
            }
        }

        private void treeView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (treeView.SelectedItem != null)
            {
                string path = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
                dataGrid.ItemsSource = MainController.GetList(path);
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            btnClick.IsEnabled = false;
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
                // img.Freeze мы используем для того, чтобы не возникало исключений. 
                // исключения ругаются на то, что мы получаем картинку в одном потоке, 
                // и пытаемся её использовать в другом; в нашем случае, backgroundworker
                // пытается получить картинку из основного потока, и вылетает исключение. 
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
            aboutAll = allFilesAndDirectories;

        }

        private void BoopIsto_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(BoopIsto.Text) && !String.IsNullOrWhiteSpace(BoopIsto.Text)
                && !worker.IsBusy)
            {
                btnClick.IsEnabled = true;
            }
        }
    }

}
