using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using static FileManager.v10.ShellManager;

namespace FileManager.v10.Models
{
    public class FileSystemObjectInfo : BaseObject
    {
        public FileSystemObjectInfo(FileSystemInfo info)
        {
            if (this is BranchFileSystemObjectInfo) return;

            this.Children = new ObservableCollection<FileSystemObjectInfo>();

            this.FileSystemInfo = info;

            if (info is DirectoryInfo)
            {
                this.ImageSource = FolderManager.GetImageSource(info.FullName, ItemState.Close);
                this.AddBranch();
            }
            else if (info is FileInfo)
            {
                this.ImageSource = FileManager.GetImageSource(info.FullName);
            }
            this.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(FileSystemObjectInfo_PropertyChanged);
        }

        public FileSystemObjectInfo(DriveInfo drive)
            : this(drive.RootDirectory)
        {
            this.Drive = drive;
        }


        // свойства:
        // коллекция "детей"/"веток"; 
        // изображение;
        // IsExpanded, развёрнуто или свёрнуто;
        // FileSystemInfo для обработки объекта FileInfo или DirectoryInfo
        // Drive, в том случае, если в данный момент мы просматриваем диск

        public ObservableCollection<FileSystemObjectInfo> Children
        {
            get { return base.GetValue<ObservableCollection<FileSystemObjectInfo>>("Children"); }
            private set { base.SetValue("Children", value); }
        }


        public ImageSource ImageSource
        {
            get { return base.GetValue<ImageSource>("ImageSource"); }
            private set { base.SetValue("ImageSource", value); }
        }

        public bool IsExpanded
        {
            get { return base.GetValue<bool>("IsExpanded"); }
            set { base.SetValue("IsExpanded", value); }
        }

        public FileSystemInfo FileSystemInfo
        {
            get { return base.GetValue<FileSystemInfo>("FileSystemInfo"); }
            private set { base.SetValue("FileSystemInfo", value); }
        }

        private DriveInfo Drive
        {
            get { return base.GetValue<DriveInfo>("Drive"); }
            set { base.SetValue("Drive", value); }
        }




        /// <summary>
        /// Добавляет объект в коллекцию дочерних элементов ("Children")
        /// </summary>
        private void AddBranch()
        {
            this.Children.Add(new BranchFileSystemObjectInfo());
        }


        /// <summary>
        /// Проверяет наличие объекта в свойстве Children
        /// </summary>
        /// <returns></returns>
        private bool HasBranch()
        {
            return !object.ReferenceEquals(this.GetBranch(), null);
        }

        /// <summary>
        /// Возвращает текущий добавленный объект BranchFileSystemObjectInfo
        /// </summary>
        /// <returns></returns>
        private BranchFileSystemObjectInfo GetBranch()
        {
            var list = this.Children.OfType<BranchFileSystemObjectInfo>().ToList();
            if (list.Count > 0) return list.First();
            return null;
        }

        /// <summary>
        /// Удаляет объект BranchFileSystemObjectInfo, который уже был добавлен в свойство Children 
        /// </summary>
        private void RemoveBranch()
        {
            this.Children.Remove(this.GetBranch());
        }



        private void ExploreDirectories()
        {
            if (!object.ReferenceEquals(this.Drive, null))
            {
                if (!this.Drive.IsReady) return;
            }
            try
            {
                if (this.FileSystemInfo is DirectoryInfo)
                {
                    var directories = ((DirectoryInfo)this.FileSystemInfo).GetDirectories();
                    foreach (var directory in directories.OrderBy(d => d.Name))
                    {
                        if (!object.Equals((directory.Attributes & FileAttributes.System), FileAttributes.System) &&
                            !object.Equals((directory.Attributes & FileAttributes.Hidden), FileAttributes.Hidden))
                        {
                            this.Children.Add(new FileSystemObjectInfo(directory));
                        }
                    }
                }
            }
            catch
            {
               
            }
        }

        private void ExploreFiles()
        {
            if (!object.ReferenceEquals(this.Drive, null))
            {
                if (!this.Drive.IsReady) return;
            }
            try
            {
                if (this.FileSystemInfo is DirectoryInfo)
                {
                    var files = ((DirectoryInfo)this.FileSystemInfo).GetFiles();
                    foreach (var file in files.OrderBy(d => d.Name))
                    {
                        if (!object.Equals((file.Attributes & FileAttributes.System), FileAttributes.System) &&
                            !object.Equals((file.Attributes & FileAttributes.Hidden), FileAttributes.Hidden))
                        {
                            this.Children.Add(new FileSystemObjectInfo(file));
                        }
                    }
                }
            }
            catch
            {
                
            }
        }

        /// <summary>
        /// Обработчик события PropertyChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FileSystemObjectInfo_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (this.FileSystemInfo is DirectoryInfo)
            {
                if (string.Equals(e.PropertyName, "IsExpanded", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (this.IsExpanded)
                    {
                        this.ImageSource = FolderManager.GetImageSource(this.FileSystemInfo.FullName, ItemState.Open);
                        if (this.HasBranch())
                        {
                            this.RemoveBranch();
                            this.ExploreDirectories();
                            this.ExploreFiles();
                        }
                    }
                    else
                    {
                        this.ImageSource = FolderManager.GetImageSource(this.FileSystemInfo.FullName, ItemState.Close);
                    }
                }
            }
        }
    }
}