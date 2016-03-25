(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
    #I @"..\..\bin\AntaniXml"
    #I @"..\..\..\FSharp.Data\bin" // need FSharp.Data
    #r "FsCheck.dll"
    #r "System.Xml.Linq"
    #r "AntaniXml.dll"
    #r "FSharp.Data"


    open AntaniXml
    open XsdFactory
    open System.Xml

(**
Tutorial
========================

The [public API](http://giacomociti.github.io/AntaniXml/reference/antanixml-schema.html) 
comprises factory methods to load a schema.
Usually schema definitions are given as xsd files, so you need to specify their Uri.
Overloads accepting xsd as plain text are also provided; they're handy for experimenting with 
little xsd snippets:

*)

    let xsdText = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" 
            elementFormDefault="qualified" attributeFormDefault="unqualified">
            <xs:element name="e1" type="xs:int" />
            <xs:element name="e2" type="xs:string" />
        </xs:schema>""" 

    Schema.CreateFromText(xsdText)
          .Generator(XmlQualifiedName "e1")
          .GenerateInfinite()
          |> Seq.take 5
          |> Seq.iter (printfn "%A")


(**

Choosing a global element we can get a generator for it.
The same example in C# is:

    var xsdText = @"
        <xs:schema xmlns:xs = 'http://www.w3.org/2001/XMLSchema'
            elementFormDefault = 'qualified' attributeFormDefault = 'unqualified' >
            <xs:element name = 'e1' type = 'xs:int' />
            <xs:element name = 'e2' type = 'xs:string' />
        </xs:schema > ";

    Schema.CreateFromText(xsdText)
        .Generator(new XmlQualifiedName("e1"))
        .GenerateInfinite()
        .Take(5)
        .ToList()
        .ForEach(Console.WriteLine);


and may generate something like this:

    [lang=xml]
    <e1>0</e1>
    <e1>	-3</e1>
    <e1>0</e1>
    <e1>4</e1>
    <e1>-1</e1>


### Property based testing

For property based testing we can get instances of the
`Arbitrary` type [defined by FsCheck](https://fscheck.github.io/FsCheck/TestData.html):

*)
    let arb = Schema.CreateFromUri("po.xsd")
                    .Arbitrary(XmlQualifiedName "purchaseOrder")
    

(**

Again, the C# version is almost the same:

    var arb = Schema.CreateFromUri("po.xsd")
        .Arbitrary(new XmlQualifiedName("purchaseOrder"));


The idea of property based testing is to express a specification with boolean functions (properties).
But instead of trying to *prove* that a property holds, we simply check that the function is true for a big number of randomly
generated input values.

In our context the first, obligatory example is validity.
This is of course a property we always expect to hold and hopefully AntaniXml produces valid elements, 
but it's worth checking it because for some schema it may not be the case,
and you may discover the need to customize generators in order to obtain valid elements.

The `Check.Quick` function generates a certain number of values using the given `Arbitrary` instance and,
for each one, checks if the property holds; in this case it checks if the generated element is valid:

*)
    open FsCheck
    
    let schema = Schema.CreateFromUri "foo.xsd"
    let arbFoo = schema.Arbitrary(XmlQualifiedName "foo")
    Prop.forAll arbFoo schema.IsValid
    |> Check.Quick 

(**


The same in C# is:

    var schema = Schema.CreateFromUri("foo.xsd");
    var arbFoo = schema.Arbitrary(new XmlQualifiedName("foo"));
    Prop.ForAll(arbFoo, x => schema.IsValid(x))
        .QuickCheck();


In the standard output a message like the following should be printed 
    
    Ok, passed 100 tests.

In case a counter-example is found, it is printed instead.
FsCheck has a concept of shrinking aimed at minimizing counter-examples.
At the moment AntaniXml lacks proper support for shrinking so counter-examples 
provided for failing tests may be bigger than necessary.

When checking properties in a unit test, the function `Check.QuickThrowOnFailure` may be used instead of `Check.Quick` 
so that a test failure is triggered when a property does not hold.
For popular unit testing frameworks like NUnit and XUnit there are also extensions enabling to 
express FsCheck properties more directly.

A more interesting example of property based testing is about XML data binding and serialization.
Suppose you have a class representing a global element in a schema.
This kind of data binding classes are often obtained with tools 
like [`xsd.exe`](https://msdn.microsoft.com/en-us/library/x6c1kb0s(v=vs.110).aspx).
It may be interesting to check that all valid elements can be properly deserialized into instances
of the corresponding class. And serializing such instances back to XML should result in equivalent elements.
Probably it's not required for the resulting elements to be identical to the original ones, especially when it
comes to formatting details; but at least we should expect no loss of contents.
You may be surprised to discover that for many schemas it is quite hard or impossible to get a suitable data binding class.
This is due to the [X/O impedance mismatch](http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.105.1550&rep=rep1&type=pdf).

One more use case is schema evolution. Evolving a schema ensuring backward compatibility means that all the
elements valid according to the old version of the schema should also be valid for the new version.
This can be checked with property based testing.

*)
    open FsCheck
    
    let oldSchema = Schema.CreateFromUri "old.xsd"
    let newSchema = Schema.CreateFromUri "new.xsd"
    let arbFooOld = oldSchema.Arbitrary(XmlQualifiedName "foo")

    let isStillValid elm = oldSchema.IsValid elm ==> newSchema.IsValid elm

    Prop.forAll arbFooOld isStillValid
    |> Check.Quick

(**

The same in C# is:

    var oldSchema = Schema.CreateFromUri("old.xsd");
    var newSchema = Schema.CreateFromUri("new.xsd");
    var arbFooOld = oldSchema.Arbitrary(new XmlQualifiedName("foo"));
    Prop.ForAll(arbFooOld, x => oldSchema.IsValid(x).When(newSchema.IsValid(x)))
        .QuickCheck();

In this example we also see in action a conditional property, 
expressed in F# with the `==>` operator and in C# with the fluent method `When`.
Again, the concept of conditional property is well explained 
in the [FsCheck documentation](https://fscheck.github.io/FsCheck/Properties.html).



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
        Schema.CreateFromUri(@"C:\temp\po.xsd")
              .Generator(new XmlQualifiedName "purchaseOrder")
              .Generate(5)
    XElement(XName.Get "root", samples).Save(@"C:\temp\samples.xml")

    type po = XmlProvider< @"C:\temp\samples.xml", SampleIsList = true>


(**

Of course when a schema is available it would be a better option to infer types directly from it.
Future versions of FSharp.Data may support xsd.


### Known Limitations

[XML Schema](http://www.w3.org/XML/Schema) is rich and complex, it's inevitable to have some limitations.
A few ones are known and listed below. Some of them may hopefully be addressed in the future.
But likely there are many more unknown limitations. If you find one please raise an [issue](https://github.com/giacomociti/AntaniXml/issues).
Anyway don't be too scared of this disclaimer. AntaniXml can cope with many nuances and support many features 
(like regex patterns thanks to [Fare](https://github.com/moodmosaic/Fare)).
The main limitations currently known are:

#### built-in types
A few built-in types are not supported: Notation, NmTokens, Id, Idref, Idrefs, Entity and Entities.

#### identity and subset constraint

XML Schema provides rough equivalents of primary and foreign keys in databases.
Version 1.1 also introduced assertions to allow further constraints.
Schemas are primarily grammar based, so they are a good fit for random generators.
But these complementary features for specifying constraints are at odd with the generative approach.

#### wildcards
With certain kinds of wildcards (e.g. `##other`) it may be impossible to generate valid contents.

#### regex patterns
Some regex patterns may not be properly supported, for example those using the character 
classes `\i` and `\c` which are specific to W3C XML Schema.

### Public API
The public types of the `AntaniXml` namespace constitute the public API.
Users of the library are expected to interact only with the `Schema` class and 
sometimes with the `CustomGenerators` class.

The rest of the library is organized in modules.
Even if they are public they are not designed to be directly used by C# client code.


*)
