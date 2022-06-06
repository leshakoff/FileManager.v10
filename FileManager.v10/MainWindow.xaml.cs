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
using static FileManager.v10.NamesFromAnotherWindow;
using System.Text.RegularExpressions;

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

        public bool isMainList = false;

        public bool IsMainList
        {
            get { return isMainList; }
            set
            {
                isMainList = value;
                if (isMainList) ChangeButtonsEnabled();
                else ChangeButtonsDisabled();
            }
        }

        public string pathForDG = "";


        public List<FileAbout> aboutAll = new List<FileAbout>();    // временный список, в котором мы будем хранить
                                                                    // найденные файлы/папки из BackgroundWorkera. 


        public MainWindow()
        {
            InitializeComponent();

            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (drive.IsReady) this.treeView.Items.Add(new FileSystemObjectInfo(drive));
            }

            var model = new IndexViewModel();
            DataContext = model;

            CheckDataGridItems();
        }

        private void CheckDataGridItems()
        {
            if (dataGrid.Items.Count > 0)
                dataGrid.Visibility = Visibility.Visible;
            else
                dataGrid.Visibility = Visibility.Collapsed;
        }

        private void UpdateTreeView(string pathForExpand = null)
        {
            this.treeView.Items.Clear();



            CheckDataGridItems();

            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (drive.IsReady) this.treeView.Items.Add(new FileSystemObjectInfo(drive));
            }

            if (!String.IsNullOrEmpty(pathForExpand))
            {


                string addSlashes = @"\";

                string[] nodes = pathForExpand.Split('\\');

                for (int k = 0; k < nodes.Length; k++)
                {
                    if (k == 0)
                        nodes[k] += addSlashes;
                    else
                    {
                        nodes[k] = nodes[k - 1].TrimEnd('\\') + addSlashes + nodes[k];
                    }
                }


                int i = treeView.Items.Count;

                FileSystemObjectInfo mainNode;

                for (int j = 0; j < i; j++)
                {
                    if ((treeView.Items[j] as FileSystemObjectInfo).FileSystemInfo.FullName == nodes[0])
                    {
                        mainNode = treeView.Items[j] as FileSystemObjectInfo;
                        mainNode.IsExpanded = true;

                        if (mainNode.Children.Count > 0 && nodes.Length > 1)
                        {
                            FileSystemObjectInfo b = mainNode.Children
                                .First(x => x.FileSystemInfo.FullName == nodes[1]);
                            b.IsExpanded = true;

                            for (int k = 1; k < nodes.Length; k++)
                            {
                                if (k != nodes.Length - 1)
                                {
                                    ExpandChildren(b, nodes[k + 1]);
                                }
                            }

                        }

                    }

                }


            }
        }

        private void ExpandChildren(FileSystemObjectInfo mainNode, string childrenName)
        {
            if (mainNode.Children.Count > 0)
            {
                FileSystemObjectInfo childNode = mainNode.Children.First(x => x.FileSystemInfo.FullName == childrenName);
                childNode.IsExpanded = true;
            }
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
            IsMainList = true;

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

                    CheckDataGridItems();

                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

        }

        private void StartSearch(object sender, RoutedEventArgs e)
        {
            IsMainList = false;
            btnClick.IsEnabled = false;
            cancelSearchButton.IsEnabled = true;
            pbStatus.Visibility = Visibility.Visible;
            WorkerParam wp = new WorkerParam(searchString.Text, searchLocation.SelectedItem
                .ToString()
                .Replace("System.Windows.Controls.ComboBoxItem: ", ""));
            //if (!worker.IsBusy) worker.RunWorkerAsync(wp);
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
                BackgroundWorker bg = sender as BackgroundWorker;
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
                && !String.IsNullOrWhiteSpace(searchString.Text))
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

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dataGrid.SelectedItem != null)
            {
                string path = (dataGrid.SelectedItem as FileAbout).FullPath;
                MainController.OpenInWinExplorer(path);
            }
        }

        private void CreateFile(object sender, RoutedEventArgs e)
        {
            if (treeView.SelectedItem != null)
            {
                string path = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;

                CreateOrRename create = new CreateOrRename("Введите название и расширение файла или название папки", null);
                create.Owner = this;
                create.ShowDialog();

                try
                {
                    if (NamesFromAnotherWindow.extension == (int)TypesForCreationFile.Folder)
                    {
                        if (Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path + @"\" + NamesFromAnotherWindow.name);
                            ShowMessageBox("Папка", path + @"\" + NamesFromAnotherWindow.name, "создана");
                        }
                        else
                        {
                            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) + @"\" + NamesFromAnotherWindow.name);
                            ShowMessageBox("Папка",
                                System.IO.Path.GetDirectoryName(path) + @"\" + NamesFromAnotherWindow.name,
                                "создана");
                        }
                    }
                    else
                    {
                        if (Directory.Exists(path))
                        {
                            var file = File.Create(path + @"\" + NamesFromAnotherWindow.name);
                            file.Close();
                            ShowMessageBox("Файл", path + @"\" + NamesFromAnotherWindow.name, "создан");
                        }
                        else
                        {
                            string newPath = System.IO.Path.GetDirectoryName(path);
                            var file = File.Create(newPath + @"\" + NamesFromAnotherWindow.name);
                            file.Close();
                            ShowMessageBox("Файл", newPath + @"\" + NamesFromAnotherWindow.name, "создан");
                        }
                    }
                }
                catch (PathTooLongException)
                {
                    MessageBox.Show("Название файла/папки слишком длинное, " +
                        "создать файл/папку не удалось.", "Ошибка создания файла/папки",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Cоздать файл/папку не удалось. {ex.Message}", "Ошибка создания файла/папки",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
        }

        private void ShowMessageBox(string obj, string path, string operation)
        {
            MessageBox.Show($"{obj} по пути: {path} успешно {operation}",
                 $"{obj} {operation}", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RenameFile(object sender, RoutedEventArgs e)
        {
            if (treeView.SelectedItem != null)
            {
                CreateOrRename create;

                string path = "";
                try
                {
                    if (dataGrid.SelectedItem == null)
                    {
                        path = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
                        create = new CreateOrRename("Введите новое имя файла или папки",
                                (treeView.SelectedItem as FileSystemObjectInfo));
                    }
                    else
                    {
                        path = (dataGrid.SelectedItem as FileAbout).FullPath;
                        create = new CreateOrRename("Введите новое имя файла или папки",
                                (dataGrid.SelectedItem as FileAbout));
                    }


                    create.Owner = this;
                    create.ShowDialog();

                    if (NamesFromAnotherWindow.extension == (int)TypesForCreationFile.Folder)
                    {
                        string newPath = System.IO.Path.GetDirectoryName(path) + @"\" + NamesFromAnotherWindow.name;
                        Directory.Move(path, newPath);
                        ShowMessageBox("Папка",
                            System.IO.Path.GetDirectoryName(path) + @"\" + NamesFromAnotherWindow.name,
                            "переименована");
                    }
                    else
                    {
                        string newPath = System.IO.Path.GetDirectoryName(path) + @"\" + NamesFromAnotherWindow.name;
                        File.Move(path, newPath);
                        ShowMessageBox("Файл", path + @"\" + NamesFromAnotherWindow.name, "переименован");
                    }

                    UpdateTreeView(System.IO.Path.GetDirectoryName(path));
                }
                catch
                {
                    MessageBox.Show("Переименовать файл/папку не удалось.", "Ошибка переименования файла/папки",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }


            }
        }

        private void CopyFile(object sender, RoutedEventArgs e)
        {
            if (treeView.SelectedItem != null)
            {

                string path = "";
                string fileName = "";
                string extension = "";
                try
                {
                    if (dataGrid.SelectedItem == null)
                    {
                        path = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
                        fileName = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.Name;
                        extension = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.Extension;
                    }
                    else
                    {
                        path = (dataGrid.SelectedItem as FileAbout).FullPath;
                        fileName = (dataGrid.SelectedItem as FileAbout).Name;
                        extension = (dataGrid.SelectedItem as FileAbout).Extension;
                    }

                    int index = 0;

                    Regex r = new Regex(@"\([\d]+\)");
                    MatchCollection matches = r.Matches(fileName);
                    if (matches.Count > 0)
                    {
                        Match m = matches[matches.Count - 1];
                        index = Int32.Parse(m.Value.Trim('.', ')', '('));
                    }

                    if (index != 0)
                    {
                        if (Directory.Exists(path))
                        {
                            string newName = fileName.Replace($"({index})", "");
                            newName += $"({++index})";
                            MainController.CopyDirectory($"{path}", $"{System.IO.Path.GetDirectoryName(path)}\\{newName}");
                            ShowMessageBox("Папка", path, "скопирована");
                        }
                        else if (File.Exists(path))
                        {
                            string newName = System.IO.Path.GetFileNameWithoutExtension(path);
                            newName = newName.Replace($"({index})", "");
                            newName += $"({++index}){extension}";
                            File.Copy(path, $"{System.IO.Path.GetDirectoryName(path)}\\{newName}");
                            ShowMessageBox("Файл", path, "скопирован");
                        }
                    }
                    else
                    {
                        if (Directory.Exists(path))
                        {
                            MainController.CopyDirectory($"{path}", $"{path}({++index})");
                            ShowMessageBox("Папка", path, "скопирована");
                        }
                        else if (File.Exists(path))
                        {
                            File.Copy(path, $"{System.IO.Path.GetDirectoryName(path)}" +
                                $"\\{System.IO.Path.GetFileNameWithoutExtension(path)}" +
                                $"({++index})" +
                                $"{extension}");
                            ShowMessageBox("Файл", path, "скопирован");
                        }
                    }

                    UpdateTreeView(System.IO.Path.GetDirectoryName(path));

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Копировать файл/папку не удалось. {ex.Message}", "Ошибка копирования файла/папки",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteFile(object sender, RoutedEventArgs e)
        {
            if (treeView.SelectedItem != null)
            {

                string path = "";
                try
                {
                    if (dataGrid.SelectedItem == null)
                    {
                        path = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;

                    }
                    else
                    {
                        path = (dataGrid.SelectedItem as FileAbout).FullPath;
                    }

                    if (Directory.Exists(path))
                    {
                        bool res = ShowMessageBoxForDeleting(path, "папку");
                        if (res)
                        {
                            Directory.Delete(path, true);
                            ShowMessageBox("Папка", path, "удалена");
                        }
                    }
                    else if (File.Exists(path))
                    {
                        bool res = ShowMessageBoxForDeleting(path, "файл");
                        if (res)
                        {
                            File.Delete(path);
                            ShowMessageBox("Файл", path, "удалён");
                        }
                    }

                    UpdateTreeView(System.IO.Path.GetDirectoryName(path));

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Удалить файл/папку не удалось. {ex.Message}", "Ошибка удаления файла/папки",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool ShowMessageBoxForDeleting(string path, string obj)
        {
            MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить {obj} по пути {path}?",
                 "Подтвердите удаление", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) return true;
            else return false;

        }

        private void ReadFile(object sender, RoutedEventArgs e)
        {

        }

        private void ChangeButtonsEnabled()
        {
            createFileBtn.IsEnabled = true;
            deleteFileBtn.IsEnabled = true;
            renameFileBtn.IsEnabled = true;
            copyFileBtn.IsEnabled = true;
        }

        private void ChangeButtonsDisabled()
        {
            createFileBtn.IsEnabled = false;
            //deleteFileBtn.IsEnabled = false;
            //renameFileBtn.IsEnabled = false;
            //copyFileBtn.IsEnabled = false;
        }

        private void CheckElementExtension(object sender, MouseButtonEventArgs e)
        {
            string ext = "";
            if (dataGrid.SelectedItem != null)
            {

                ext = (dataGrid.SelectedItem as FileAbout).Extension;
                MessageBox.Show($"Мы здесь, расширение - {ext}");

                if (ext == ".txt")
                {
                    //readFileBtn.IsEnabled = true;
                    //editFileBtn.IsEnabled = true;

                    //playMediaFileBtn.IsEnabled = false;
                    //pauseMediaFileBtn.IsEnabled = false;
                    //stopMediaFileBtn.IsEnabled = false;
                }
                else if (ext == ".mp4")
                {
                    //playMediaFileBtn.IsEnabled = true;
                    //pauseMediaFileBtn.IsEnabled = true;
                    //stopMediaFileBtn.IsEnabled = true;

                    //readFileBtn.IsEnabled = false;
                    //editFileBtn.IsEnabled = false;
                }
                else
                {
                    //readFileBtn.IsEnabled = false;
                    //editFileBtn.IsEnabled = false;
                    //playMediaFileBtn.IsEnabled = false;
                    //pauseMediaFileBtn.IsEnabled = false;
                    //stopMediaFileBtn.IsEnabled = false;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowBlureEffect wp = new WindowBlureEffect(this, AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND);
        }
    }

}
