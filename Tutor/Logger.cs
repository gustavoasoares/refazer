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

        private string _logFile = "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/log.txt"; 
        public void Log(string message)
        {
            File.AppendAllText(_logFile, message);
        }
    }
}
