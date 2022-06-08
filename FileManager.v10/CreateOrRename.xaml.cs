using FileManager.v10.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static FileManager.v10.NamesFromAnotherWindow;

namespace FileManager.v10
{
    public partial class CreateOrRename : Window
    {

        FileSystemObjectInfo currentFileFromTree;               // объявляем две глобальных переменных,
                                                                // FileSystemObjectInfo - если файл, переданный в это окно,
                                                                // был из TreeView, 
        FileAbout currentFileFromDataGrid = new FileAbout();    // и FileAbout, если файл был передан из DataGrid. 


        /// <summary>
        /// </summary>
        /// <param name="name">Тайтл с подсказкой пользователю, 
        /// что мы будем делать - переименовывать файл или 
        /// создаввать новый.</param>
        /// <param name="currentFile">В случае, если какой-либо файл/папку 
        /// необходимо переименовать, передаём файл. 
        /// Если файл/папка создаются, передаём null.</param>
        public CreateOrRename(string name, object currentFile)
        {
            InitializeComponent();

            userInput.Text = name;          // помещаем тайтл в TextBlock


            if (currentFile != null)        // если currentFile - не null, значит, нам нужно переименовать файл/папку.
            {
                // скрываем радиокнопки, т.к. при переименовании они нам не нужны

                radioFile.Visibility = Visibility.Hidden;
                radioFolder.Visibility = Visibility.Hidden;


                // выясняем, какого типа файл нам передан в конструктор, 
                // и присваиваем его значение одной из глобальных переменных. 
                // в текстбокс передаём текущее название файла/папки
                if (Object.ReferenceEquals(currentFile.GetType(), currentFileFromDataGrid.GetType()))
                {
                    currentFileFromDataGrid = currentFile as FileAbout;
                    nameOfFile.Text = (currentFile as FileAbout).Name;
                }
                else
                {
                    currentFileFromTree = currentFile as FileSystemObjectInfo;
                    nameOfFile.Text = (currentFile as FileSystemObjectInfo).FileSystemInfo.Name;
                }
            }
            else
            {
                // если же это не операция переименования,
                // показываем радиокнопки, чтобы пользователь мог выбрать:
                // создаём мы файл, либо папку
                radioFile.Visibility = Visibility.Visible;
                radioFolder.Visibility = Visibility.Visible;
            }
        }


        private void DoneOperations(object sender, RoutedEventArgs e)
        {
            // проверяем, не пуста ли строка с новым именем
            if (!String.IsNullOrEmpty(nameOfFile.Text) && !String.IsNullOrWhiteSpace(nameOfFile.Text))
            {
                // если обе глобальных переменных равны null, создаём новый файл; 
                // для передачи данных из одного окна в другое используем вспомогательный класс 
                // NamesFromAnotherWindow, куда помещаем имя нового файла и его расширение - т.е.
                // файл это или папка
                if (currentFileFromTree == null && currentFileFromDataGrid.FullPath == null)
                {
                    NamesFromAnotherWindow.name = nameOfFile.Text;
                    if (radioFile.IsChecked == true) 
                        NamesFromAnotherWindow.extension = (int)TypesForCreationFile.File;

                    else if (radioFolder.IsChecked == true) 
                        NamesFromAnotherWindow.extension = (int)TypesForCreationFile.Folder;
                }

                // если какая-то из переменных не null, проверяем существование такой директории/файла,
                // и похожим образом передаём в NamesFromAnotherWindow расширение и новое имя. 
                else
                {
                    if (currentFileFromTree != null)
                    {
                        if (File.Exists(currentFileFromTree.FileSystemInfo.FullName))
                        {
                            NamesFromAnotherWindow.extension = (int)TypesForCreationFile.File;
                            NamesFromAnotherWindow.name = nameOfFile.Text;
                        }
                        else if (Directory.Exists(currentFileFromTree.FileSystemInfo.FullName))
                        {
                            NamesFromAnotherWindow.extension = (int)TypesForCreationFile.Folder;
                            NamesFromAnotherWindow.name = nameOfFile.Text;
                        }
                    }
                    else if (currentFileFromDataGrid != null)
                    {
                        if (File.Exists(currentFileFromDataGrid.FullPath))
                        {
                            NamesFromAnotherWindow.extension = (int)TypesForCreationFile.File;
                            NamesFromAnotherWindow.name = nameOfFile.Text;
                        }
                        else if (Directory.Exists(currentFileFromDataGrid.FullPath))
                        {
                            NamesFromAnotherWindow.extension = (int)TypesForCreationFile.Folder;
                            NamesFromAnotherWindow.name = nameOfFile.Text;
                        }
                    }


                }

                this.Close();
            }
            else
            {
                MessageBox.Show("Имя файла не может быть пустым, повторите попытку",
                    "Ошибка создания файла",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void CloseWithoutChanges(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Метод, позволяющий "двигать" окошко, зажав кнопку мыши
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}
