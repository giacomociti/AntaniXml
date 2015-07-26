(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/AntaniXml"
#r "FsCheck.dll"
#r "System.Xml.Linq"
#r "AntaniXml.dll"
open AntaniXml

(**
Tutorial
========================

The [public API](http://giacomociti.github.io/AntaniXml/reference/antanixml-xmlelementgenerator.html) 
comprises factory methods to create xml generators.
Usually schema definitions are given in xsd files so you need to specify their Uri.
Overloads accepting xsd as plain text are also provided: they're handy for experimenting with little xsd snippets.


*)

let xsdText = """
    <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" 
        elementFormDefault="qualified" attributeFormDefault="unqualified">
        <xs:element name="e1" type="xs:int" />
        <xs:element name="e2" type="xs:string" />
    </xs:schema>""" 

XmlElementGenerator
    .CreateFromSchemaText(xsdText, "e1", "")
    .GenerateInfinite()
    |> Seq.take 5
    |> Seq.iter (printfn "%A")

(**
the above code may for example produce:
```xml
    <e1>1</e1>
    <e1>1</e1>
    <e1>-4</e1>
    <e1>0</e1>
    <e1>2</e1>
``` 

### Property based testing

For property based testing there are factory methods providing instances of the
`Arbitrary` type [defined within FsCheck](https://fscheck.github.io/FsCheck/TestData.html):

*)

let arb = XmlElementGenerator.CreateArbFromSchemaUri("po.xsd", "purchaseOrder", "")

(**

Examples of how to use this with `FsCheck.NUnit` are in the [unit tests](https://github.com/giacomociti/AntaniXml/blob/master/tests/AntaniXml.Tests/XmlGeneratorTest.fs) 
(TBD: put here some examples).
Notice that at the moment we lack proper shrinking so counter-examples 
provided for failing tests may be bigger than necessary.

### Known Limitations

[XML Schema](http://www.w3.org/XML/Schema) is rich and complex, it's inevitable to have some limitations.
A few ones are known and listed below. Some of them may hopefully be addressed in the future.
But likely there are many more unknown limitations. If you find one please raise an [issue](https://github.com/fsprojects/AntaniXml/issues).
Anyway don't be too scared of this disclaimer. I think AntaniXml can cope with many nuances and support many features 
(like regex thanks to [Fare](https://github.com/moodmosaic/Fare)).
The main limitations currently known are:

#### recursive schemas

at the moment, we simply throw an exception when an element definition refers to itself.

#### identity and subset constraint

XML Schema provides rough equivalents of primary and foreign keys in databases.
Version 1.1 also introduced assertions to allow further constraints.
Schemas are primarily grammar based, so they are a good fit for random generators.
But these complementary features for specifying constraints are at odd with the *generative* approach.


### Core Modules
While the type `XmlElementGenerator` is an OO (C# friendly) entry point,
the rest of the library is organized in modules.
At the moment they are not designed to be directly used by client code (in the future this may change
in order to provide some *hook* to tweak generators).
Anyway here's an overview for those interested in implementation details (contributions are welcome).

#### Xsd Domain
This module provides types to represent a schema.
F# data types like discriminated unions allow for very clear definitions.
The XML schema specification is rich and complex, so this module provides a simplified view.
Nevertheless, we cover a few specific (and sometimes tricky) concepts, like nillable values, fixed values,
union and lists for simple types, whitespace handling etc.

#### Xsd Factory
This module is in charge of parsing xsd files and creating models according to XsdDomain data types.
Parsing xsd is a complex task, and for this we rely on the .NET BCL library (namespace `System.Xml.Schema`).
Converting the Schema Object Model (SOM) provided by the BCL library into our own 
simpler model allows decoupling of parsing xsd and building generators.

#### Lexical Mappings
This modules contains whitespace handling and also parse and format functions for simple datatypes.
In W3C terms, parse is a map from the lexical space to the value space of a given datatype.
Usually we implement it relying on `System.XmlConvert`.
The format function instead maps values to lexical representations. 
It returns a string list because the same value may have multiple representations (e.g. a plus sign may optionally prefix a number).

NOTE: features in this module may be implemented differently in future releases.

#### Constrained Generators
This module provides support for simple types featuring multiple facets.
For example when a simple type has both a Length facet and a pattern.

*TODO: Elaborate*

#### Facet based generators

This module provides generators supporting the facets defined in XML schema.

For example the `pattern` facet is supported using a [regex based generator](https://github.com/moodmosaic/Fare).
Much easier is the one for `enum` facets, yielding a generator for a fixed set of values.
For text based data types we have generators of bounded length values according 
to `Length`, `MinLength` and `MaxLength` facets.
And for data types that can be sorted there is a generator of values restricted to a
certain interval dictated by facets like `MinInclusive`, `MaxExclusive`...

#### Atomic generators
This module provides generators for simple (atomic) data types.
Generation of random values is mainly based on FsCheck generators.
Each simple datatype is mapped to a suitable CLR type (e.g. `xs:int` 
to `System.Int32`, `xs:integer` to `System.Numerics.BigInteger`...) 
for which an [FsCheck](https://github.com/fscheck/FsCheck) generator already exists 
or can be easily built.


#### Xml Generator

This is where elements and types (simple and complex) defined in a schema 
are mapped to random generators. Complex generators are composed using FsCheck combinators.

*)
