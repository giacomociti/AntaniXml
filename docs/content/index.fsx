(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/AntaniXml"
#r "FsCheck.dll"
#r "System.Xml.Linq"
#r "AntaniXml.dll"
open AntaniXml

(**

AntaniXml is a .NET library for generating random xml based on a schema.

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      It can be <a href="https://nuget.org/packages/AntaniXml">installed from NuGet</a>:
      <pre>PM> Install-Package AntaniXml</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Example
-------
The basic usage from C# is shown [here][readme] but the library is developed in [F#](http://fsharp.org) so here's an F# version of the same example:

*)

let gen = XmlElementGenerator.CreateFromSchemaUri("po.xsd", "purchaseOrder", "")
let samples = gen.Generate 10

(**

There is also a `GenerateInfinite` method allowing never ending sequences of random xml to be created on demand.
This may be handy for stress testing so that you don't have to produce and store up-front huge amounts of data.



Documentation
-----------------------

 * [Tutorial](tutorial.html) contains a further explanation of this library.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library.
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. 

The library is available under Public Domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/giacomociti/AntaniXml/tree/master/docs/content
  [gh]: https://github.com/giacomociti/AntaniXml
  [issues]: https://github.com/giacomociti/AntaniXml/issues
  [readme]: https://github.com/giacomociti/AntaniXml/blob/master/README.md
  [license]: https://github.com/giacomociti/AntaniXml/blob/master/LICENSE.txt
*)
