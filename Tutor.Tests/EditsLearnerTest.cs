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
using Community.CsharpSqlite;
using Expression = System.Linq.Expressions.Expression;

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
            var m = new Match("BinaryExpression", children, new BindingInfo() {BindingIndex = 1 });
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
            var m = new Match("BinaryExpression", children, new BindingInfo() { BindingIndex = 0 }); ;
            Assert.IsFalse(m.HasMatch(code));
        }

        [TestMethod]
        public void TestMatchNode2()
        {
            var py = Python.CreateEngine();
            var code = ParseContent("n == 1", py);
            var children = new List<string>() { "NameExpression", "literal" };
            var m = new Match("BinaryExpression", children, new BindingInfo() { BindingIndex = 2});
            Assert.IsTrue(m.HasMatch(code));
            Assert.AreEqual("BinaryExpression", m.MatchResult.Anchor.NodeName);
            Assert.AreEqual("literal", m.MatchResult.Bindings.First().NodeName);
        }

        [TestMethod]
        public void TestUpdateNode()
        {
            var py = Python.CreateEngine();
            var code = ParseContent("n == 1", py);
            code.Bind();
            var children = new List<string>() { "NameExpression", "literal" };
            var m = new Match("BinaryExpression", children, new BindingInfo() { BindingIndex = 2 });
            Assert.IsTrue(m.HasMatch(code));
            Assert.AreEqual("BinaryExpression", m.MatchResult.Anchor.NodeName);

            var newNode = new IronPython.Compiler.Ast.ConstantExpression(0);
            var update = new Update() { NewNode = newNode };
            var newAst = update.Run(code, m.MatchResult);
            var ast = newAst as PythonAst;
            var body = ast.Body as SuiteStatement;
            var stmt = body.Statements.First() as ExpressionStatement;
            var binaryExp = stmt.Expression as IronPython.Compiler.Ast.BinaryExpression;
            var constant = binaryExp.Right as IronPython.Compiler.Ast.ConstantExpression;
            Assert.AreEqual(0,constant.Value);
        }

        [TestMethod]
        public void TestUpdateNode2()
        {
            var py = Python.CreateEngine();
            var code = @"
def accumulate(combiner, base, n, term):
    if n == 1:
        return base
    else:
        return combiner(term(n), accumulate(combiner, base, n - 1, term))";
            var ast = ParseContent(code, py);
            ast.Bind();
            var children = new List<string>() { "NameExpression", "literal" };
            var m = new Match("BinaryExpression", children, new BindingInfo() { BindingIndex = 2 });
            Assert.IsTrue(m.HasMatch(ast));
            Assert.AreEqual("BinaryExpression", m.MatchResult.Anchor.NodeName);

            var newNode = new IronPython.Compiler.Ast.ConstantExpression(0);
            var update = new Update() {NewNode = newNode};
            var newAst = update.Run(ast, m.MatchResult);
            var expected = @"
def accumulate(combiner, base, n, term):
    if n == 0:
        return base
    else:
        return combiner(term(n), accumulate(combiner, base, n - 1, term))";
            var actual = new Unparser().Unparse(newAst as PythonAst);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestRunPythonMethod()
        {
            var py = Python.CreateEngine();
            var code = ParseContent("def identity(n) : \n    return n", py);
            code.Bind();
            
            var executed = py.Execute(new Unparser().Unparse(code) + "\nidentity(2)");
            Assert.AreEqual(2, executed);
        }

        [TestMethod]
        public void TestUnparser()
        {
            var py = Python.CreateEngine();
            var code = ParseContent("def identity(n) : \n    return n", py);
            code.Bind();
            Assert.AreEqual("\r\ndef identity(n):\r\n    return n", new Unparser().Unparse(code));

        }

        [TestMethod]
        public void TestUnparser2()
        {
            var py = Python.CreateEngine();
            var code = ParseContent("def identity(n) : \n    return n == 0", py);
            code.Bind();
            Assert.AreEqual("\r\ndef identity(n):\r\n    return n == 0", new Unparser().Unparse(code));
        }

        [TestMethod]
        public void TestUnparser3()
        {
            var py = Python.CreateEngine();
            var code = @"
def accumulate(combiner, base, n, term):
    if n == 1:
        return base
    else:
        return combiner(term(n), accumulate(combiner, base, n - 1, term))";
            var ast = ParseContent(code,py);
            ast.Bind();
            var actual = new Unparser().Unparse(ast);
            Assert.AreEqual(code, actual);
        }

        [TestMethod]
        public void TestfixProgram()
        {
            var program = @"
def product(n, term):
    total, k = 0, 1
    while k <= n:
        total, k = total * term(k), k + 1
    return total";

            var py = Python.CreateEngine();

            var children = new List<string>() { "TupleExpression", "NameExpression", "NameExpression", "TupleExpression", "literal", "literal" };
            var match = new Match("AssignmentStatement", children, new BindingInfo() { BindingIndex = 5 });

            var newNode = new IronPython.Compiler.Ast.ConstantExpression(1);
            var update = new Update() { NewNode = newNode };
            var fix = new EditsProgram(match, update);

            var fixer = new SubmissionFixer();

            var testSetup = @"def square(x):
    return x * x

def identity(x):
    return x
";
            var tests = new Dictionary<String, int>
            {
                {testSetup + "product(3, identity)", 6},
                {testSetup + "product(5, identity)", 120},
                {testSetup + "product(3, square)", 36},
                {testSetup + "product(5, square)", 14400}
            };
            var isFixed = fixer.Fix(program, new List<EditsProgram>() { fix }, tests);
            Assert.AreEqual(true, isFixed);

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
