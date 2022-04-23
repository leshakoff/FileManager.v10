using System;
using System.Collections.Generic;
using System.Text;

namespace FileManager.v10.Models
{
    /// <summary>
    /// Реализация базового объекта, который наследуется от PropertyNotifier
    /// </summary>
    public abstract class BaseObject : PropertyNotifier
    {
        /// <summary>
        /// Создаём словарь, в котором мы будем хранить значение свойств, 
        /// и используем CurrentCultureIgnoreCase, 
        /// чтобы проверка ключей словаря была нечувствительна к регистру. 
        /// </summary>
        private IDictionary<string, object> values = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);


        // следующие функции получают или устанавливают свойства. 
        // и Get- метод перегруженный, 
        // так как это обеспечивает типобезопасность 
        public T GetValue<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return default(T);
            }
            var value = this.GetValue(key);
            if (value is T)
            {
                return (T)value;
            }
            return default(T);
        }

        private object GetValue(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            if (this.values.ContainsKey(key))
            {
                return this.values[key];
            }
            return null;
        }

        // в методе Set- мы вызываем событие OnPropertyChanged, из класса PropertyNotifier, 
        // чтобы уведомить о событии PropertyChanged для текущего свойства
        public void SetValue(string key, object value)
        {
            if (!this.values.ContainsKey(key))
            {
                this.values.Add(key, value);
            }
            else
            {
                this.values[key] = value;
            }
            base.OnPropertyChanged(key);
        }

    }
}
