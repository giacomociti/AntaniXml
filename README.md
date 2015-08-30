[![Issue Stats](http://issuestats.com/github/giacomociti/AntaniXml/badge/issue)](http://issuestats.com/github/giacomociti/AntaniXml)
[![Issue Stats](http://issuestats.com/github/giacomociti/AntaniXml/badge/pr)](http://issuestats.com/github/giacomociti/AntaniXml)

# AntaniXml

AntaniXml is a .NET library for generating random xml based on a schema.
This is useful mainly for testing - especially to produce stress test data -
but also for unit and property based testing.
Of course generating samples may also help in figuring out concretely what 
kind of xml is defined by a certain schema.

The API is straightforward, given a schema file you have to choose an element 
definition to use as a template. Then you call the `Generate` method to get the 
desired number of samples:

    var samples = Schema.CreateFromUri("po.xsd")
        .Generator(new XmlQualifiedName("purchaseOrder"))
        .Generate(10);

You can also customize the generators. Comprehensive documentation is available on
the [project site](http://giacomociti.github.io/AntaniXml/).



