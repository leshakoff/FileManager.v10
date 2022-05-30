using System;
using System.Collections.Generic;
using System.Text;

namespace FileManager.v10
{
    /// <summary>
    /// Класс, предназначенный для передачи параметров из
    /// дополнительного окна WPF в главное. 
    /// </summary>
    public class NamesFromAnotherWindow
    {
        /// <summary>
        /// Имя создаваемого файла/Новое имя файла
        /// </summary>
        public static string name = "";
        /// <summary>
        /// Расширение.
        /// </summary>
        public static int extension = 0;

        /// <summary>
        /// Перечисление с расширениями
        /// </summary>
        public enum TypesForCreationFile
        { 
            File = 0,
            Folder = 1
        }
    }
}
