using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
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
             
    }

    
}
