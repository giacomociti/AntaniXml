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

(**
Customizations
========================

This article is about how you can customize the generated output.


### Simply change the output

For some schemas the generator may produce something not correct (for example ignoring identity constraints).
Or maybe the generated values simply look ugly to you (you may want meaningful words instead of random gibberish).
Obviously one option is to change the produced output by using XSLT or by any other mean. Since the output is
an `XElement`, the `System.Xml.Linq` API is already well suited for this task:

*)

let samples = 
    Schema.CreateFromUri("foo")
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

### Providing you own generators

Allowing custom generators is of course a superior alternative.

To keep things simple, custom generators are allowed only for elements and for global complex types.
The only way to customize the generation of simple types and attributes is to provide generators for
the enclosing elements or global complex types. Hopefully this is an acceptable limitation for most practical purposes.

FsCheck provides excellent documentation about creating custom generators.
Anyway there is also an option to provide only a mapping function, so the default generators are used but the generated elements
are then transformed with a custom function.
Custom generators are expected to produce `XElement` instances.
In case of complex types the name of the generated element actually is not important because it will be replaced
by the proper element name required in each context.

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
So, in case the schema is designed in a *Russian doll* style, custom generators for elements are a better bet.
Just beware of (nasty) schemas where two distinct element definitions describe elements with the exact same name 
but different content models. In this case a custom generator may not be used because it would apply to
both element definitions.








*)
