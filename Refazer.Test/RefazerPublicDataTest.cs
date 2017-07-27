using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Refazer.Test
{
    [TestClass]
    public class RefazerPublicDataTest
    {
        [TestMethod]
        public void TestLearn1_Extraction()
        {
            var before = "x = 0;";
            var after = 1;
            TestUtils.AssertCorrectExtraction(before, after);
        }

        [TestMethod]
        public void TestLearn2_Extraction()
        {
            var before = @"def product(n, term):
    def counter(i, total = 0):
        return total * term(i)";
            var after = 1;
            TestUtils.AssertCorrectExtraction(before, after);
        }

        [TestMethod]
        public void TestLearn3_Extraction()
        {
            var before = @"def product(n, term):
    def counter(i, total = 0):
        return total * term(i)";
            var after = 2;
            TestUtils.AssertCorrectExtraction(before, after);
        }

        [TestMethod]
        public void TestLearnMultipleExamples1_Extraction()
        {
            var examples = new List<Tuple<string, int>>();
            var before = @"i = 0";
            var after = 1;
            examples.Add(Tuple.Create(before, after));
            before = @"j = 1";
            after = 1;
            examples.Add(Tuple.Create(before, after));
            TestUtils.AssertCorrectExtraction(examples);
        }

        [TestMethod]
        public void TestLearnMultipleExamples2_Extraction()
        {
            var examples = new List<Tuple<string, int>>();
            var before = @"i = 0";
            var after = 2;
            examples.Add(Tuple.Create(before, after));
            before = @"j = 1";
            after = 2;
            examples.Add(Tuple.Create(before, after));
            TestUtils.AssertCorrectExtraction(examples);
        }

        [TestMethod]
        public void TestLearn1()
        {
            var before = "x = 0;";
            var after = @"x = 1;";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn2_JS()
        {
            var before = @"function product(n, term) {
    total = 0;
    k = 1;
    return total;
}";


            var after = @"function product(n, term) {
    total = 1;
    k = 1;
    return total;
}";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn3()
        {
            var before = @"def product(n, term):
    def counter(i, total = 0):
        return total * term(i)";

            var after = @"def product(n, term):
    def counter(i, total = 1):
        return total*term(i)";
            TestUtils.AssertCorrectTransformation(before, after);
        }


        [TestMethod]
        public void TestLearn4()
        {
            var before = @"total = total * term(i)";
            var after = @"total = term(i)*total";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn5()
        {
            var before = @"def product(n, term):
    item, Total = 0, 1
    while item<=n:
        item, Total = item + 1, Total * term(item)
        return Total";

            var after = @"def product(n, term):
    i, Total = 0, 1
    while i<=n:
        i, Total = i+1, Total*term(i)
    return Total";

            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn6()
        {

            var before = @"product = term(n)";
            var after = @"product = 1";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn7()
        {
            var before = @"total *= i * term(i)";
            var after = @"total *= term(i)";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn8()
        {
            var before = @"n >= 1";
            var after = @"n>1";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn9()
        {
            var before = @"def product(n, term):
    k, product = 1, 1
    while k <= n:
        product, k = (product * term(k)), k + 1
    return product";

            var after = @"def product(n, term):
    k, product = 1, 1
    while k<=n:
        product, k = product*term(k), k+1
    return product";

            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn10()
        {
            var before = @"term(i)";
            var after = @"term(i+1)";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn11()
        {
            var before = @"n, y = 0, 0";
            var after = @"n, y = 1, 0";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn12()
        {
            var before = @"helper(0,n)";
            var after = @"helper(1, n)";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn13()
        {
            var before = @"def product(n, term):
    def helper(a,n):
        if n==0:
            return a
        else:
            a=a*term(n)
        return helper(a,n-1)
    return helper(0,n)";
            var after = @"def product(n, term):
    def helper(a, n):
        if n==0:
            return a
        else:
            a = a*term(n)
        return helper(a, n-1)
    return helper(1, n)";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn14()
        {
            var before = @"def product(n, term):
    x = y
    y = 1
    while x>1:
        x -= 1
        y = y*term(x)
    return y";
            var after = @"def product(n, term):
    x, y = n, 1
    while x>=1:
        x, y = x-1, y*term(x)
    return y";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn15()
        {
            var before = @"def product(n, term):
    if n == 0:
        return 1
    else:
        return n * product(n-1, term)";
            var after = @"def product(n, term):
    if n==0:
        return 1
    else:
        return term(n)*product(n-1, term)";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn16()
        {
            var before = @"def product(n, term):
    if n == 0:
        return 1
    else:
        return mul(n, product(n - 1, term))
";
            var after = @"def product(n, term):
    if n==0:
        return 1
    else:
        return mul(term(n), product(n-1, term))";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn17()
        {
            var before = @"def product(n, term):
    x = lambda term: term
    total = 1
    for i in range (1, n + 1):
        total = total * x(i)
    return total
";
            var after = @"def product(n, term):
    total = 1
    for i in range(1, n+1):
        total = total*term(i)
    return total";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn18()
        {
            var before = @"def product(n, term):
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
            var after = @"def product(n, term):
    if (n==1):
        return term(n)
    else:
        return term(n)*product(n-1, term)";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn20()
        {
            var before = @"def product(n, term):
    k = 1
    total = 1
    while k<n+1:
        total = total*term(k)
        k+1
    return total
";
            var after = @"def product(n, term):
    k = 1
    total = 1
    while k<n+1:
        total = total*term(k)
        k += 1
    return total";
            TestUtils.AssertCorrectTransformation(before, after);
        }


        [TestMethod]
        public void TestLearn19()
        {
            var before = @"def product(n, term):
    def total_prod(x, total):
        if x==n:
            return total*term(x)
    return total_prod(1, 1)
";
            var after = @"def product(n, term):
    def total_prod(x, total):
        if x==n:
            return total
        else:
            return total_prod(x+1, total*term(x+1))
    return total_prod(1, 1)";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn21()
        {
            var before = @"def product(n, term):
    summed = 1
    k = 1
    while k<=n:
        summed *= term(k)
        increment(k)
    return summed
";
            var after = @"def product(n, term):
    summed = 1
    k = 1
    while k<=n:
        summed *= term(k)
        k += 1
    return summed";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn22()
        {
            var before = @"def product(n, term):
    def multi(x):
        if x==n:
            return term(n)
        else:
            return term(x+1)*x
    return multi(1)
";
            var after = @"def product(n, term):
    def multi(x, func):
        if x==n:
            return func(n)
        else:
            return multi(x+1, func)*func(x)
    return multi(1, term)";
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn23()
        {
            var before = @"def product(n, term):
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
            var after = @"def product(n, term):
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
            TestUtils.AssertCorrectTransformation(before, after);
        }

        [TestMethod]
        public void TestLearn24()
        {
            var before = @"def product(n, term):
    i, Total = 0, 1
    while item<=n:
        i, Total = i + 1, Total * term(i)
        return Total";

            var after = @"def product(n, term):
    i, Total = 0, 1
    while i<=n:
        i, Total = i+1, Total*term(i)
    return Total";

            TestUtils.AssertCorrectTransformation(before, after);
        }





        [TestMethod]
        public void TestLearnMultipleExamples1()
        {
            var examples = new List<Tuple<string, string>>();
            var before = @"i = 0";
            var after = @"i = 1";
            examples.Add(Tuple.Create(before, after));
            before = @"i, j = 0, 1";
            after = @"i, j = 1, 1";
            examples.Add(Tuple.Create(before, after));
            TestUtils.AssertCorrectTransformation(examples);
        }

        [TestMethod]
        public void TestLearnMultipleExamples2()
        {
            var examples = new List<Tuple<string, string>>();
            var before = @"def product(n, term):
    if n == 1:
        return term(1)
    else:
        return term(n) + product(n - 1, term)";
            var after = @"def product(n, term):
    if n==1:
        return term(1)
    else:
        return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));
            before = @"def product(n, term):
    if n == 1:
        return term(1)
    else:
        return term(n) + product(n - 1, term)
";
            after = @"def product(n, term):
    if n==1:
        return term(1)
    else:
        return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            before = @"def product(n, term):
    if n==0:
        return 0
    elif n==1:
        return term(1)
    else:
        return term(n) + product(n-1, term)";
            after = @"def product(n, term):
    if n==0:
        return 0
    elif n==1:
        return term(1)
    else:
        return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            before = @"def product(n, term):
    if n == 1:
        return term(1)
    else:
        return term(n) + product(n-1, term)";
            after = @"def product(n, term):
    if n==1:
        return term(1)
    else:
        return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            before = @"def product(n, term):
    if n == 1:
        return term(1)
    else:
        return term(n) + product(n - 1, term)";
            after = @"def product(n, term):
    if n==1:
        return term(1)
    else:
        return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            before = @"def product(n, term):
    if n ==1:
        return n
    return term(n)+ product(n-1, term)";
            after = @"def product(n, term):
    if n==1:
        return n
    return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            before = @"def product(n, term):    
    if n == 1:
        return term(n)
    else:
        return term(n) + product(n-1, term)";
            after = @"def product(n, term):
    if n==1:
        return term(n)
    else:
        return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            before = @"def product(n, term):    
    if n == 1:
        return term(n)
    return term(n) + product(n-1,term)";
            after = @"def product(n, term):
    if n==1:
        return term(n)
    return term(n)*product(n-1, term)";
            examples.Add(Tuple.Create(before, after));

            TestUtils.AssertCorrectTransformation(examples);
        }


        [TestMethod]
        public void TestLearnMultipleExamples3()
        {
            var examples = new List<Tuple<string, string>>();
            var before = @"def product(n, term):
    i = 1
    total =1
    while i <= n:        
        total *=i
        i+=1
    return total";
            var after = @"def product(n, term):
    i = 1
    total = 1
    while i<=n:
        total *= term(i)
        i += 1
    return total";
            examples.Add(Tuple.Create(before, after));
            before = @"def product(n, term):
    counter, product = 1, 1
    while counter <= n:
        product *= counter
        counter += 1
    return product";
            after = @"def product(n, term):
    counter, product = 1, 1
    while counter<=n:
        product *= term(counter)
        counter += 1
    return product";
            examples.Add(Tuple.Create(before, after));
            TestUtils.AssertCorrectTransformation(examples);
        }


        [TestMethod]
        public void TestLearnMultipleExamples4()
        {
            var examples = new List<Tuple<string, string>>();
            var before = @"def product(n, term):
    total = 1
    k = 1
    if k <= n:
        total = total * term(k)
        k += 1";
            var after = @"def product(n, term):
    total = 1
    k = 1
    if k<=n:
        total = total*term(k)
        k += 1
    return total";
            examples.Add(Tuple.Create(before, after));
            before = @"def product(n, term):
    total = 1
    k = 1";
            after = @"def product(n, term):
    total = 1
    k = 1
    return total";
            examples.Add(Tuple.Create(before, after));
            TestUtils.AssertCorrectTransformation(examples);
        }


        [TestMethod]
        public void TestLearnMultipleExamples5()
        {
            var examples = new List<Tuple<string, string>>();
            var before = @"def product(n, term):
    trial, result = 1, 1
    while trial <= n:
        result = result * trial
        trial = trial + 1
    return result";
            var after = @"def product(n, term):
    trial, result = 1, 1
    while trial<=n:
        result = result*term(trial)
        trial = trial+1
    return result";
            examples.Add(Tuple.Create(before, after));
            before = @"def product(n, term):
    total = 1
    while n != 0:
        total = total*n
        n -= 1
    return total";
            after = @"def product(n, term):
    total = 1
    while n!=0:
        total = total*term(n)
        n -= 1
    return total";
            examples.Add(Tuple.Create(before, after));

            before = @"def product(n, term):
    x = 1
    total = 1
    while x <= n:
        total = total * x
        x += 1
    return total";
            after = @"def product(n, term):
    x = 1
    total = 1
    while x<=n:
        total = total*term(x)
        x += 1
    return total";
            examples.Add(Tuple.Create(before, after));

            TestUtils.AssertCorrectTransformation(examples);
        }


        [TestMethod]
        public void TestLearnMultipleExamples6()
        {
            var examples = new List<Tuple<string, string>>();
            var before = @"def product(n, term):
    total, identity = 1, 1
    while identity < n:
        total, identity = total * term(identity), identity + 1
    return product(total, identity)";
            var after = @"def product(n, term):
    total, identity = 1, 1
    while identity<=n:
        total, identity = total*term(identity), identity+1
    return total";
            examples.Add(Tuple.Create(before, after));



            TestUtils.AssertCorrectTransformation(examples);
        }

    }
}
