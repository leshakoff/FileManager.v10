using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            var thread = new Thread(new ParameterizedThreadStart(DoCalck));
            thread.Start(cancellationTokenSource);
        }

        private void DoCalck(object state)
        {
            CancellationTokenSource source = (CancellationTokenSource)state;
            /**
             * 
             */

            //foreach(var file in _locations)
            //{
            //    file.Size = GetFormattedSize(file, sizeCache);
            //}

            NotifyPropertyChanged("Locations");
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

        private void NotifyPropertyChanged([CallerMemberName] string member = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(member));
        }
    }
}
