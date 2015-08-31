(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I @"..\..\bin\AntaniXml"
#r "FsCheck.dll"
#r "System.Xml.Linq"
#r "AntaniXml.dll"

open AntaniXml
open System.Xml
open System.Xml.Linq

let xsdText = """
    <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" 
        elementFormDefault="qualified" attributeFormDefault="unqualified">
        <xs:complexType name="barType">
            <xs:attribute name="id" type="xs:string" use="required" />
        </xs:complexType>
        <xs:element name="foo">
            <xs:complexType>
	            <xs:sequence>
                    <xs:element name="bar" type="barType" />
		            <xs:element name="baz" type="xs:string" />
	            </xs:sequence>
            </xs:complexType>
        </xs:element>
    </xs:schema>"""

(**
Customizations
========================

This article is about how you can customize the generated output.


### Simply change the output

For some schemas the generator may produce something not correct (for example ignoring identity constraints).
Or maybe the generated values simply look ugly to you (you may want meaningful words instead of random gibberish).
Obviously one option is to change the produced output by using XSLT or by any other means. Since the output is
an `XElement`, the `System.Xml.Linq` API is already well suited for this task:

*)

let samples = 
    Schema.CreateFromUri("foo.xsd")
          .Generator(XmlQualifiedName("e1"))
          .Generate(10)

samples
|> Seq.mapi (fun i xml ->
    for bar in xml.Descendants(XName.Get("bar")) do
        bar.Attribute(XName.Get("id")).Value <- i.ToString()
    xml)      
|> Seq.iter (printfn "%A") 
    
(**
The same in C# is:

    var samples = Schema.CreateFromUri("foo.xsd")
        .Generator(new XmlQualifiedName("e1"))
        .Generate(10);

    samples.Descendants("bar")
        .Select((bar, i) =>
            {
                bar.Attribute("id").Value = i.ToString();
                return bar;
            })
        .ToList()
        .ForEach(Console.WriteLine);
                   
The above example is just to get the idea. If you don't like imperative code
you can map the element to another one, but for little fixes the
mutability allowed in `System.Xml.Linq` may be just fine.

### Providing your own generators

Custom generators are of course a superior alternative.

To keep things simple, customizations are allowed only for elements and for global complex types.
The only way to customize attributes and values of simple types, is to provide 
custom generators for the enclosing elements or global complex types. 
Hopefully this is an acceptable limitation for most practical purposes.

FsCheck provides excellent documentation about creating custom generators, both in F# and in C#.
Anyway in AntaniXml there is also an option to provide only a mapping function, so the default 
generators are still used but the generated elements are then transformed with a custom function.
Custom generators are expected to produce `XElement` instances.
In case of complex types the name of the generated element actually is not important because it
will be replaced by the proper element name required in each context.

#### Customizing a global complex type
The qualified name of the complex type is used as an identifier to pick up a custom generator for all the
elements explicitly defined to be of such a type.
Hence only global types can be customized since anonymous types lack a name to be used as an identifier.
Also in case of an element whose anonymous type is derived from a global one for which a customization is provided, 
the custom generator is *not* picked up.

#### Customizing elements
Custom generators for elements instead may have a broader reach.
When a custom generator is provided for elements with a given qualified name, that generator is used for all
elements with such a name, regardless of their type.
So, in case the schema is designed in *Russian doll* style, custom generators for elements are a better bet.
Just beware of (nasty) schemas where two distinct element definitions describe elements with the exact same name 
but different content models. In this case a custom generator may not be used because it would apply to
both element definitions.

Let's give a few concrete examples. In the following we'll be using this schema 
(assuming a string variable `xsdText` holds it):

    [lang=xml]
    <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" 
        elementFormDefault="qualified" attributeFormDefault="unqualified">
        <xs:complexType name="barType">
            <xs:attribute name="id" type="xs:string" use="required" />
        </xs:complexType>
        <xs:element name="foo">
            <xs:complexType>
	            <xs:sequence>
                    <xs:element name="bar" type="barType" />
		            <xs:element name="baz" type="xs:string" />
	            </xs:sequence>
            </xs:complexType>
        </xs:element>
    </xs:schema>

First we show a custmization for a complex type:

*)

open FsCheck

let foo = XmlQualifiedName("foo")
let barType = XmlQualifiedName("barType")
let toUpper (xml: XElement) = 
    let id = xml.Attribute(XName.Get("id"))
    id.Value <- id.Value.ToUpper() 
    xml
let cust = CustomGenerators().ForComplexType(barType, toUpper)
Schema.CreateFromText(xsdText).Arbitrary(foo, cust).Generator
|> Gen.sample 5 1
|> Seq.iter (printfn "%A")

(**

The equivalent C# code is the following:

    var foo = new XmlQualifiedName("foo");
    var barType = new XmlQualifiedName("barType");
    var cust = new CustomGenerators()
        .ForComplexType(barType, xml =>
        {
            var id = xml.Attribute("id");
            id.Value = id.Value.ToUpper();
            return xml;
        });
    var arb = Schema.CreateFromText(xsdText).Arbitrary(foo, cust);
    var samples = Gen.Sample(5, 1, arb.Generator);
    samples.ToList().ForEach(Console.WriteLine);

The main thing to notice is the `CustomGenerators` class and its method accepting a mapping
for transforming in uppercase the value of the `id` attribute for all the elements of type `barType`.
The `Arbitrary' instance created embeds a generator, and `Gen.sample` is the FsCheck method to create
samples, specifying a size (5 in the example but let's ignore the concept of size for now)
and the number of samples to create.
The element generated may look like this:

    [lang=xml]
    <foo>
      <bar id="FX(-K䤊Q&#xA;N(7RIS" />
      <baz>j慬X@V'yQy[y9O~r~rTH2kwkwkMA+dzdЁdz:$]s</baz>
    </foo>


Yes, text is gibberish (but for testing purposes usually it's a good idea to probe a system
with strange data), anyway you see the value of attribute `id` is all upper case thanks to 
the customization.

In the next example instead we are creating a generator from scratch using the FsCheck combinators.
Starting from a set of fixed string values, a generator ranging over such a set is built and then mapped
to another one that wraps the random string values in xml element.

*)

let baz = XmlQualifiedName("baz")
let abcGen = 
    Gen.elements ["a"; "b"; "c"] 
    |> Gen.map (fun x -> XElement(XName.Get("baz"), x))
let cus = CustomGenerators().ForElement(baz, abcGen)
Schema.CreateFromText(xsdText).Arbitrary(foo, cus).Generator
|> Gen.sample 5 2
|> Seq.iter (printfn "%A")

(**

Here's the C# version of the same example:

    var baz = new XmlQualifiedName("baz");
    var abcGen = Gen.Elements(new[] { "a", "b", "c" })
        .Select(x => new XElement("baz", x));
    var cus = new CustomGenerators().ForElement(baz, abcGen);
    var arb = Schema.CreateFromText(xsdText).Arbitrary(foo, cus);
    var samples = Gen.Sample(5, 2, arb.Generator);
    samples.ToList().ForEach(Console.WriteLine);


Now `baz` is no more gibberish because we took full control of the random generation:

    [lang=xml]
    <foo>
      <bar id=",B{e{e{;%;t^t^t4y4mWmWm-r-fPfPf&amp;k&amp;_" />
      <baz>b</baz>
    </foo>
    <foo>
      <bar id="ZDZB}B}BS=S５" />
      <baz>c</baz>
    </foo>



FsCheck provides many more combinators to compositionally create many kinds of generators.

*)
