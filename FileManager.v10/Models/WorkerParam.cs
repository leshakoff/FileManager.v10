using System;
using System.Collections.Generic;
using System.Text;

namespace FileManager.v10
{
    /// <summary>
    /// Класс, предназначенный для того, чтобы передать параметры BackgroundWorker'у. 
    /// </summary>
    public class WorkerParam
    {
        public string param;
        public string sourcePath;

        public WorkerParam(string p, string source)
        {
            param = p;
            sourcePath = source;
        }
    }
}
