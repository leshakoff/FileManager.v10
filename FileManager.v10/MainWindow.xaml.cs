using FastSearchLibrary;
using FileManager.v10.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace FileManager.v10
{
    public partial class MainWindow : Window
    {
        private Thread backgroundWorkerThread;         // Thread используется в небольшом костыле, 
                                                       // предзназначенном для отмены поиска, который
                                                       // мы выполняем в методе DoWork бэкграундворкера.
                                                       // Так как у самого BakcgroundWorker-а 
                                                       // отмена задачи работает не очень удобно, 
                                                       // мы используем эдакий костыль в виде 
                                                       // присвоения работы бэкграундворкера обычному 
                                                       // экземпляру класса Thread, у которого мы можем
                                                       // вызвать метод Interrupt, позволяющий нам прервать 
                                                       // работу потока. 

        public TimeSpan stopwatch;                      // глобальная переменная, в которую мы можем записать
                                                        // время, за которое осуществился поиск. 

        public long quantity;                           // глобальная переменная для записи количества
                                                        // найденных файлов. Почему глобально? Потому что из
                                                        // BackgroundWorker-а, в котором сам поиск осуществляется, 
                                                        // невозможно работать с UI-элементами напрямую. 
                                                        // Поэтому мы временно сохраняем эти значения, 
                                                        // и затем используем их в UI в основном потоке. 

        bool mainWindowState = false;                   // "переключатель" для корректной работы кнопки 
                                                        // "развернуть" окно. По умолчанию - false; 
                                                        // если окно развёрнуто - true.


        public List<FileAbout> aboutAll = new List<FileAbout>();    // временный список, в котором мы будем хранить
                                                                    // найденные файлы/папки из BackgroundWorkera. 

        public List<FileAbout> aboutForDG = new List<FileAbout>();    // временный список, в котором мы будем хранить
                                                                      // найденные файлы/папки из BackgroundWorkera. 

        //public BackgroundWorker worker = new BackgroundWorker();
        //public BackgroundWorker workerForGrid = new BackgroundWorker();

        public string pathForDG = "";


        public MainWindow()
        {
            InitializeComponent();
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (drive.IsReady) this.treeView.Items.Add(new FileSystemObjectInfo(drive));
            }


            //workerForGrid = new BackgroundWorker();
            //workerForGrid.DoWork += new DoWorkEventHandler(GetCurrentContent);
            //workerForGrid.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CurrentContentCompleted);


            var model = new IndexViewModel();
            DataContext = model;

            //worker = new BackgroundWorker();
            //worker.DoWork += new DoWorkEventHandler(Search);
            //worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SearchCompleted);
            //worker.WorkerSupportsCancellation = true;

            if (dataGrid.Items.Count > 0)
                dataGrid.Visibility = Visibility.Visible;

        }

        private void OpenInExplorer(object sender, MouseButtonEventArgs e)
        {
            if (treeView.SelectedItem != null)
            {
                string file = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
                MainController.OpenInWinExplorer(file);
            }
        }

        /*
        private void GetCurrentContent(object sender, DoWorkEventArgs e)
        {
            aboutForDG = MainController.GetList(pathForDG);
            e.Result = true;
        }
      

        private void CurrentContentCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            //MessageBox.Show("я завершился");

            try
            {
                spinnerGif.Visibility = Visibility.Collapsed;
                dataGrid.ItemsSource = aboutForDG;

                if (dataGrid.Items.Count == 0)
                    Boop.Text = "Ничего не найдено.";

                if (dataGrid.Items.Count > 0)
                    dataGrid.Visibility = Visibility.Visible;
                else
                    dataGrid.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }

        }


  */


        private void GetFilesInCurrentFolder(object sender, MouseButtonEventArgs e)
        {
            if (treeView.SelectedItem != null)
            {
                pathForDG = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
                spinnerGif.Visibility = Visibility.Visible;

                (DataContext as IndexViewModel).LoadData(pathForDG).ContinueWith(r =>
                {
                    Boop.Text = String.Empty;
                    spinnerGif.Visibility = Visibility.Collapsed;

                    if (dataGrid.Items.Count == 0)
                        Boop.Text = "Ничего не найдено.";

                    if (dataGrid.Items.Count > 0)
                        dataGrid.Visibility = Visibility.Visible;
                    else
                        dataGrid.Visibility = Visibility.Collapsed;
                }, TaskScheduler.FromCurrentSynchronizationContext());



                //if (!workerForGridThread.IsAlive)
                //    workerForGridThread.Start();

                //if (!workerForGrid.IsBusy) 
                //    workerForGrid.RunWorkerAsync();



            }

        }

        private void StartSearch(object sender, RoutedEventArgs e)
        {
            btnClick.IsEnabled = false;
            cancelSearchButton.IsEnabled = true;
            pbStatus.Visibility = Visibility.Visible;

            WorkerParam wp = new WorkerParam(searchString.Text, searchLocation.SelectedItem
                .ToString()
                .Replace("System.Windows.Controls.ComboBoxItem: ", ""));

            //if (!worker.IsBusy) 
            //    worker.RunWorkerAsync(wp);

            Boop.Text = $"Выполняется поиск в {wp.sourcePath} по запросу: {searchString.Text}...";
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

            if (dataGrid.Items.Count > 0)
                dataGrid.Visibility = Visibility.Visible;
            else
                dataGrid.Visibility = Visibility.Collapsed;

        }

        private void Search(object sender, DoWorkEventArgs e)
        {
            try
            {
                backgroundWorkerThread = Thread.CurrentThread;

                Stopwatch sp = new Stopwatch();
                sp.Start();
                WorkerParam wp = (WorkerParam)e.Argument;
                if (!String.IsNullOrEmpty(wp.param) && !String.IsNullOrWhiteSpace(wp.param))
                {
                    string search = $"*{wp.param}*";
                    string path = wp.sourcePath;

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
            if (!String.IsNullOrEmpty(searchString.Text)
                && !String.IsNullOrWhiteSpace(searchString.Text)
                /*&& !worker.IsBusy*/)
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
            if (backgroundWorkerThread != null) backgroundWorkerThread.Interrupt();
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
            if (!mainWindowState)
            {
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
                mainWindowState = true;
            }
            else
            {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                mainWindowState = false;
            }
        }

        private void DragNDropWindow(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.DragMove();
            }
            catch
            { }

        }

        private void dataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dataGrid.SelectedItem != null)
            {
                string path = (dataGrid.SelectedItem as FileAbout).FullPath;
                MainController.OpenInWinExplorer(path);
            }
        }
    }

}
