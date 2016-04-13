using System;
using System.Linq;
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
            Assert.AreEqual(2, editDistance.Item1);

        }

        [TestMethod]
        public void TestCompute5()
        {
            var py = Python.CreateEngine();
            var before = @"
def product(n, term):
    if n == 0:
        return term(0)
    else:
        return term(n) * product((n - 1), term)";  
            var ast1 = NodeWrapper.Wrap(ParseContent(before, py));
            var after = @"
def product(n, term):
    if n == 1:
        return term(1)
    else:
        return term(n) * product((n - 1), term)";
            var ast2 = NodeWrapper.Wrap(ParseContent(after, py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(2, editDistance.Item1);
        }

        [TestMethod]
        public void TestCompute6()
        {
            var py = Python.CreateEngine();
            var before = @"term(0)";
            var ast1 = NodeWrapper.Wrap(ParseContent(before, py));
            var after = @"term(1)";
            var ast2 = NodeWrapper.Wrap(ParseContent(after, py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(1, editDistance.Item1);
        }

        [TestMethod]
        public void TestCompute7()
        {
            var py = Python.CreateEngine();
            var before = @"
def product(n, term):
    product(n-1)";
            var ast1 = NodeWrapper.Wrap(ParseContent(before, py));
            var after = @"
def product(n, term):
    product(n-1, term)";
            var ast2 = NodeWrapper.Wrap(ParseContent(after, py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(2, editDistance.Item1);
            Assert.AreEqual("NameExpression", editDistance.Item2.First().NewNode.InnerNode.NodeName);
            Assert.AreEqual("Arg", editDistance.Item2.Last().NewNode.InnerNode.NodeName);
        }

        [TestMethod]
        public void TestCompute8()
        {
            var py = Python.CreateEngine();
            var before = @"i * term(i)";
            var ast1 = NodeWrapper.Wrap(ParseContent(before, py));
            var after = @"term(i)";
            var ast2 = NodeWrapper.Wrap(ParseContent(after, py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(2, editDistance.Item1);
            Assert.IsTrue(editDistance.Item2.First() is Delete);
            Assert.IsTrue(editDistance.Item2.Last() is Delete);
            Assert.AreEqual("NameExpression", editDistance.Item2.First().NewNode.InnerNode.NodeName);
            Assert.AreEqual("BinaryExpression", editDistance.Item2.Last().NewNode.InnerNode.NodeName);
        }

        [TestMethod]
        public void TestCompute9()
        {
            var py = Python.CreateEngine();
            var before = @"total = i * term(i)";
            var ast1 = NodeWrapper.Wrap(ParseContent(before, py));
            var after = @"total = term(i)";
            var ast2 = NodeWrapper.Wrap(ParseContent(after, py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(2, editDistance.Item1);
            Assert.IsTrue(editDistance.Item2.First() is Delete);
            Assert.IsTrue(editDistance.Item2.Last() is Delete);
            Assert.AreEqual("NameExpression", editDistance.Item2.First().NewNode.InnerNode.NodeName);
            Assert.AreEqual("BinaryExpression", editDistance.Item2.Last().NewNode.InnerNode.NodeName);
        }

        [TestMethod]
        public void TestCompute10()
        {
            var py = Python.CreateEngine();
            var before = @"
def product(n, term):
    total = 0
    i = 1
    while i <= n:
        total *= i * term(i)
        i += 1
    return total";
            var ast1 = NodeWrapper.Wrap(ParseContent(before, py));
            var after = @"
def product(n, term):
    total = 1
    i = 1
    while i <= n:
        total *= term(i)
        i += 1
    return total";
            var ast2 = NodeWrapper.Wrap(ParseContent(after, py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(3, editDistance.Item1);
            
            Assert.IsTrue(editDistance.Item2.First() is Update);
            Assert.IsTrue(editDistance.Item2.ElementAt(1) is Delete);
            Assert.IsTrue(editDistance.Item2.Last() is Delete);
            Assert.AreEqual("literal", editDistance.Item2.First().NewNode.InnerNode.NodeName);
            Assert.AreEqual("NameExpression", editDistance.Item2.ElementAt(1).NewNode.InnerNode.NodeName);
            Assert.AreEqual("BinaryExpression", editDistance.Item2.Last().NewNode.InnerNode.NodeName);
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
