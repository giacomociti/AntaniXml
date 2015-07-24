(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
Introducing your project
========================

...

The core modules are not intended to be directly called by client code (in the future this may change
in order to provide some *hook* to tweak generators)
Anyway here's an overview for those interested in implementation details (contributions are welcome).
The library is written in F# and organized in modulues.
#### Xsd Domain
This module provides types to represent a schema.
F# data types like discriminated unions allow for very clear definitions.
The XML schema specification is rich and complex, so this module provides a simplified view.
Nevertheless, we cover a few specific (and sometimes tricky) concepts, like nillable values, fixed values,
union and lists for simple types, whitespace handling etc.

#### Xsd Factory
This module is in charge of parsing xsd files and creating models according to XsdDomain data types.
Parsing xsd is a complex task, and for this we rely on the .net BCL library (namespace Xmlchema..).
Converting the Schema Object Model (SOM) provided by the BCL library into our own simpler model is not strictly necessary.
But allows us to focus on..

#### Xml Generator


#### Constrained Generators




#### Known Limitations

[XML Schema](http://www.w3.org/XML/Schema) is rich and complex, it's inevitable to have some limitations.
A few ones are known and listed below. Some of them may hopefully be addressed in the future.
But likely there are many more unknown limitations. If you find one please raise an [issue](https://github.com/fsprojects/AntaniXml/issues).
Anyway don't be too scared of this disclaimer. I think AntaniXml can cope with many nuances and support many features 
(like regex thanks to [Fare](https://github.com/moodmosaic/Fare)).

* recursive schemas -
    at the moment, we simply throws an exception when an element definition refers to itself.

* identity and subset constraint -
    they goes against the 'generative' nature of schemas.



### Design notes

## Generators for simple datatypes
Generation of random values for simple datatypes is mainly based on FsCheck generators.
Each simple datatype is mapped to a suitable CLR type (e.g. `xs:int` to `System.Int32`, `xs:integer` to `System.Numerics.BigInteger`...) 
for which an [FsCheck](https://github.com/fscheck/FsCheck) generator already exists or can be easily built.
Anyway this is not enough. In case of constraining facets (like MaxLength) we have to narrow the set of values that can be generated.
An easy option is to drop generated values that violate a constraint. But we may en up discarding too many values.
Suppose we have the constraint of Length = 5. We should not generate strings of arbitrary length and then retain only those with length 5.
So we need more specific generators for each kind of constraint.


 

And for each simple datatype we need parse and format functions.
In W3C terms, parse is a map from the lexical space to the value space of the given datatype.
Usually we implement it relying on `System.XmlConvert`.
The format function instead maps values to lexical representations. 
It returns a string list because the same value may have multiple representations (e.g a plus sign may optionally prefix a number).



*)
