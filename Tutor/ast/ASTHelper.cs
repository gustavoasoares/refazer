using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting;
using System.ServiceModel;
using System.ServiceProcess;
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

        //public static JSAst ParseFileJS(string path)
        //{
        //    var js = //Python.CreateEngine();
        //    var src = HostingHelpers.GetSourceUnit(js.CreateScriptSourceFromFile(path));
        //    return Parse(src, js);
        //}

        public static PythonAst ParseContent(string content)
        {
            var py = Python.CreateEngine();
            var src = HostingHelpers.GetSourceUnit(py.CreateScriptSourceFromString(content));
            return Parse(src, py);
        }

        //public static JSAst ParseContentJS(string content)
        //{
        //    var hs = //Python.CreateEngine();
        //    var src = HostingHelpers.GetSourceUnit(js.CreateScriptSourceFromString(content));
        //    return Parse(src, js);
        //}

        private static PythonAst Parse(SourceUnit src, ScriptEngine py)
        {
            var pylc = HostingHelpers.GetLanguageContext(py);
            var compilerContext = new CompilerContext(src, pylc.GetCompilerOptions(), ErrorSink.Default);
            var parser = Parser.CreateParser(compilerContext,
                (PythonOptions)pylc.Options);
            return parser.ParseFile(true);
        }

        //private static JSAst ParseJS(SourceUnit src, ScriptEngine js)
        //{
            
        //}

        //private static bool _timeout = false;
        private static readonly Uri ServiceUri = new Uri("net.pipe://localhost/Pipe");
        private const string PipeName = "TutorGradeService";
        private static readonly EndpointAddress ServiceAddress = 
            new EndpointAddress(string.Format(CultureInfo.InvariantCulture, 
                "{0}/{1}", ServiceUri.OriginalString, PipeName));

    }

    
}
