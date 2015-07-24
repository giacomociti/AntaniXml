[![Issue Stats](http://issuestats.com/github/giacomociti/AntaniXml/badge/issue)](http://issuestats.com/github/giacomociti/AntaniXml)
[![Issue Stats](http://issuestats.com/github/giacomociti/AntaniXml/badge/pr)](http://issuestats.com/github/giacomociti/AntaniXml)

# AntaniXml

AntaniXml is a .NET library for generating random xml based on a schema.
This is useful mainly for testing, especially to produce stress test data, but also for unit and property based testing.
Of course generating samples may also help in figuring out concretely what kind of xml is defined by a certain schema.

The API is straightforward, just obtain a generator from a factory method:

	var gen = XmlElementGenerator.CreateFromSchemaUri("po.xsd", 
		elmName: "purchaseOrder", elmNs: string.Empty);

specifyng the xsd file and the element definition within the schema to use as a template.
Then you call the Generate method to get the desired number of samples:

	XElement[] samples = gen.Generate(10);

AntaniXml is built on top of the awesome [FsCheck] (https://github.com/fscheck/FsCheck) library, and it's easy to use 
for property based testing with FsCheck.
Property based testing is an interesting and effective technique. FsCheck is well documented to get you started.
Here we provide *Arbitrary* instances for the given schemas so that you can feed tests with random (but valid) xml.



