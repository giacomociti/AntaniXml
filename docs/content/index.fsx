(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/AntaniXml"
#r "FsCheck.dll"
#r "System.Xml.Linq"

(**

AntaniXml is a .NET library for generating random xml based on a schema.

This is useful mainly for testing, especially to produce stress test data, 
but also for unit and property based testing.
Of course generating samples may also help in figuring out concretely what 
kind of xml is defined by a certain schema.

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The library can be <a href="https://nuget.org/packages/AntaniXml">installed from NuGet</a>:
      <pre>PM> Install-Package AntaniXml</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

AntaniXml is developed in [F#](http://fsharp.org) but the public API is usable also from C#.
In fact most examples are given in both languages.

Example
-------

The first example is straightforward, given a schema file you have to choose an element 
definition to use as a template. Then you call the `Generate` method to get the 
desired number of samples:

*)
#r "AntaniXml.dll"
open AntaniXml
open System.Xml

let samples = 
    Schema.CreateFromUri("po.xsd")
          .Generator(XmlQualifiedName("purchaseOrder"))
          .Generate(10)

(**

The C# code is almost the same:

    var samples = Schema.CreateFromUri("po.xsd")
        .Generator(new XmlQualifiedName("purchaseOrder"))
        .Generate(10);


There is also a `GenerateInfinite` method allowing never ending sequences of random xml to be created on demand.
This may be handy for stress testing so that you don't have to produce and store up-front huge amounts of data.


AntaniXml is built on top of the awesome [FsCheck](https://github.com/fscheck/FsCheck) library, 
and you can use it for property based testing with FsCheck.
Property based testing is an interesting and effective technique. 
FsCheck is well documented to get you started. This library provides `Arbitrary` instances 
so that you can feed tests with random (but valid) xml.

While the first example shows the easiest way to use the library, there is another public API 
allowing finer control on random generators. This API also exposes FsCheck types so that you can 
directly use their advanced features. Examples are available in the rest of the documentation.



Documentation
-----------------------

 * [Tutorial](tutorial.html) contains a further explanation of this library.

 * [Customizations](customizations.html) hints at ways to tweak generators.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library.
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. 

The library is available under a public domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/giacomociti/AntaniXml/tree/master/docs/content
  [gh]: https://github.com/giacomociti/AntaniXml
  [issues]: https://github.com/giacomociti/AntaniXml/issues
  [readme]: https://github.com/giacomociti/AntaniXml/blob/master/README.md
  [license]: https://github.com/giacomociti/AntaniXml/blob/master/LICENSE.txt
*)
