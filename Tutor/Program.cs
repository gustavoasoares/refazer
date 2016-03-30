using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython;
using IronPython.Compiler;
using IronPython.Compiler.Ast;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tutor
{
    class Program
    {
      

        static void Main(string[] args)
        {
            var report = new Report();
            report.GenerateReport();           
        }

      
    }

   
}
