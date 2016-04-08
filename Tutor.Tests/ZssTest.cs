using System;
using IronPython;
using IronPython.Compiler;
using IronPython.Compiler.Ast;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.Scripting.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tutor.Tests
{
    [TestClass]
    public class ZssTest
    {
        [TestMethod]
        public void TestCompute1()
        {
            var py = Python.CreateEngine();
            var ast1 = NodeWrapper.Wrap(ParseContent("k = 0", py));
            var ast2 = NodeWrapper.Wrap(ParseContent("k = 1", py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(1, editDistance.Item1);
        }

        [TestMethod]
        public void TestCompute2()
        {
            var py = Python.CreateEngine();
            var ast1 = NodeWrapper.Wrap(ParseContent("total, k = 0, 1", py));
            var ast2 = NodeWrapper.Wrap(ParseContent("total, k = 1, 1", py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(1, editDistance.Item1);
        }

        [TestMethod]
        public void TestCompute3()
        {
            var py = Python.CreateEngine();
            var ast1 = NodeWrapper.Wrap(ParseContent("x * a", py));
            var ast2 = NodeWrapper.Wrap(ParseContent("term(x) * a", py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(3, editDistance.Item1);
        }

        [TestMethod]
        public void TestCompute4()
        {
            var py = Python.CreateEngine();
            var ast1 = NodeWrapper.Wrap(ParseFile(Environment.CurrentDirectory + "../../../resources/before_1.py", py));
            var ast2 = NodeWrapper.Wrap(ParseFile(Environment.CurrentDirectory + "../../../resources/after_1.py", py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(1, editDistance.Item1);

        }

        private PythonAst ParseContent(string content, ScriptEngine py)
        {
            var src = HostingHelpers.GetSourceUnit(py.CreateScriptSourceFromString(content));
            return Parse(py, src);
        }

        private PythonAst ParseFile(string path, ScriptEngine py)
        {
            var src = HostingHelpers.GetSourceUnit(py.CreateScriptSourceFromFile(path));
            return Parse(py, src);
        }

        private PythonAst Parse(ScriptEngine py, SourceUnit src)
        {
            var pylc = HostingHelpers.GetLanguageContext(py);
            var parser = Parser.CreateParser(new CompilerContext(src, pylc.GetCompilerOptions(), ErrorSink.Default),
                (PythonOptions)pylc.Options);
            return parser.ParseFile(true);
        }
    }
}
