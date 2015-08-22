(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I @"..\..\bin\AntaniXml"
#I @"..\..\..\FSharp.Data\bin" // need FSharp.Data
//#I @"..\..\..\AntaniXml\bin\AntaniXml"
#r "FsCheck.dll"
#r "System.Xml.Linq"
#r "AntaniXml.dll"
#r "FSharp.Data"


open AntaniXml


(**
Tutorial
========================

The [public API](http://giacomociti.github.io/AntaniXml/reference/antanixml-xmlelementgenerator.html) 
comprises factory methods to create xml generators.
Usually schema definitions are given as xsd files, so you need to specify their Uri.
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


### Property based testing

For property based testing there are factory methods providing instances of the
`Arbitrary` type [defined by FsCheck](https://fscheck.github.io/FsCheck/TestData.html):

*)

let arb = XmlElementGenerator.CreateArbFromSchemaUri("po.xsd", "purchaseOrder", "")

(**

Examples of how to use this with `FsCheck.NUnit` are in the [unit tests](https://github.com/giacomociti/AntaniXml/blob/master/tests/AntaniXml.Tests/XmlGeneratorTest.fs) 
(TBD: put here some examples).
Notice that at the moment we lack proper shrinking so counter-examples 
provided for failing tests may be bigger than necessary.

### [Creating samples for the XML type provider](#XMLTypeProvider)

Another possible usage scenario is creating samples for the XML type provider.

*)


(**
[FSharp.Data](http://fsharp.github.io/FSharp.Data) is a popular F# library featuring many type providers, including one for XML.
Strongly typed access to xml documents is achieved with inference on samples. AntaniXml may help to produce the needed samples:
*)


open AntaniXml
open System.IO
open FSharp.Data
open System.Xml.Linq


let samples = 
    XmlElementGenerator
        .CreateFromSchemaUri(@"C:\temp\po.xsd", "purchaseOrder", "")
        .Generate(5)
XElement(XName.Get("root"), samples).Save(@"C:\temp\samples.xml")

type po = XmlProvider< @"C:\temp\samples.xml", SampleIsList = true>


(**

Of course when a schema is available it would be a better option to infer types directly from it.
Future versions of FSharp.Data may support xsd.


### Known Limitations

[XML Schema](http://www.w3.org/XML/Schema) is rich and complex, it's inevitable to have some limitations.
A few ones are known and listed below. Some of them may hopefully be addressed in the future.
But likely there are many more unknown limitations. If you find one please raise an [issue](https://github.com/giacomociti/AntaniXml/issues).
Anyway don't be too scared of this disclaimer. I think AntaniXml can cope with many nuances and support many features 
(like regex patterns thanks to [Fare](https://github.com/moodmosaic/Fare)).
The main limitations currently known are:

#### built-in types
A few built-in types are not supported: Notation, NmTokens, Id, Idref, Idrefs, Entity and Entities.

#### abstract types and elements
See this [issue] (https://github.com/giacomociti/AntaniXml/issues/5).

#### identity and subset constraint

XML Schema provides rough equivalents of primary and foreign keys in databases.
Version 1.1 also introduced assertions to allow further constraints.
Schemas are primarily grammar based, so they are a good fit for random generators.
But these complementary features for specifying constraints are at odd with the generative approach.


### Public API
The type `XmlElementGenerator` is an OO (C# friendly) entry point,
the rest of the library is organized in modules.
At the moment they are not designed to be directly used by client code (in the future this may change
in order to provide some *hook* to tweak generators).
Also the design of the OO public API may change, 
see this [issue] (https://github.com/giacomociti/AntaniXml/issues/4).


*)
