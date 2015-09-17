using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AntaniXml;
using FsCheck;

namespace CSharpTests
{
    [TestClass]
    public class UnitTests
    {
        static CustomGenerators GetCustomGenerators(string file)
        {
            switch (file)
            {
                case "xmlopts":
                    var nsOpts = "http://www.altova.com/xmlopts";
                    return new CustomGenerators()
                        .ForElement(new XmlQualifiedName("option", nsOpts),
                            Gen.Constant(new XElement(XName.Get("core.linenumbers", nsOpts), true)));

                case "config":
                    var nsConfig = "http://www.altova.com/schemas/altova/raptorxml/config";
                    return new CustomGenerators()
                        .ForElement(new XmlQualifiedName("option", nsConfig),
                            Gen.Constant(new XElement(XName.Get("http.environment", nsConfig), "foo")));

                default: return new CustomGenerators();
            }
        }

        //.*:.*

        [TestMethod]
        public void XegerIssue1()
        {
            var x = new Fare.Xeger(".*");

            // loops
            //x = new Fare.Xeger(".*.*");
            //var x = new Fare.Xeger(".*:.*");
        }

        //[TestMethod]
        //public void XegerIssue()
        //{
        //    var x = new Fare.Xeger(@"\p{5}");
        //    var sample = x.Generate();
        //    Assert.AreEqual("ppppp", sample);
        //    // was meant to produce something like 3.2.7
        //    var y = new Fare.Xeger(@"\p{N}\.\p{N}\.\p{N}");
        //    //var x = new Fare.Xeger(@"\p{ N}\.\p{ N}");
        //}

        [TestMethod]
        public void UBL_CommonAggregateComponents()
        {
            CheckSchema(@"C:\temp\Schemas\UBL\files\common\UBL-CommonAggregateComponents-2.1.xsd");
        }

        [TestMethod]
        public void CheckSchemas()
        {
            var path = @"C:\temp\Schemas\UBL";

            foreach (var xsd in Directory.EnumerateFiles(path, "*.xsd", SearchOption.AllDirectories)
                .GroupBy(x => Path.GetFileNameWithoutExtension(x))
                .Select(x => x.First()))
            {
                CheckSchema(xsd);
            }

            //var file = Path.Combine(path, @"Common2015\Schemas\voicexml\files\vxml-attribs.xsd");
            //CheckSchema(file);
        }

        static void Error(string category, string msg)
        {
            Console.WriteLine("ERROR " + category + ": " + msg);
        }

        // UBL-CommonAggregateComponents-2.1
        static void CheckSchema(String uri)
        {
            //Console.WriteLine("checking " + uri);
            try
            {
                var schema = Schema.CreateFromUri(uri);
                var cust = GetCustomGenerators(Path.GetFileNameWithoutExtension(uri));
                foreach (var elm in schema.GlobalElements)
                {
                    //Console.Write("element " + elm.ElementName.Name);
                    var gen = schema.Arbitrary(elm, cust).Generator;
                    // collect samples of increasing size
                    var samples = gen.Sample(0, 1)
                        .Concat(gen.Sample(5, 4))
                        .Concat(gen.Sample(20, 4))
                        .Concat(gen.Sample(1000, 1))
                        .ToArray();
                    var invalid = String.Join(Environment.NewLine,
                        samples
                        .Select(x => schema.Validate(x))
                        .Where(x => x.IsFailure)
                        .Select(x => string.Join(Environment.NewLine,
                            x.Errors.Select(e => e.Message))));
                    var allValid = invalid == "";
                    //Console.WriteLine(allValid ? " all valid" : " validation errors");
                    if (!allValid)
                    {
                        Console.WriteLine("VALIDATION_ERROR for " + elm + " in " + uri);
                        Console.WriteLine(invalid);
                    }
                }
            }
            catch (XmlSchemaException e)
            {
                //Error("XmlSchemaException", e.Message);
            }
            catch (Exception e)
            {
                if (e.Source == "Fare")
                {
                    //Error("Fare", e.Message);
                }
                else if (e.Message.EndsWith("is an invalid character."))
                {
                    //Error("is an invalid character.", e.Message);
                }
                else if (e.Message.StartsWith("unsupported type"))
                {
                    //Error("unsupported type", e.Message); 
                }
                else if (e.Message.StartsWith("cannot mix constraints"))
                {
                    //Error("cannot mix constraints", e.Message); 
                }
                else if (e.Source == "AntaniXml")
                {
                    Error("AntaniXml", e.Message);
                }
                else
                {
                    Error("Unknown", e.Message);
                }

            }
        }


        [TestMethod]
        public void AbstractRecursive()
        {
            var xsd = XsdFactory.xmlSchemaSetFromUri("Formula.xsd");
            var els = xsd.GlobalElements.Values.OfType<XmlSchemaElement>();
            var form = els.Single(x => x.Name == "Formula");
            var and = els.Single(x => x.Name == "And");
            var seq = (XmlSchemaSequence)((XmlSchemaComplexType)and.ElementSchemaType).ContentTypeParticle;
            var formRef =(XmlSchemaElement)seq.Items[0];
            Assert.IsTrue(form.IsAbstract);
            Assert.IsTrue(form.RefName.IsEmpty);
            Assert.IsFalse(form.QualifiedName.IsEmpty);

            
            Assert.IsFalse(formRef.IsAbstract); // Notice this
            Assert.AreEqual(form.QualifiedName, formRef.QualifiedName);
            Assert.AreEqual(formRef.QualifiedName, formRef.RefName);

            Assert.AreSame(form.ElementSchemaType, formRef.ElementSchemaType);


            var schema = new Schema(xsd);

            var gen = schema.Arbitrary(new XmlQualifiedName("Formula")).Generator;
            var samples = gen.Sample(10, 10);

            

            samples.ToList().ForEach(Console.WriteLine);

            var allvalid = samples.All(x => schema.Validate(x).IsSuccess);
            Assert.IsTrue(allvalid);
        }

    }
}
