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
        private Thread _backgroundWorkerThread;
        public TimeSpan stopwatch;
        public long quantity;


        public List<FileAbout> aboutAll = new List<FileAbout>();
        public class WorkerParam
        {
            public string param;
            public string sourcePath;

            public WorkerParam(string p, string source)
            {
                param = p;
                sourcePath = source;
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
            worker.WorkerSupportsCancellation = true;

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
                if (dataGrid.Items.Count == 0)
                    Boop.Text = "Ничего не найдено.";
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            btnClick.IsEnabled = false;
            cancelSearchButton.IsEnabled = true;
            pbStatus.Visibility = Visibility.Visible;
            WorkerParam wp = new WorkerParam(BoopIsto.Text, searchLocation.SelectedItem
                .ToString()
                .Replace("System.Windows.Controls.ComboBoxItem: ", ""));
            worker.RunWorkerAsync(wp);
            Boop.Text = $"Выполняется поиск в {wp.sourcePath} по запросу: {BoopIsto.Text}...";
            pbStatus.IsIndeterminate = true;

        }

        private void SerchCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            dataGrid.ItemsSource = aboutAll;
            if (dataGrid.Items.Count == 0)
                Boop.Text = $"Упс... По вашему запросу ничего не найдено";
            else
            {
                Boop.Text = $"Поиск завершён! \n" +
                            $"Найдено: {quantity} файлов/директорий за " +
                            $"{stopwatch.Hours} чч" +
                            $":{stopwatch.Minutes} мм" +
                            $":{stopwatch.Seconds} сс" +
                            $":{stopwatch.Milliseconds} мс";
            }



            pbStatus.IsIndeterminate = false;
            pbStatus.Visibility = Visibility.Hidden;
        }

        private void Search(object sender, DoWorkEventArgs e)
        {
            try
            {
                _backgroundWorkerThread = Thread.CurrentThread;

                Stopwatch sp = new Stopwatch();
                sp.Start();
                BackgroundWorker bg = sender as BackgroundWorker;
                WorkerParam wp = (WorkerParam)e.Argument;
                string search = $"*{wp.param}*";
                string path = wp.sourcePath;


                List<FileAbout> allFilesAndDirectories = new List<FileAbout>();

                Task<List<FileInfo>> task = FileSearcher.GetFilesFastAsync(path, search);

                Task<List<DirectoryInfo>> dirs = DirectorySearcher.GetDirectoriesFastAsync(path, search);

                aboutAll = MainController.GetList(dirs.Result, task.Result);

                sp.Stop();
                stopwatch = sp.Elapsed;
                quantity = aboutAll.Count;
            }
            catch (ThreadInterruptedException)
            {
                
            }

        }

        private void BoopIsto_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(BoopIsto.Text)
                && !String.IsNullOrWhiteSpace(BoopIsto.Text)
                && !worker.IsBusy)
            {
                btnClick.IsEnabled = true;
                cancelSearchButton.IsEnabled = true;
            }
        }

        private void searchLocation_DropDownOpened(object sender, EventArgs e)
        {
            if (treeView.SelectedItem != null)
            {
                currentFolder.Text = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
            }
            else
            {
                currentFolder.Visibility = Visibility.Collapsed;
            }

        }

        private void cancelSearchButton_Click(object sender, RoutedEventArgs e)
        {

            if (_backgroundWorkerThread != null) _backgroundWorkerThread.Interrupt();

            Boop.Text = "Поиск был прерван пользователем";
            pbStatus.Visibility = Visibility.Hidden;
            btnClick.IsEnabled = true;
            cancelSearchButton.IsEnabled = false;


        }
    }

}
