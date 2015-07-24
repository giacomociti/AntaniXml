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
      The AntaniXml library can (NOT YET) be <a href="https://nuget.org/packages/AntaniXml">installed from NuGet</a>:
      <pre>PM> Install-Package AntaniXml</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Example
-------

The API is straightforward, just obtain a generator from a factory method:

	var gen = XmlElementGenerator.CreateFromSchemaUri("po.xsd", 
		elmName: "PurchaseOrder", elmNs: string.Empty);

specifyng the xsd file and the element definition within the schema to use as a template.
Then you call the Generate method to get the desired number of samples:

	XElement[] samples = gen.Generate(10);

The above example is in C#. The F# equivalent is the following:

*)

printfn "hello = %i" <| Library.hello 0

(**
In fact the library is implemented in F#. 

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
