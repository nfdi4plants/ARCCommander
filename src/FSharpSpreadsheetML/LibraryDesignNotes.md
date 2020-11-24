# FSharpSpreadsheetML library design notes

The `FSharpSpreadsheetML` library is in its core a functional wrapper for the [OpenXML SDK](https://github.com/OfficeDev/Open-XML-SDK). The needs for the API are however highly influenced by the needs of the ArcCommander project. 

Currently, the aim of this library is to focus on value based data retrieval, insertion, and manipulation. 
Due to the deeply nested structure of the OpenXML SpresdsheetML, this leads for the need of propagation of central functionality across hierarchical levels. 

Consider this example:

Given the following hierarchical module structure:

```
A
└───B
    └───C
```

functionallity such as `get`, `set`, or `map` must be hierarchically accessed as well, e.g.

```
A
└───getB
    └───getC
```

So to `get` `C` from the `A` level is a two step process of `getB` from A, and then `getC` from the resulting `B` of previous function.

```
A
|> getB
|> getC
```

One aim of this library is to make it convenient to work across these levels, e.g providing `getCbyB` in addition to `getB` and `getC` to skip the pipelining in above example. 
This might seem trivial in this exemplary 3 level hierarchy, but be assured that there are more levels in the actual XML structure, and **not all traversals make sense**.

For now, this will be implemented with a clear focus on functions needed in the scope of the ArcCommander. Achieving feature completeness is an aim for a future standalone release of this library.

## Module names and design

- Modules are named the same as the hierarchical types provided by the OpenXML SDK where possible and reasonable.
- Functions in a module named `A` should always have a last parameter of type `A`, 
    
    example: signature of a function `A.getCbyB`: (B -> **A** -> C)

    so you can always pipe a value of type `A` into functions contained in the module with the same name.

    example for `A.getCbyB`:

    ```
    A
    |> A.getCByB B
    ```

## Function names

Abiding the function naming scheme hinted above would get out of hand very fast, and yielding very long function names, e.g. `A.getEbyDandCandB`.

Therefore, it is necessary to drop some expressiveness in favor of conciseness. 

However, a function with the name `A.getE` with the signature B -> C -> D -> **A** -> E with adequate parameter names might seem logical for people with insights about the underlying XML structure, but may hide too much complexity to be accessible for others. 
This is  the favoured approach for now, as the most 'user facing' API is the `SpreadsheetDocument` API, with little need to dig deeper when correctly implemented.

There is no clear answer here, and input is encouraged on that front.