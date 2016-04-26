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
            Assert.AreEqual(1, editDistance.Distance);
        }

        [TestMethod]
        public void TestCompute2()
        {
            var py = Python.CreateEngine();
            var ast1 = NodeWrapper.Wrap(ParseContent("total, k = 0, 1", py));
            var ast2 = NodeWrapper.Wrap(ParseContent("total, k = 1, 1", py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(1, editDistance.Distance);
        }

        [TestMethod]
        public void TestCompute3()
        {
            var py = Python.CreateEngine();
            var ast1 = NodeWrapper.Wrap(ParseContent("x * a", py));
            var ast2 = NodeWrapper.Wrap(ParseContent("term(x) * a", py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(3, editDistance.Distance);
        }

       

        [TestMethod]
        public void TestCompute4()
        {
            var py = Python.CreateEngine();
            var ast1 = NodeWrapper.Wrap(ParseFile(Environment.CurrentDirectory + "../../../resources/before_1.py", py));
            var ast2 = NodeWrapper.Wrap(ParseFile(Environment.CurrentDirectory + "../../../resources/after_1.py", py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(2, editDistance.Distance);

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
            Assert.AreEqual(2, editDistance.Distance);
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
            Assert.AreEqual(1, editDistance.Distance);
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
            Assert.AreEqual(2, editDistance.Distance);
            Assert.AreEqual("NameExpression", editDistance.Edits.First().NewNode.InnerNode.NodeName);
            Assert.AreEqual("Arg", editDistance.Edits.Last().NewNode.InnerNode.NodeName);
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
            Assert.AreEqual(2, editDistance.Distance);
            Assert.IsTrue(editDistance.Edits.First() is Delete);
            Assert.IsTrue(editDistance.Edits.Last() is Delete);
            Assert.AreEqual("NameExpression", editDistance.Edits.First().NewNode.InnerNode.NodeName);
            Assert.AreEqual("BinaryExpression", editDistance.Edits.Last().NewNode.InnerNode.NodeName);
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
            Assert.AreEqual(2, editDistance.Distance);
            Assert.IsTrue(editDistance.Edits.First() is Delete);
            Assert.IsTrue(editDistance.Edits.Last() is Delete);
            Assert.AreEqual("NameExpression", editDistance.Edits.First().NewNode.InnerNode.NodeName);
            Assert.AreEqual("BinaryExpression", editDistance.Edits.Last().NewNode.InnerNode.NodeName);
            Assert.AreEqual("BinaryExpression", editDistance.Edits.First().Target.InnerNode.NodeName);
            Assert.AreEqual("AssignmentStatement", editDistance.Edits.Last().Target.InnerNode.NodeName);
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
            Assert.AreEqual(3, editDistance.Distance);
            
            Assert.IsTrue(editDistance.Edits.First() is Update);
            Assert.IsTrue(editDistance.Edits.ElementAt(1) is Delete);
            Assert.IsTrue(editDistance.Edits.Last() is Delete);
            Assert.AreEqual("literal", editDistance.Edits.First().NewNode.InnerNode.NodeName);
            Assert.AreEqual("NameExpression", editDistance.Edits.ElementAt(1).NewNode.InnerNode.NodeName);
            Assert.AreEqual("BinaryExpression", editDistance.Edits.Last().NewNode.InnerNode.NodeName);
        }


        [TestMethod]
        public void TestCompute11()
        {
            var py = Python.CreateEngine();
            var before = @"total = term(i)";
            var ast1 = NodeWrapper.Wrap(ParseContent(before, py));
            var after = @"total = i * term(i)";
            var ast2 = NodeWrapper.Wrap(ParseContent(after, py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(2, editDistance.Distance);
            Assert.IsTrue(editDistance.Edits.First() is Insert);
            Assert.IsTrue(editDistance.Edits.Last() is Insert);
            Assert.AreEqual("NameExpression", editDistance.Edits.First().NewNode.InnerNode.NodeName);
            Assert.AreEqual("BinaryExpression", editDistance.Edits.Last().NewNode.InnerNode.NodeName);
            Assert.AreEqual("BinaryExpression", editDistance.Edits.First().Target.InnerNode.NodeName);
            Assert.AreEqual("AssignmentStatement", editDistance.Edits.Last().Target.InnerNode.NodeName);
        }

        [TestMethod]
        public void TestCompute12()
        {
            var py = Python.CreateEngine();
            var before = @"i";
            var ast1 = NodeWrapper.Wrap(ParseContent(before, py));
            var after = @"term(i) * x";
            var ast2 = NodeWrapper.Wrap(ParseContent(after, py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(5, editDistance.Distance);
        }

        [TestMethod]
        public void TestCompute13()
        {
            var py = Python.CreateEngine();
            var before = @"i";
            var ast1 = NodeWrapper.Wrap(ParseContent(before, py));
            var after = @"term(x)";
            var ast2 = NodeWrapper.Wrap(ParseContent(after, py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(4, editDistance.Distance);

        }

        [TestMethod]
        public void TestCompute14()
        {
            var py = Python.CreateEngine();
            var before = @"
def product(n, term):
    item, Total = 0, 1
    while item<=n:
        item, Total = item + 1, Total * term(item)
        return Total";
            var ast1 = NodeWrapper.Wrap(ParseContent(before, py));
            var after = @"
def product(n, term):
    item, Total = 0, 1
    while item<=n:
        item, Total = item + 1, Total * term(item)
    return Total";
            var ast2 = NodeWrapper.Wrap(ParseContent(after, py));
            var zss = new PythonZss(ast1, ast2);
            var editDistance = zss.Compute();
            Assert.AreEqual(3, editDistance.Distance);

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
