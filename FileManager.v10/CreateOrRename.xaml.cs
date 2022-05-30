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
    /// <summary>
    /// Логика взаимодействия для CreateOrRename.xaml
    /// </summary>
    public partial class CreateOrRename : Window
    {
        FileSystemObjectInfo currentFileFromTree;
        FileAbout currentFileFromDataGrid = new FileAbout();

        public CreateOrRename(string name, object currentFile)
        {
            InitializeComponent();
            userInput.Text = name;
            if (currentFile != null)
            {
                if (Object.ReferenceEquals(currentFile.GetType(), currentFileFromDataGrid.GetType()))
                {
                    currentFileFromDataGrid = currentFile as FileAbout;
                    nameOfFile.Text = (currentFile as FileAbout).Name;
                }
                else 
                {
                    //Object.ReferenceEquals(currentFile.GetType(), currentFileFromTree.GetType())
                    //-- выдаёт null, т.к. FileSystemObjectInfo по умолчанию null
                    currentFileFromTree = currentFile as FileSystemObjectInfo;
                    nameOfFile.Text = (currentFile as FileSystemObjectInfo).FileSystemInfo.Name;
                }
            }
        }


        private void CloseWindow(object sender, RoutedEventArgs e)
        {

            if (!String.IsNullOrEmpty(nameOfFile.Text) && !String.IsNullOrWhiteSpace(nameOfFile.Text))
            {
                if (currentFileFromTree == null && currentFileFromDataGrid.FullPath == null)
                {
                    NamesFromAnotherWindow.name = nameOfFile.Text;
                    if (radioFile.IsChecked == true) NamesFromAnotherWindow.extension = (int)TypesForCreationFile.File;
                    else if (radioFolder.IsChecked == true) NamesFromAnotherWindow.extension = (int)TypesForCreationFile.Folder;
                }
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
}
}
