using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IronPython;
using IronPython.Compiler;
using IronPython.Compiler.Ast;
using IronPython.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.Scripting.Runtime;

namespace Tutor
{
    public class ASTHelper
    {

        public static PythonAst ParseFile(string path)
        {
            var py = Python.CreateEngine();
            var src = HostingHelpers.GetSourceUnit(py.CreateScriptSourceFromFile(path));
            return Parse(src, py);
        }

        public static PythonAst ParseContent(string content)
        {
            var py = Python.CreateEngine();
            var src = HostingHelpers.GetSourceUnit(py.CreateScriptSourceFromString(content));
            return Parse(src, py);
        }

        private static PythonAst Parse(SourceUnit src, ScriptEngine py)
        {
            var pylc = HostingHelpers.GetLanguageContext(py);
            var compilerContext = new CompilerContext(src, pylc.GetCompilerOptions(), ErrorSink.Default);
            var parser = Parser.CreateParser(compilerContext,
                (PythonOptions)pylc.Options);
            return parser.ParseFile(true);
        }


        private static dynamic result;
        private static string script;

        public static dynamic Run(string s)
        {
            script = s;
            Worker workerObject = new Worker(s);
            Thread workerThread = new Thread(workerObject.DoWork, 100000000);

            try
            {
                workerThread.Start();
                if (!workerThread.Join(TimeSpan.FromMilliseconds(300)))
                {
                    workerObject.RequestStop();
                    workerThread.Abort();
                    GC.Collect();
                    result = 0;
                    throw new Exception("More than 1 secs.");
                }
                result = workerObject.Result;
            }
            catch (OutOfMemoryException e)
            {
                Console.Out.WriteLine("--------------------------------------------- Exception: " + e);
                workerObject.RequestStop();
                workerThread.Abort();
                result = 0;
                GC.Collect();
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("--------------------------------------------- Exception: " + e);
                workerObject.RequestStop();
                workerThread.Abort();
                result = 0;
                GC.Collect();
            }

            return result;
        }

        
        //private static void Execute()
        //{
        //    try
        //    {
        //        Dictionary<String, Object> options = new Dictionary<string, object>();
        //        options["LightweightScopes"] = true;
        //        py = Python.CreateEngine(options);
        //        ScriptScope scope = py.Runtime.CreateScope();
        //        var source = py.CreateScriptSourceFromString(script);
        //        var comped = source.Compile();
        //        result = comped.Execute(scope);
        //        py.Runtime.Shutdown();
        //    }
        //    catch (Exception)
        //    {
        //        result = 0;
        //    }
        //}
    }

    public class Worker
    {
        private readonly string _script;

        public dynamic Result { get; set; }
        public Worker(string script)
        {
            _script = script;
            py = Python.CreateEngine();
        }

        private ScriptEngine py;

        // This method will be called when the thread is started. 
        public void DoWork()
        {
            try
            {
                var options = new Dictionary<string, object>();
                options["LightweightScopes"] = true;
                py = Python.CreateEngine(options);
                var source = py.CreateScriptSourceFromString(_script);
                var comped = source.Compile();
                Result = comped.Execute();
                py.Runtime.Shutdown();
            }
            catch (Exception)
            {
                py.Runtime.Shutdown();
                Result = 0;
            }
        }
        public void RequestStop()
        {
            py.Runtime.Shutdown();
        }

        public bool ShouldContinue()
        {
            StackTrace s = new StackTrace();
            if (s.FrameCount > 50)
                return false;
            return true;
        }
    }
}
