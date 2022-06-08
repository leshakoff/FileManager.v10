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

        public static CancellationTokenSource cts =
            new CancellationTokenSource();                            // TokenSource для отмены таски,
                                                                      // в которой ведётся поиск файлов

        public CancellationTokenSource ctsForTaskChilds =             // TokenSource для отмены
            new CancellationTokenSource();                            // внутренних тасков, которые  
                                                                      // запускает таск для поиска файлов

        public CancellationToken token = cts.Token;                   // токен для отмены таски, в которой
                                                                      // ведётся поиск файлов. 

        bool mainWindowState = false;                                 // "переключатель" для корректной
                                                                      // работы кнопки "развернуть" окно.
                                                                      // По умолчанию - false;  если окно развёрнуто
                                                                      // - true.

        public bool isMainList = false;                               

        public bool IsMainList                                        // свойство, которое указывает, 
        {                                                             // является ли текущий список итемов
            get { return isMainList; }                                // в датагриде основным - т.е. 
            set                                                       // если это просто список, раскрытый из ветки
            {                                                         // TreeView, то этот список будет считаться
                isMainList = value;                                   // главным (isMainList = true).
                                                                      // Если же это список, полученный из 
                if (isMainList) ChangeButtonsEnabled();               // функции поиска, то isMainList = false; 
                else ChangeButtonsDisabled();                         // Сделано это для того, чтобы предоставить  
            }                                                         // для текущих файлов функционал. Если файлы 
                                                                      // находятся не в главном списке, то с ними
        }                                                             // всё ещё можно проделывать операции 
                                                                      // переименования, копирования, удаления и т.д.
                                                                      // Но создать файл уже не получится, так как 
                                                                      // у результата поиска нет какой-то "своей" папки, 
                                                                      // так как все найденные файлы могут находиться 
                                                                      // в разных папках. Если же список главный, 
                                                                      // то в текущей папке мы можем создать новый файл.


        public MainWindow()
        {
            InitializeComponent();

            // заполняем TreeView
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (drive.IsReady) this.treeView.Items.Add(new FileSystemObjectInfo(drive));
            }

            // создаём модель данных, из которых
            // мы потом будем получать текущий список для DataGrid-a
            var model = new IndexViewModel();
            DataContext = model;

            CheckDataGridItems();
        }

        /// <summary>
        /// Метод, предзназначенный для проверки Датагрида на итемы.
        /// Если Датагрид не пустой, отображаем его. Если итемов нет, 
        /// прячем. 
        /// </summary>
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

            string pathForDG = "";

            if (treeView.SelectedItem != null)
            {
                pathForDG = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
                spinnerGif.Visibility = Visibility.Visible;

                (DataContext as IndexViewModel).LoadData(pathForDG).ContinueWith(r =>
                {
                    Boop.Text = String.Empty;
                    spinnerGif.Visibility = Visibility.Collapsed;

                    CheckDataGridItems();

                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

        }

        private void Search(object sender, RoutedEventArgs e)
        {
            TimeSpan stopwatch;
            //long quantity;


            string search = $"*{searchString.Text}*";
            string path = searchLocation.SelectedItem
                .ToString()
                .Replace("System.Windows.Controls.ComboBoxItem: ", "");

            try
            {

                Stopwatch sp = new Stopwatch();
                sp.Start();

                if (!String.IsNullOrEmpty(search) && !String.IsNullOrWhiteSpace(search))
                {

                    IsMainList = false;
                    btnClick.IsEnabled = false;
                    cancelSearchButton.IsEnabled = true;
                    pbStatus.Visibility = Visibility.Visible;

                    Boop.Text = $"Выполняется поиск в {path} по запросу: {search}...";

                    pbStatus.IsIndeterminate = true;
 
                    CancellationToken tok = ctsForTaskChilds.Token;


                    (DataContext as IndexViewModel).LoadData(search, path, tok).ContinueWith(r =>
                    {

                        sp.Stop();
                        stopwatch = sp.Elapsed;

                        //quantity = dataGrid.Items.Count;

                        if (dataGrid.Items.Count == 0)
                        {
                            Boop.Text = $"Упс... По вашему запросу ничего не найдено";

                        }
                        else
                        {
                            Boop.Text = $"Поиск завершён! \n" +
                                        $" Прошло: " +
                                        $" : {stopwatch.Minutes} мм " +
                                        $" : {stopwatch.Seconds} сс " +
                                        $" : {stopwatch.Milliseconds} мс ";
                        }


                        cancelSearchButton.IsEnabled = false;
                        pbStatus.IsIndeterminate = false;
                        pbStatus.Visibility = Visibility.Hidden;

                        CheckDataGridItems();


                    }, token, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());


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


            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;


                ctsForTaskChilds.Cancel();
            }


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
                catch
                {
                    MessageBox.Show($"Cоздать файл/папку не удалось.", "Ошибка создания файла/папки",
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
            if (dataGrid.SelectedItem != null && (dataGrid.SelectedItem as FileAbout).Extension != "Папка")
            {
                MainController.OpenFileWithStandartProgram((dataGrid.SelectedItem as FileAbout).FullPath);
            }
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
        }

        private void CheckElementExtension(object sender, MouseButtonEventArgs e)
        {
            if (dataGrid.SelectedItem != null && (dataGrid.SelectedItem as FileAbout).Extension != "Папка")
            {
                readFileBtn.IsEnabled = true;
            }
            else
            {
                readFileBtn.IsEnabled = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowBlureEffect wp = new WindowBlureEffect(this, AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND);
        }

    }

}
