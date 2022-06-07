using FastSearchLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.v10.Models
{
    public class IndexViewModel : INotifyPropertyChanged
    {

        private ObservableCollection<FileAbout> _locations;

        public event PropertyChangedEventHandler PropertyChanged;

        private CancellationTokenSource cancellationTokenSource = null;

        public ObservableCollection<FileAbout> Locations
        {
            get { return _locations; }
            set
            {
                if (_locations != value)
                {
                    _locations = value;
                    NotifyPropertyChanged();
                    CalculateFolderSizes();
                }
            }
        }

        private void CalculateFolderSizes()
        {
            cancellationTokenSource = new CancellationTokenSource();
            var thread = new Thread(new ParameterizedThreadStart(DoCalc));
            thread.Start(cancellationTokenSource);
        }

        private void DoCalc(object state)
        {
            CancellationTokenSource source = (CancellationTokenSource)state;

            source.Cancel();


            foreach (var file in _locations)
            {
                if (file.Extension == "Папка")
                {
                    file.Size = MainController.GetSize(GetFileSizeSumFromDirectory(file.FullPath));
                }

            }

            NotifyPropertyChanged("Locations");
        }

        public static long GetFileSizeSumFromDirectory(string searchDirectory)
        {
            try
            {
                var files = Directory.EnumerateFiles(searchDirectory);

                var currentSize = (from file in files let fileInfo = new FileInfo(file) select fileInfo.Length).Sum();

                var directories = Directory.EnumerateDirectories(searchDirectory);

                var subDirSize = (from directory in directories select GetFileSizeSumFromDirectory(directory)).Sum();

                return currentSize + subDirSize;
            }
            catch
            {
                return 0;
            }

        }

        public Task LoadData(string location)
        {
            if (cancellationTokenSource != null &&
                !cancellationTokenSource.IsCancellationRequested)
                cancellationTokenSource.Cancel();

            var t = Task.Factory.StartNew(() =>
            {
                return MainController.GetList(location);
            });

            t.ContinueWith(t =>
            {
                Locations = new ObservableCollection<FileAbout>(t.Result);
            }).ConfigureAwait(false);

            return t;
        }

        public Task LoadData(string search, string path, CancellationToken token)
        {
            var t = Task.Factory.StartNew(() =>
            {
                Task<List<FileInfo>> files = FileSearcher.GetFilesFastAsync(path, search);

                Task<List<DirectoryInfo>> dirs = DirectorySearcher.GetDirectoriesFastAsync(path, search);

                return MainController.GetList(dirs.Result, files.Result);
            }, token);


            t.ContinueWith(t =>
            {
                Locations = new ObservableCollection<FileAbout>(t.Result);
            }, TaskContinuationOptions.NotOnCanceled).ConfigureAwait(false);

            return t;
        }

        private void NotifyPropertyChanged([CallerMemberName] string member = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(member));
        }
    }
}
