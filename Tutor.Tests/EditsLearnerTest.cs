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
using Microsoft.ProgramSynthesis.Diagnostics;
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
    k, product = 1, 1
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
    x = y
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
    return y";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn15()
        {
            var before = @"
def product(n, term):
    if n == 0:
        return 1
    else:
        return n * product(n-1, term)";
            var after = @"
def product(n, term):
    if n==0:
        return 1
    else:
        return term(n)*product(n-1, term)";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn16()
        {
            var before = @"
def product(n, term):
    if n == 0:
        return 1
    else:
        return mul(n, product(n - 1, term))
";
            var after = @"
def product(n, term):
    if n==0:
        return 1
    else:
        return mul(term(n), product(n-1, term))";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn17()
        {
            var before = @"
def product(n, term):
    x = lambda term: term
    total = 1
    for i in range (1, n + 1):
        total = total * x(i)
    return total
";
            var after = @"
def product(n, term):
    total = 1
    for i in range(1, n+1):
        total = total*term(i)
    return total";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn18()
        {
            var before = @"
def product(n, term):
    x = 1
    product = 1
    x = term(x)
    while n>0:
        product = product*x
        x += 1
        n -= 1
    if n==0:
        return product
";
            var after = @"
def product(n, term):
    if (n==1):
        return term(n)
    else:
        return term(n)*product(n-1, term)";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn20()
        {
            var before = @"
def product(n, term):
    k = 1
    total = 1
    while k<n+1:
        total = total*term(k)
        k+1
    return total
";
            var after = @"
def product(n, term):
    k = 1
    total = 1
    while k<n+1:
        total = total*term(k)
        k += 1
    return total";
            AssertCorrectTransformation(before, after);
        }


        [TestMethod]
        public void TestLearn19()
        {
            var before = @"
def product(n, term):
    def total_prod(x, total):
        if x==n:
            return total*term(x)
    return total_prod(1, 1)
";
            var after = @"
def product(n, term):
    def total_prod(x, total):
        if x==n:
            return total
        else:
            return total_prod(x+1, total*term(x+1))
    return total_prod(1, 1)";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn21()
        {
            var before = @"
def product(n, term):
    summed = 1
    k = 1
    while k<=n:
        summed *= term(k)
        increment(k)
    return summed
";
            var after = @"
def product(n, term):
    summed = 1
    k = 1
    while k<=n:
        summed *= term(k)
        k += 1
    return summed";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn22()
        {
            var before = @"
def product(n, term):
    def multi(x):
        if x==n:
            return term(n)
        else:
            return term(x+1)*x
    return multi(1)
";
            var after = @"
def product(n, term):
    def multi(x, func):
        if x==n:
            return func(n)
        else:
            return multi(x+1, func)*func(x)
    return multi(1, term)";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn23()
        {
            var before = @"
def product(n, term):
    k = 1
    sum1 = 1
    if term==identity:
        while k<=n:
            k += 1
        return sum1*term(k)
    if term==square:
        while k<=n:
            k += 1
        return sum1*term(k)
";
            var after = @"
def product(n, term):
    k = 1
    sum1 = 1
    if term==identity:
        while k<=n:
            sum1 = sum1*term(k)
            k += 1
        return sum1
    if term==square:
        while k<=n:
            sum1 = sum1*term(k)
            k += 1
        return sum1";
            AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn24()
        {
            var before = @"
def product(n, term):
    i, Total = 0, 1
    while item<=n:
        i, Total = i + 1, Total * term(i)
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
        public void TestLearnAccumulateExample()
        {
            var before = @"
def accumulate(combiner, base, n, term):
    if n == 0:
        return base
    elif n == 1:
        return term(n)
    return combiner(base, combiner(term(n), accumulate(combiner, lambda base: 1 if base == mul else 0, n - 1, term)))
from operator import add, mul";

            var after = @"
def accumulate(combiner, base, n, term):
    if n <= 0:
        return base
    elif n == 1:
        return term(n)
    return combiner(base, combiner(term(n), accumulate(combiner, lambda base: 1 if base == mul else 0, n - 1, term)))
from operator import add, mul";

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

        [TestMethod]
        public void TestLearnMultipleExamples2()
        {
            var examples = new List<Tuple<string, string>>();
            var before = @"
def product(n, term):
    if n == 1:
        return term(1)
    else:
        return term(n) + product(n - 1, term)";
            var after = @"
def product(n, term):
    if n==1:
        return term(1)
    else:
        return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));
            before = @"
def product(n, term):
    if n == 1:
        return term(1)
    else:
        return term(n) + product(n - 1, term)
";
            after = @"
def product(n, term):
    if n==1:
        return term(1)
    else:
        return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            before = @"
def product(n, term):
    if n==0:
        return 0
    elif n==1:
        return term(1)
    else:
        return term(n) + product(n-1, term)";
            after = @"
def product(n, term):
    if n==0:
        return 0
    elif n==1:
        return term(1)
    else:
        return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            before = @"
def product(n, term):
    if n == 1:
        return term(1)
    else:
        return term(n) + product(n-1, term)";
            after = @"
def product(n, term):
    if n==1:
        return term(1)
    else:
        return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            before = @"
def product(n, term):
    if n == 1:
        return term(1)
    else:
        return term(n) + product(n - 1, term)";
            after = @"
def product(n, term):
    if n==1:
        return term(1)
    else:
        return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            before = @"
def product(n, term):
    if n ==1:
        return n
    return term(n)+ product(n-1, term)";
            after = @"
def product(n, term):
    if n==1:
        return n
    return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            before = @"
def product(n, term):    
    if n == 1:
        return term(n)
    else:
        return term(n) + product(n-1, term)";
            after = @"
def product(n, term):
    if n==1:
        return term(n)
    else:
        return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            before = @"
def product(n, term):    
    if n == 1:
        return term(n)
    return term(n) + product(n-1,term)";
            after = @"
def product(n, term):
    if n==1:
        return term(n)
    return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            AssertCorrectTransformation(examples);
        }


        [TestMethod]
        public void TestLearnMultipleExamples3()
        {
            var examples = new List<Tuple<string, string>>();
            var before = @"
def product(n, term):
    i = 1
    total =1
    while i <= n:        
        total *=i
        i+=1
    return total";
            var after = @"
def product(n, term):
    i = 1
    total = 1
    while i<=n:
        total *= term(i)
        i += 1
    return total";
            examples.Add(Tuple.Create(before, after));
            before = @"
def product(n, term):
    counter, product = 1, 1
    while counter <= n:
        product *= counter
        counter += 1
    return product";
            after = @"
def product(n, term):
    counter, product = 1, 1
    while counter<=n:
        product *= term(counter)
        counter += 1
    return product";
            examples.Add(Tuple.Create(before, after));
            AssertCorrectTransformation(examples);
        }


        [TestMethod]
        public void TestLearnMultipleExamples4()
        {
            var examples = new List<Tuple<string, string>>();
            var before = @"
def product(n, term):
    total = 1
    k = 1
    if k <= n:
        total = total * term(k)
        k += 1";
            var after = @"
def product(n, term):
    total = 1
    k = 1
    if k<=n:
        total = total*term(k)
        k += 1
    return total";
            examples.Add(Tuple.Create(before, after));
            before = @"
def product(n, term):
    total = 1
    k = 1";
            after = @"
def product(n, term):
    total = 1
    k = 1
    return total";
            examples.Add(Tuple.Create(before, after));
            AssertCorrectTransformation(examples);
        }


        [TestMethod]
        public void TestLearnMultipleExamples5()
        {
            var examples = new List<Tuple<string, string>>();
            var before = @"
def product(n, term):
    trial, result = 1, 1
    while trial <= n:
        result = result * trial
        trial = trial + 1
    return result";
            var after = @"
def product(n, term):
    trial, result = 1, 1
    while trial<=n:
        result = result*term(trial)
        trial = trial+1
    return result";
            examples.Add(Tuple.Create(before, after));
            before = @"
def product(n, term):
    total = 1
    while n != 0:
        total = total*n
        n -= 1
    return total";
            after = @"
def product(n, term):
    total = 1
    while n!=0:
        total = total*term(n)
        n -= 1
    return total";
            examples.Add(Tuple.Create(before, after));

            before = @"
def product(n, term):
    x = 1
    total = 1
    while x <= n:
        total = total * x
        x += 1
    return total";
            after = @"
def product(n, term):
    x = 1
    total = 1
    while x<=n:
        total = total*term(x)
        x += 1
    return total";
            examples.Add(Tuple.Create(before, after));

            AssertCorrectTransformation(examples);
        }

        private static void AssertCorrectTransformation(IEnumerable<Tuple<string,string>> mistakes) 
        {
            var examples = new Dictionary<State, object>();
            foreach (var mistake in mistakes)
            {
                var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.Item1));

                var input = State.Create(grammar.Value.InputSymbol, astBefore);
                var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.Item2));
                examples.Add(input,astAfter);
            }
            var spec = new ExampleSpec(examples);
            var learned = prose.LearnGrammar(spec);
            var first = learned.RealizedPrograms.First();

            foreach (var mistake in mistakes)
            {
                var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.Item1));
                var input = State.Create(grammar.Value.InputSymbol, astBefore);
                var output = first.Invoke(input) as IEnumerable<PythonNode>;

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

        private static Result<Grammar> grammar = DSLCompiler.LoadGrammarFromFile(@"..\..\..\Tutor\synthesis\Transformation.grammar");
        private static SynthesisEngine prose = new SynthesisEngine(grammar.Value, new SynthesisEngine.Config { LogListener = new LogListener() });

        private static void AssertCorrectTransformation(string before, string after)
        {

            var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(before));

            var input = State.Create(grammar.Value.InputSymbol, astBefore);
            var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(after));

            var examples = new Dictionary<State, object> {{input, astAfter}};
            var spec = new ExampleSpec(examples);

            
            var learned = prose.LearnGrammarTopK(spec, "Score");
            prose.Configuration.LogListener.SaveLogToXML("log.xml");
            var first = learned.First();
            var output = first.Invoke(input) as IEnumerable<PythonNode>;

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

            var executed = py.Execute(new Unparser().Unparse(code) + "\nidentity(2)");
            Assert.AreEqual(2, executed);
        }

        [TestMethod]
        public void TestUnparser()
        {
            var py = Python.CreateEngine();
            var code = ParseContent("def identity(n) : \n    return n", py);
            Assert.AreEqual("\r\ndef identity(n):\r\n    return n", new Unparser().Unparse(code));

        }

        [TestMethod]
        public void TestUnparser2()
        {
            var py = Python.CreateEngine();
            var code = ParseContent("def identity(n) : \n    term(n) == \"teste\"", py);
            Assert.AreEqual("\r\ndef identity(n):\r\n    term(n)==\"teste\"", new Unparser().Unparse(code));
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
            var actual = new Unparser().Unparse(ast);
            Assert.AreEqual(code, actual);
        }

        [TestMethod]
        public void TestUnparser6()
        {
            var py = Python.CreateEngine();
            var code = @"
def product(n, term):
    def term_output(n):
        return term(n)
    if n>1:
        return 
    else:
        return product(n-1, term_output(n-1))*term_output(n)";
            var ast = ParseContent(code, py);
            var actual = new Unparser().Unparse(ast);
            Assert.AreEqual(code, actual);
        }

        [TestMethod]
        public void TestUnparser7()
        {
            var py = Python.CreateEngine();
            var code = @"
def product(n, term):
    def helper(n, acc):
        if n>0:
            return helper(n-1, acc)
        print(acc)
    return helper(n, 1)";
            var ast = ParseContent(code, py);
            var actual = new Unparser().Unparse(ast);
            Assert.AreEqual(code, actual);
        }

        [TestMethod]
        public void TestUnparser8()
        {
            var py = Python.CreateEngine();
            var code = @"
def product(n, term):
    counter = n
    listen = []
    while counter>0:
        counter -= 1
        x = term(n)
        listen.append(x)
    product = 1
    for x in listen:
        product *= x
    return product";
            var ast = ParseContent(code, py);
            var actual = new Unparser().Unparse(ast);
            Assert.AreEqual(code, actual);
        }

        [TestMethod]
        public void TestUnparser9()
        {
            var py = Python.CreateEngine();
            var code = @"
def product(n, term):
    lst = []
    for i in range (1, n + 1):
        lst.append(i)
    return [term(x) for x in lst]";
            var ast = ParseContent(code, py);
            var actual = new Unparser().Unparse(ast);
            Assert.AreEqual(code, actual);
        }

        private PythonNode ParseContent(string content, ScriptEngine py)
        {
            var src = HostingHelpers.GetSourceUnit(py.CreateScriptSourceFromString(content));
            return NodeWrapper.Wrap(Parse(py, src));
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
