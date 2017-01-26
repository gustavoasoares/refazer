# Refazer

Refazer (rebuild, in Portuguese) is technique for automatically learning program transformations. It builds on the observation that code edits performed by developers can be used as input-output examples for learning program transformations. Example edits may share the same structure but involve different variables and subexpressions, which must be generalized in a transformation at the right level of abstraction.

## Usage 

Let's say that we want to learn a transformation that adds a missing call to a function. The following code illustrates how we can invoke Refazer passing an input-output example and Refazer learns a list o transformations that satisfy the example. 

```c#
var input = 
@"def product(n, term):
  return term(n)*product(n-1)"
var output = 
@"def product(n, term):
  if n == 1: 
    return term(1)
  return term(n)*product(n-1)"
        
var examples = {Tuple.Create(input,output)};; sss
var refazer = new Refazer4Python();
var transformations = refazer.LearnTransformations(examples);
```

Next we apply the transformation to a different input program. 

```
var input = "" 
var output = refazer.Apply(transformation, input);
Console.Out.WriteLine("");
```

