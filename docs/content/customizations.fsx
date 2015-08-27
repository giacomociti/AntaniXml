(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I @"..\..\bin\AntaniXml"
#r "FsCheck.dll"
#r "System.Xml.Linq"
#r "AntaniXml.dll"

open AntaniXml
open System.Xml.Linq

(**
Customizations
========================

This article is about how you can customize the generated output.


### Simply change the output

For some schemas the generator may produce something not correct (for example ignoring identity constraints).
Or maybe the generated values simply look ugly to you (you may want meaningful words instead of random gibberish).
One option is to change the produced output by using XSLT or by any other mean. Since the output is
an `XElement`, the `System.Xml.Linq` API is already well suited for this task:

*)

let samples = 
    XmlElementGenerator
        .CreateFromSchemaUri("foo", "e1", "")
        .Generate 10

samples
|> Seq.mapi (fun i xml ->
    for bar in xml.Descendants(XName.Get("bar")) do
        bar.Attribute(XName.Get("id")).Value <- i.ToString()
    xml)      
|> Seq.iter (printfn "%A") 
    

(**

The same in C# is

        var samples = XmlElementGenerator
            .CreateFromSchemaUri("foo.xsd", "e1", "")
            .Generate(10);

        samples.Descendants("bar")
            .Select((bar, i) =>
                {
                    bar.Attribute("id").Value = i.ToString();
                    return bar;
                })
            .ToList()
            .ForEach(Console.WriteLine);
                   


The above example is trivial but you get the idea. If you don't like imperative code
you can map the element to another one, but if you just need to do little fixes the
imperative approach may be just fine.

### Providing you own generators

Allowing custom generators is of course a superior alternative.
Support for it is still work in progress so stay tuned or, even better, fork the project and contribute :)

*)
