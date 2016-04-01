using System;
using System.Linq.Expressions;
using IronPython;
using IronPython.Compiler;
using IronPython.Compiler.Ast;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.Scripting.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Tutor.Tests
{
    [TestClass]
    public class EditsLearnerTest
    {
        [TestMethod]
        public void TestLearn()
        {
            EditsLearner learner = new EditsLearner();
            var py = Python.CreateEngine();
            var before = ParseContent("x * a", py);
            var after = ParseContent("term(x) * a", py);
            var editsProgram = learner.Learn(before, after);
            Assert.IsTrue(editsProgram != null);
        }

        [TestMethod]
        public void TestMatchNode()
        {
            var py = Python.CreateEngine();
            var code = ParseContent("x * a", py);
            var children = new List<string>() { "NameExpression", "NameExpression" };
            var m = new Match(code, "BinaryExpression", children, 1);
            Assert.IsTrue(m.HasMatch(code));
            Assert.AreEqual("BinaryExpression", m.MatchResult.Anchor.NodeName);
            Assert.AreEqual("NameExpression", m.MatchResult.Bindings.First().NodeName);
        }

        [TestMethod]
        public void TestMatchNodeFalse()
        {
            var py = Python.CreateEngine();
            var code = ParseContent("x * 0", py);
            var children = new List<string>() { "NameExpression", "NameExpression" };
            var m = new Match(code, "BinaryExpression", children, 0); ;
            Assert.IsFalse(m.HasMatch(code));
        }

        [TestMethod]
        public void TestMatchNode2()
        {
            var py = Python.CreateEngine();
            var code = ParseContent("n == 1", py);
            var children = new List<string>() { "NameExpression", "literal" };
            var m = new Match(code, "BinaryExpression", children, 2);
            Assert.IsTrue(m.HasMatch(code));
            Assert.AreEqual("BinaryExpression", m.MatchResult.Anchor.NodeName);
            Assert.AreEqual("literal", m.MatchResult.Bindings.First().NodeName);
        }

        [TestMethod]
        public void TestUpdateNode()
        {
            var py = Python.CreateEngine();
            var code = ParseContent("1 == 1", py);
            code.Bind();
            var children = new List<string>() { "literal", "literal" };
            var m = new Match(code, "BinaryExpression", children, 2);
            Assert.IsTrue(m.HasMatch(code));
            Assert.AreEqual("BinaryExpression", m.MatchResult.Anchor.NodeName);

            var newNode = new IronPython.Compiler.Ast.ConstantExpression(0);
            var newAst = Edits.Update(code, m.MatchResult, newNode);
            var body = newAst as SuiteStatement;
            var stmt = body.Statements.First() as ExpressionStatement;
            var binaryExp = stmt.Expression as IronPython.Compiler.Ast.BinaryExpression;
            var constant = binaryExp.Right as IronPython.Compiler.Ast.ConstantExpression;
            Assert.AreEqual(0,constant.Value);
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
