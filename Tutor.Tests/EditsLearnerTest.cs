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
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Logging;
using Microsoft.ProgramSynthesis.Specifications;

namespace Tutor.Tests
{
    [TestClass]
    public class EditsLearnerTest
    {
        [TestMethod]
        public void TestLearn1()
        {
            var before = "x = 0";
            var after = @"
x = 1";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn2()
        {
            var before = @"
def product(n, term):
    total, k = 0, 1
    while k <= n:
        total, k = total * term(k), k + 1
    return total";

            var after = @"
def product(n, term):
    total, k = 1, 1
    while k<=n:
        total, k = total*term(k), k+1
    return total";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn3()
        {
            var before = @"
def product(n, term):
    def counter(i, total = 0):
        return total * term(i)";

            var after = @"
def product(n, term):
    def counter(i, total = 1):
        return total*term(i)";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn4()
        {
            var before = @"total = total * term(i)";
            var after = @"
total = term(i)*total";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn5()
        {
            var before = @"
def product(n, term):
    item, Total = 0, 1
    while item<=n:
        item, Total = item + 1, Total * term(item)
        return Total";

            var after = @"
def product(n, term):
    i, Total = 0, 1
    while i<=n:
        i, Total = i+1, Total*term(i)
    return Total";

            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn6()
        {

            var before = @"product = term(n)";

            var after = @"
product = 1";

            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn7()
        {

            var before = @"total *= i * term(i)";

            var after = @"
total *= term(i)";

            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn8()
        {
            var before = @"n >= 1";
            var after = @"
n>1";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn9()
        {
            var before = @"
def product(n, term):
    k, product = 1, 0
    while k <= n:
        product, k = (product * term(k)), k + 1
    return product";

            var after = @"
def product(n, term):
    k, product = 1, 1
    while k<=n:
        product, k = product*term(k), k+1
    return product";

            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn10()
        {
            var before = @"
term(i)";
            var after = @"
term(i+1)";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn11()
        {
            var before = @"n, y = 0, 0";
            var after = @"
n, y = 1, 0";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn12()
        {
            var before = @"helper(0,n)";
            var after = @"
helper(1, n)";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn13()
        {
            var before = @"
def product(n, term):
    def helper(a,n):
        if n==0:
            return a
        else:
            a=a*term(n)
        return helper(a,n-1)
    return helper(0,n)";
            var after = @"
def product(n, term):
    def helper(a, n):
        if n==0:
            return a
        else:
            a = a*term(n)
        return helper(a, n-1)
    return helper(1, n)";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn14()
        {
            var before = @"
def product(n, term):
    x = n
    y = 1
    while x>1: 
        x -= 1
        y = y*term(x)
    return y";
            var after = @"
def product(n, term):
    x, y = n, 1
    while x>=1:
        x, y = x-1, y*term(x)
    return y
    x = n
    y = 1
    while x>=1: 
        temp = x 
        x -= 1
        y = y*term(temp)
    return y";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearnMultipleExamples1()
        {
            var examples = new List<Tuple<string, string>>();
            var before = @"i = 0";
            var after = @"
i = 1";
            examples.Add(Tuple.Create(before, after));
            before = @"i, j = 0, 1";
            after = @"
i, j = 1, 1";
            examples.Add(Tuple.Create(before, after));
            AssertCorrectTransformation(examples);
        }

        private static void AssertCorrectTransformation(IEnumerable<Tuple<string,string>> mistakes) 
        {
            var grammar = DSLCompiler.LoadGrammarFromFile(@"C:\Users\Gustavo\git\Tutor\Tutor\synthesis\Transformation.grammar");

            var examples = new Dictionary<State, object>();
            foreach (var mistake in mistakes)
            {
                var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.Item1));

                var input = State.Create(grammar.Value.InputSymbol, astBefore);
                var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.Item2));
                examples.Add(input,astAfter);
            }
            var spec = new ExampleSpec(examples);
            var prose = new SynthesisEngine(grammar.Value);
            var learned = prose.LearnGrammarTopK(spec, "Score", k: 1);
            var first = learned.First();

            foreach (var mistake in mistakes)
            {
                var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.Item1));
                var input = State.Create(grammar.Value.InputSymbol, astBefore);
                var output = first.Invoke(input) as IEnumerable<PythonAst>;

                var isFixed = false;
                foreach (var fixedProgram in output)
                {
                    var unparser = new Unparser();
                    var newCode = unparser.Unparse(fixedProgram);
                    isFixed = mistake.Item2.Equals(newCode);
                    if (isFixed)
                        break;
                }
                Assert.IsTrue(isFixed);
            }
        }

        private static void AssertCorrectTransformation(string before, string after)
        {
            var grammar = DSLCompiler.LoadGrammarFromFile(@"C:\Users\Gustavo\git\Tutor\Tutor\synthesis\Transformation.grammar");

            var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(before));

            var input = State.Create(grammar.Value.InputSymbol, astBefore);
            var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(after));

            var examples = new Dictionary<State, object> {{input, astAfter}};
            var spec = new ExampleSpec(examples);

            
            var prose = new SynthesisEngine(grammar.Value, new SynthesisEngine.Config { LogListener = new LogListener() });
            var learned = prose.LearnGrammarTopK(spec,"Score", k:1);
            prose.Configuration.LogListener.SaveLogToXML("log.xml");
            var first = learned.First();
            var output = first.Invoke(input) as IEnumerable<PythonAst>;

            var isFixed = false;
            foreach (var fixedProgram in output)
            {
                var unparser = new Unparser();
                var newCode = unparser.Unparse(fixedProgram);
                 isFixed = after.Equals(newCode);
                if (isFixed)
                    break; 
            }
            Assert.IsTrue(isFixed);
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
            Assert.AreEqual("\r\ndef identity(n):\r\n    return n==0", new Unparser().Unparse(code));
        }

        [TestMethod]
        public void TestUnparser3()
        {
            var py = Python.CreateEngine();
            var code = @"
def accumulate(combiner, base, n, term):
    if n==1:
        return base
    else:
        return combiner(term(n), accumulate(combiner, base, n-1, term))";
            var ast = ParseContent(code, py);
            ast.Bind();
            var actual = new Unparser().Unparse(ast);
            Assert.AreEqual(code, actual);
        }

        [TestMethod]
        public void TestUnparser4()
        {
            var py = Python.CreateEngine();
            var code = @"
def product(n, term):
    if n==0:
        return term(0)
    else:
        return term(n)*product((n-1), term)";
            var ast = ParseContent(code, py);
            ast.Bind();
            var actual = new Unparser().Unparse(ast);
            Assert.AreEqual(code, actual);
        }

        [TestMethod]
        public void TestUnparser5()
        {
            var py = Python.CreateEngine();
            var code = @"
functools.reduce(lambda x, y: x*y, apply(term), a[1])";
            var ast = ParseContent(code, py);
            ast.Bind();
            var actual = new Unparser().Unparse(ast);
            Assert.AreEqual(code, actual);
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
