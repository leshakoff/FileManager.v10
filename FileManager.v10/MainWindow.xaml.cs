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
        bool MainWindowState = false;
        public string Reason = "";


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
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SearchCompleted);
            worker.WorkerSupportsCancellation = true;

        }


        private void OpenInExplorer(object sender, MouseButtonEventArgs e)
        {
            if (treeView.SelectedItem != null)
            {
                string file = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
                MainController.OpenInWinExplorer(file);
            }
        }

        private void GetFilesInCurrentFolder(object sender, MouseButtonEventArgs e)
        {
            if (treeView.SelectedItem != null)
            {
                string path = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;

                // v вот это нужно делегировать другому потоку, а пока он выполняется, 
                // нужно добавить какое-то отображение выполняемого прогресса
                dataGrid.ItemsSource = MainController.GetList(path);
                if (dataGrid.Items.Count == 0)
                    Boop.Text = "Ничего не найдено.";
            }

        }

        private void StartSearch(object sender, RoutedEventArgs e)
        {
            btnClick.IsEnabled = false;
            cancelSearchButton.IsEnabled = true;
            pbStatus.Visibility = Visibility.Visible;
            WorkerParam wp = new WorkerParam(BoopIsto.Text, searchLocation.SelectedItem
                .ToString()
                .Replace("System.Windows.Controls.ComboBoxItem: ", ""));
            if (!worker.IsBusy) worker.RunWorkerAsync(wp);
            Boop.Text = $"Выполняется поиск в {wp.sourcePath} по запросу: {BoopIsto.Text}...";
            pbStatus.IsIndeterminate = true;

        }

        private void SearchCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dataGrid.ItemsSource = aboutAll;
            if (dataGrid.Items.Count == 0)
            {
                Boop.Text = $"Упс... По вашему запросу ничего не найдено";
                cancelSearchButton.IsEnabled = false;
            }
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
                if (!String.IsNullOrEmpty(wp.param) && !String.IsNullOrWhiteSpace(wp.param))
                {
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
                else
                {
                    SearchStringIsEmpty();
                }
                
            }
            catch (ThreadInterruptedException)
            {
                
            }

        }

        private void SearchStringIsEmpty()
        {
            MessageBox.Show("Строка поиска пуста");
        }

        private void SearchStringChanged(object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(BoopIsto.Text)
                && !String.IsNullOrWhiteSpace(BoopIsto.Text)
                && !worker.IsBusy)
            {
                btnClick.IsEnabled = true;
                cancelSearchButton.IsEnabled = true;
            }
        }

        private void GetCurrentSearchLocation(object sender, EventArgs e)
        {
            if (treeView.SelectedItem != null)
            {
                currentFolder.Text = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
                currentFolderCombobox.Visibility = Visibility.Visible;
            }
            else
            {
                currentFolderCombobox.Visibility = Visibility.Collapsed;
            }

        }

        private void CancelSearchClick(object sender, RoutedEventArgs e)
        {
            if (_backgroundWorkerThread != null) _backgroundWorkerThread.Interrupt();
            Boop.Text = "Поиск был прерван пользователем";
            pbStatus.Visibility = Visibility.Hidden;
            btnClick.IsEnabled = true;
            cancelSearchButton.IsEnabled = false;
        }

        private void CollapseWindow(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Application app = Application.Current;
            app.Shutdown();
        }

        private void RestoreWindow(object sender, RoutedEventArgs e)
        {
            if (!MainWindowState)
            {
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
                MainWindowState = true;
            }
            else
            {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                MainWindowState = false;
            }
        }

        private void DragNDropWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }

}
