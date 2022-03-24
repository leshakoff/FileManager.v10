using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

namespace FileManager.v10.Models
{
    [Serializable]
    public abstract class PropertyNotifier : INotifyPropertyChanged
    {

        [field: NonSerialized()]
        public event PropertyChangedEventHandler PropertyChanged;


        public PropertyNotifier() : base()
        {
            this.AllowRaiseEvent = true;
        }


        [XmlIgnore]
        protected bool AllowRaiseEvent
        {
            get;
            set;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (this.AllowRaiseEvent)
            {
                if (!object.ReferenceEquals(this.PropertyChanged, null))
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
    }
}
