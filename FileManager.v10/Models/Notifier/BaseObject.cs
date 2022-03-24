using System;
using System.Collections.Generic;
using System.Text;

namespace FileManager.v10.Models
{
    public abstract class BaseObject : PropertyNotifier
    {

        private IDictionary<string, object> values = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);


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
