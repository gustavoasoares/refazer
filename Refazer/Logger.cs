using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutor
{
    public class Logger
    {
        public static Logger Instance { get;  } = new Logger();

        private string _logFile = "log.txt"; 
        public void Log(string message)
        {
            File.AppendAllText(_logFile, message);
        }
    }
}
