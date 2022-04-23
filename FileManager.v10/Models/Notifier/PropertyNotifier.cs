using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

namespace FileManager.v10.Models
{
    /// <summary>
    /// Это класс, который наследует интерфейс INotifyPropertyChanged, 
    /// для того, чтобы реализовать полноценный механизм привязки данных. 
    /// https://metanit.com/sharp/wpf/11.2.php
    /// </summary>
    [Serializable]
    public abstract class PropertyNotifier : INotifyPropertyChanged
    {
        [field: NonSerialized()]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// В конструкторе по умолчанию указываем у AllowRaiseEvent true, 
        /// чтобы по умолчанию мы могли вызывать событие
        /// </summary>
        public PropertyNotifier() : base()
        {
            this.AllowRaiseEvent = true;
        }

        // это свойство используется для определения того, разрешено ли нам вызывать событие или нет.
        // изменять значение может только производный класс
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
                if (!object.ReferenceEquals(this.PropertyChanged, null)) // <- такая проверка на NULL
                                                                         // была использована в гайде,
                                                                         // так что почему бы и нет? ¯\_(ツ)_/¯
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
    }
}
