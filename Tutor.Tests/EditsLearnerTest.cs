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
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Specifications;

namespace Tutor.Tests
{
    [TestClass]
    public class EditsLearnerTest
    {
        [TestMethod]
        public void TestLearn1()
        {
            var grammar = DSLCompiler.LoadGrammarFromFile(@"C:\Users\Gustavo\git\Tutor\Tutor\Transformation.grammar");

            var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent("x = 0"));

            var input = State.Create(grammar.Value.InputSymbol, astBefore);
            var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent("x = 1"));

            var examples = new Dictionary<State, object> { { input, astAfter } };
            var spec = new ExampleSpec(examples);

            var prose = new SynthesisEngine(grammar.Value);
            var learned = prose.LearnGrammar(spec);
            var first = learned.RealizedPrograms.First();
            var output = first.Invoke(input) as IEnumerable<PythonAst>;
            var fixedProgram = output.First();
            var unparser = new Unparser();
            var newCode = unparser.Unparse(fixedProgram);
            Assert.AreEqual("\r\nx = 1", newCode);

            var secondOutput = first.Invoke(State.Create(grammar.Value.InputSymbol,
                NodeWrapper.Wrap(ASTHelper.ParseContent("1 == 0")))) as IEnumerable<PythonAst>;
            Assert.IsTrue(secondOutput == null);
        }

        [TestMethod]
        public void TestLearn2()
        {
            var grammar = DSLCompiler.LoadGrammarFromFile(@"C:\Users\Gustavo\git\Tutor\Tutor\Transformation.grammar");

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

            var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(before));

            var input = State.Create(grammar.Value.InputSymbol, astBefore);
            var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(after));

            var examples = new Dictionary<State, object> { { input, astAfter } };
            var spec = new ExampleSpec(examples);

            var prose = new SynthesisEngine(grammar.Value);
            var learned = prose.LearnGrammar(spec);
            var first = learned.RealizedPrograms.First();
            var output = first.Invoke(input) as IEnumerable<PythonAst>;
            var fixedProgram = output.First();
            var unparser = new Unparser();
            var newCode = unparser.Unparse(fixedProgram);
            Assert.AreEqual(after, newCode);
        }

        [TestMethod]
        public void TestLearn3()
        {
            var grammar = DSLCompiler.LoadGrammarFromFile(@"C:\Users\Gustavo\git\Tutor\Tutor\Transformation.grammar");

            var before = @"
def product(n, term):
    def counter(i, total = 0):
        return total * term(i)";

            var after = @"
def product(n, term):
    def counter(i, total = 1):
        return total*term(i)";

            var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(before));

            var input = State.Create(grammar.Value.InputSymbol, astBefore);
            var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(after));

            var examples = new Dictionary<State, object> { { input, astAfter } };
            var spec = new ExampleSpec(examples);

            var prose = new SynthesisEngine(grammar.Value);
            var learned = prose.LearnGrammar(spec);
            var first = learned.RealizedPrograms.First();
            var output = first.Invoke(input) as IEnumerable<PythonAst>;
            var fixedProgram = output.First();
            var unparser = new Unparser();
            var newCode = unparser.Unparse(fixedProgram);
            Assert.AreEqual(after, newCode);
        }

        [TestMethod]
        public void TestLearn4()
        {
            var grammar = DSLCompiler.LoadGrammarFromFile(@"C:\Users\Gustavo\git\Tutor\Tutor\Transformation.grammar");

            var before = @"total = total * term(i)";

            var after = @"
total = term(i)*total";

            var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(before));

            var input = State.Create(grammar.Value.InputSymbol, astBefore);
            var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(after));

            var examples = new Dictionary<State, object> { { input, astAfter } };
            var spec = new ExampleSpec(examples);

            var prose = new SynthesisEngine(grammar.Value);
            var learned = prose.LearnGrammar(spec);
            var first = learned.RealizedPrograms.First();
            var output = first.Invoke(input) as IEnumerable<PythonAst>;
            var fixedProgram = output.First();
            var unparser = new Unparser();
            var newCode = unparser.Unparse(fixedProgram);
            Assert.AreEqual(after, newCode);
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

        private static void AssertCorrectTransformation(string before, string after)
        {
            var grammar = DSLCompiler.LoadGrammarFromFile(@"C:\Users\Gustavo\git\Tutor\Tutor\Transformation.grammar");

            var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(before));

            var input = State.Create(grammar.Value.InputSymbol, astBefore);
            var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(after));

            var examples = new Dictionary<State, object> {{input, astAfter}};
            var spec = new ExampleSpec(examples);

            var prose = new SynthesisEngine(grammar.Value);
            var learned = prose.LearnGrammarTopK(spec,"Score", k:1);

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


        //        [TestMethod]
        //        public void TestMatchNode()
        //        {
        //            var py = Python.CreateEngine();
        //            var code = ParseContent("x * a", py);
        //            var x = new NameExpression("x");
        //            var a = new NameExpression("a");
        //            var multiply = new IronPython.Compiler.Ast.BinaryExpression(PythonOperator.Multiply, x, a);
        //            var root = new PythonNode(multiply, true);
        //            root.AddChild(new PythonNode(x, true));
        //            root.AddChild(new PythonNode(a, true));
        //            var m = new Match(root);
        //            Assert.IsTrue(m.ExactMatch(code));
        //        }

        //        [TestMethod]
        //        public void TestMatchNodeFalse()
        //        {
        //            var py = Python.CreateEngine();
        //            var code = ParseContent("x * 0", py);
        //            var x = new NameExpression("x");
        //            var a = new NameExpression("a");
        //            var multiply = new IronPython.Compiler.Ast.BinaryExpression(PythonOperator.Multiply, x, a);
        //            var root = new PythonNode(multiply, true);
        //            root.AddChild(new PythonNode(x, true));
        //            root.AddChild(new PythonNode(a, true));
        //            var m = new Match(root);
        //            Assert.IsFalse(m.ExactMatch(code));
        //        }

        //        [TestMethod]
        //        public void TestMatchNode2()
        //        {
        //            var py = Python.CreateEngine();
        //            var code = ParseContent("n == 1", py);
        //            var n = new NameExpression("n");
        //            var literal = new IronPython.Compiler.Ast.ConstantExpression(1);
        //            var multiply = new IronPython.Compiler.Ast.BinaryExpression(PythonOperator.Equals, n, literal);
        //            var root = new PythonNode(multiply, true);
        //            root.AddChild(new PythonNode(n, true));
        //            root.AddChild(new PythonNode(literal, true, 1));
        //            var m = new Match(root);
        //            Assert.IsTrue(m.ExactMatch(code));
        //            Assert.AreEqual("literal", m.MatchResult.First()[1].NodeName);
        //        }

        //        [TestMethod]
        //        public void TestUpdateNode()
        //        {
        //            var py = Python.CreateEngine();
        //            var code = ParseContent("n == 1", py);
        //            code.Bind();
        //            var n = new NameExpression("n");
        //            var literal = new IronPython.Compiler.Ast.ConstantExpression(1);
        //            var multiply = new IronPython.Compiler.Ast.BinaryExpression(PythonOperator.Equals, n, literal);
        //            var root = new PythonNode(multiply, true);
        //            root.AddChild(new PythonNode(n, true));
        //            root.AddChild(new PythonNode(literal, true, 1));
        //            var m = new Match(root);
        //            Assert.IsTrue(m.ExactMatch(code));

        //            var newNode = new IronPython.Compiler.Ast.ConstantExpression(0);
        //            var update = new Update(new PythonNode(newNode, false), null);
        //            var newAst = update.Run(code, m.MatchResult.First()[1]);
        //            var ast = newAst as PythonAst;
        //            var body = ast.Body as SuiteStatement;
        //            var stmt = body.Statements.First() as ExpressionStatement;
        //            var binaryExp = stmt.Expression as IronPython.Compiler.Ast.BinaryExpression;
        //            var constant = binaryExp.Right as IronPython.Compiler.Ast.ConstantExpression;
        //            Assert.AreEqual(0, constant.Value);
        //        }

        //        [TestMethod]
        //        public void TestUpdateNode2()
        //        {
        //            var py = Python.CreateEngine();
        //            var code = @"
        //def accumulate(combiner, base, n, term):
        //    if n == 1:
        //        return base
        //    else:
        //        return combiner(term(n), accumulate(combiner, base, n - 1, term))";
        //            var ast = ParseContent(code, py);
        //            ast.Bind();


        //            var n = new NameExpression("n");
        //            var literal = new IronPython.Compiler.Ast.ConstantExpression(1);
        //            var binaryExpression = new IronPython.Compiler.Ast.BinaryExpression(PythonOperator.Equals, n, literal);
        //            var root = new PythonNode(binaryExpression, true);
        //            root.AddChild(new PythonNode(n, true));
        //            root.AddChild(new PythonNode(literal, true, 1));
        //            var m = new Match(root);

        //            Assert.IsTrue(m.ExactMatch(ast));
        //            Assert.AreEqual("literal", m.MatchResult.First()[1].NodeName);

        //            var newNode = new IronPython.Compiler.Ast.ConstantExpression(0);
        //            var update = new Update(new PythonNode(newNode, false), null);
        //            var newAst = update.Run(ast, m.MatchResult.First()[1]);

        //            var expected = @"
        //def accumulate(combiner, base, n, term):
        //    if n==0:
        //        return base
        //    else:
        //        return combiner(term(n), accumulate(combiner, base, n-1, term))";
        //            var actual = new Unparser().Unparse(newAst as PythonAst);

        //            Assert.AreEqual(expected, actual);
        //        }


        //        [TestMethod]
        //        public void TestInsertNode()
        //        {
        //            var py = Python.CreateEngine();
        //            var code = ParseContent("n = t", py);
        //            code.Bind();

        //            var assignStatement = ((SuiteStatement)code.Body).Statements.First() as AssignmentStatement;
        //            var expression = assignStatement.Right;

        //            var context = new Dictionary<int, Node>();
        //            var i = new NameExpression("i");
        //            context.Add(1, expression);
        //            context.Add(2, expression);
        //            var generateT = new BindingNodeSynthesizer(2) { Bindings = context };
        //            var generateI = new InsertNodeSynthesizer("NameExpression", "i");
        //            var generateBinary = new InsertNodeSynthesizer("BinaryExpression", PythonOperator.Multiply,
        //                new List<INodeSynthesizer>() { generateT, generateI });


        //            var insert = new Insert(generateBinary, context);


        //            var newAst = insert.Run(code, expression);

        //            var ast = newAst as PythonAst;
        //            var body = ast.Body as SuiteStatement;
        //            var stmt = body.Statements.First() as AssignmentStatement;
        //            Assert.IsTrue(stmt.Right is BinaryExpression);
        //            var binaryExp = (BinaryExpression)stmt.Right;
        //            Assert.IsTrue(binaryExp.Operator == PythonOperator.Multiply);
        //            Assert.IsTrue(binaryExp.Left is NameExpression);
        //            Assert.IsTrue(binaryExp.Right is NameExpression);
        //            Assert.AreEqual("t", ((NameExpression)binaryExp.Left).Name);
        //            Assert.AreEqual("i", ((NameExpression)binaryExp.Right).Name);
        //        }

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

        //        [TestMethod]
        //        public void TestfixProgram()
        //        {
        //            var program = @"
        //def product(n, term):
        //    total, k = 0, 1
        //    while k <= n:
        //        total, k = total * term(k), k + 1
        //    return total";

        //            var py = Python.CreateEngine();

        //            var total = new NameExpression("total");
        //            var k = new NameExpression("k");
        //            var leftTuple = new TupleExpression(true, total, k);
        //            var literal1 = new IronPython.Compiler.Ast.ConstantExpression(0);
        //            var literal2 = new IronPython.Compiler.Ast.ConstantExpression(1);
        //            var rightTuple = new TupleExpression(true, literal1, literal2);
        //            var assign = new AssignmentStatement(new IronPython.Compiler.Ast.Expression[] { leftTuple },
        //                rightTuple);
        //            var root = new PythonNode(assign, true);
        //            var leftNode = new PythonNode(leftTuple, true);
        //            leftNode.AddChild(new PythonNode(total, true));
        //            leftNode.AddChild(new PythonNode(k, true));
        //            root.AddChild(leftNode);
        //            var rightNode = new PythonNode(rightTuple, true);
        //            rightNode.AddChild(new PythonNode(literal1, true, 1));
        //            rightNode.AddChild(new PythonNode(literal2, true));
        //            root.AddChild(rightNode);

        //            var m = new Match(root);
        //            var newNode = new IronPython.Compiler.Ast.ConstantExpression(1);
        //            var update = new Rewrite(new PythonNode(newNode, false), null);
        //            var fix = new Edit(m, update);

        //            var fixer = new SubmissionFixer();

        //            var testSetup = @"def square(x):
        //    return x * x

        //def identity(x):
        //    return x
        //";
        //            var tests = new Dictionary<String, int>
        //            {
        //                {testSetup + "product(3, identity)", 6},
        //                {testSetup + "product(5, identity)", 120},
        //                {testSetup + "product(3, square)", 36},
        //                {testSetup + "product(5, square)", 14400}
        //            };
        //            var isFixed = fixer.Fix(program, new List<Edit>() { fix }, tests);
        //            Assert.AreEqual(true, isFixed);

        //        }

        //        [TestMethod]
        //        public void TestfixProgram2()
        //        {
        //            var program = @"
        //def product(n, term):
        //    z, w = 1, 1
        //    total, k = 0, 1
        //    while k <= n:
        //        total, k = total * term(k), k + 1
        //    return total";

        //            var py = Python.CreateEngine();

        //            var total = new NameExpression("total");
        //            var k = new NameExpression("k");
        //            var leftTuple = new TupleExpression(true, total, k);
        //            var literal1 = new IronPython.Compiler.Ast.ConstantExpression(0);
        //            var literal2 = new IronPython.Compiler.Ast.ConstantExpression(1);
        //            var rightTuple = new TupleExpression(true, literal1, literal2);
        //            var assign = new AssignmentStatement(new IronPython.Compiler.Ast.Expression[] { leftTuple },
        //                rightTuple);
        //            var root = new PythonNode(assign, true);
        //            var leftNode = new PythonNode(leftTuple, true);
        //            leftNode.AddChild(new PythonNode(total, true));
        //            leftNode.AddChild(new PythonNode(k, true));
        //            root.AddChild(leftNode);
        //            var rightNode = new PythonNode(rightTuple, true);
        //            rightNode.AddChild(new PythonNode(literal1, false, 1));
        //            rightNode.AddChild(new PythonNode(literal2, true));
        //            root.AddChild(rightNode);

        //            var m = new Match(root);
        //            var newNode = new IronPython.Compiler.Ast.ConstantExpression(1);
        //            var update = new Rewrite(new PythonNode(newNode,false), null);
        //            var fix = new Edit(m, update);

        //            var fixer = new SubmissionFixer();

        //            var testSetup = @"def square(x):
        //    return x * x

        //def identity(x):
        //    return x
        //";
        //            var tests = new Dictionary<String, int>
        //            {
        //                {testSetup + "product(3, identity)", 6},
        //                {testSetup + "product(5, identity)", 120},
        //                {testSetup + "product(3, square)", 36},
        //                {testSetup + "product(5, square)", 14400}
        //            };
        //            var isFixed = fixer.Fix(program, new List<Edit>() { fix }, tests);
        //            Assert.AreEqual(true, isFixed);

        //        }

        //        [TestMethod]
        //        public void TestfixProgram3()
        //        {
        //            var program = @"
        //def product(n, term):
        //    z, w = 1, 1
        //    total, k = 0, 1
        //    while k <= n:
        //        total, k = total * term(k), k + 1
        //    return total";

        //            var py = Python.CreateEngine();

        //            var total = new NameExpression("total");
        //            var k = new NameExpression("k");
        //            var leftTuple = new TupleExpression(true, total, k);
        //            var literal1 = new IronPython.Compiler.Ast.ConstantExpression(0);
        //            var literal2 = new IronPython.Compiler.Ast.ConstantExpression(1);
        //            var rightTuple = new TupleExpression(true, literal1, literal2);
        //            var assign = new AssignmentStatement(new IronPython.Compiler.Ast.Expression[] { leftTuple },
        //                rightTuple);
        //            var root = new PythonNode(assign, true);
        //            var leftNode = new PythonNode(leftTuple, true);
        //            leftNode.AddChild(new PythonNode(total, true));
        //            leftNode.AddChild(new PythonNode(k, true));
        //            root.AddChild(leftNode);
        //            var rightNode = new PythonNode(rightTuple, true);
        //            rightNode.AddChild(new PythonNode(literal1, true, 1));
        //            rightNode.AddChild(new PythonNode(literal2, true));
        //            root.AddChild(rightNode);

        //            var m = new Match(root);
        //            var newNode = new IronPython.Compiler.Ast.ConstantExpression(1);
        //            var update = new Rewrite(new PythonNode(newNode, false), null);
        //            var fix = new Edit(m, update);

        //            var fixer = new SubmissionFixer();

        //            var testSetup = @"def square(x):
        //    return x * x

        //def identity(x):
        //    return x
        //";
        //            var tests = new Dictionary<String, int>
        //            {
        //                {testSetup + "product(3, identity)", 6},
        //                {testSetup + "product(5, identity)", 120},
        //                {testSetup + "product(3, square)", 36},
        //                {testSetup + "product(5, square)", 14400}
        //            };
        //            var isFixed = fixer.Fix(program, new List<Edit>() { fix }, tests);
        //            Assert.AreEqual(true, isFixed);

        //        }


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
