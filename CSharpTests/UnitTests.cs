using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AntaniXml;
using FsCheck;
using Fare;
using System.Collections.Generic;

namespace CSharpTests
{
    [TestClass]
    public class UnitTests
    {
        static CustomGenerators GetCustomGenerators(string file)
        {
            switch (file)
            {
                //case "xmlopts":
                //    var nsOpts = "http://www.altova.com/xmlopts";
                //    return new CustomGenerators()
                //        .ForElement(new XmlQualifiedName("option", nsOpts),
                //            Gen.Constant(new XElement(XName.Get("core.linenumbers", nsOpts), true)));

                //case "config":
                //    var nsConfig = "http://www.altova.com/schemas/altova/raptorxml/config";
                //    return new CustomGenerators()
                //        .ForElement(new XmlQualifiedName("option", nsConfig),
                //            Gen.Constant(new XElement(XName.Get("http.environment", nsConfig), "foo")));

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
        public void ImportCreditRegistry()
        {
            CheckSchema(@"C:\temp\schemas\ImportCreditRegistry_v.3.xsd");
        }

        [TestMethod]
        public void CheckSchemas()
        {
            var path = @"C:\temp\Schemas\";

            foreach (var xsd in Directory.EnumerateFiles(path, "*.xsd", SearchOption.AllDirectories)
                .GroupBy(x => Path.GetFileNameWithoutExtension(x))
                .Select(x => x.First())
                //.Take(10)
                )
            {
                CheckSchema(xsd);
                //WriteTP(xsd);
            }

            //var file = Path.Combine(path, @"Common2015\Schemas\voicexml\files\vxml-attribs.xsd");
            //CheckSchema(file);
        }

        void WriteTP(string uri)
        {
//#r "../../bin/FSharp.Data.dll"
//#r "System.Xml.Linq.dll"
            Func<string, string> clean = x => 
                x.Replace('.', '_')
                 .Replace(':', '_')
                 .Replace('-', '_')
                 .Replace('\\', '_')
                 .Replace('/', '_');
    try
            {
                var schema = Schema.CreateFromUri(uri);
                schema.GlobalElements.GroupBy(x => x.Namespace).ToList().ForEach(group =>
                {
                    var ns = clean(group.Key);
                    Console.WriteLine("module {0}_{1} =", clean(uri), ns);
                    foreach (var elm in group)
                    {
                        var path = Path.GetDirectoryName(uri);
                        var format = @"    type ``{0}`` = XmlProviderFromSchema< @""{1}"", ElementName = ""{2}"", ElementNamespace = ""{3}"", ResolutionFolder = ""{4}"" > ";
                        Console.WriteLine(format, clean(elm.Name), uri, elm.Name, elm.Namespace, path);
                    }
                    Console.WriteLine();
                });

                   
            }
            catch (XmlSchemaException e)
            {
                Console.WriteLine(@"// XmlSchemaException: " + uri + ": " + e.Message);
            }

        }

        static void Log(string category, string msg)
        {
            //Console.WriteLine("ERROR " + category + ": " + msg);
            File.WriteAllText("xsd.log", $"{DateTime.Now} {category}: {msg}");
        }

        // UBL-CommonAggregateComponents-2.1
        static void CheckSchema(String uri)
        {
            //Console.WriteLine("checking " + uri);
            try
            {
                var schema = Schema.CreateFromUri(uri);
                Log("schema", uri);
                var cust = GetCustomGenerators(Path.GetFileNameWithoutExtension(uri));
                foreach (var elm in schema.GlobalElements)
                {
                    Log("element ", elm.Name);
                    var gen = schema.Arbitrary(elm, cust).Generator;
                    // collect samples of increasing size

                    var samples = Enumerable.Range(0, 10).
                        SelectMany(x => gen.Sample(x*10, 5))
                        .ToArray();

                    //var samples = gen.Sample(10, 5)
                    //    //.Concat(gen.Sample(5, 4))
                    //    //.Concat(gen.Sample(20, 4))
                    //    //.Concat(gen.Sample(10000, 9))
                    //    .ToArray();
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
                        Log("VALIDATION_ERR", elm + " in " + uri);
                        Log("Invalid element", invalid);
                    }
                }
            }
            catch (XmlSchemaException e)
            {
                Log("XmlSchemaException", e.Message);
            }
            catch (Exception e)
            {
                if (e.Source == "Fare")
                {
                    Log("Fare", e.Message);
                }
                else if (e.Message.EndsWith("is an invalid character."))
                {
                    Log("is an invalid character.", e.Message);
                }
                else if (e.Message.StartsWith("unsupported type"))
                {
                    Log("unsupported type", e.Message); 
                }
                else if (e.Message.StartsWith("cannot mix constraints"))
                {
                    Log("cannot mix constraints", e.Message.Substring(0, 100)); 
                }
                else if (e.Source == "AntaniXml")
                {
                    Log("AntaniXml", e.Message);
                }
                else
                {
                    Log("Unknown", e.Message);
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
            var formRef = (XmlSchemaElement)seq.Items[0];
            Assert.IsTrue(form.IsAbstract);
            Assert.IsTrue(form.RefName.IsEmpty);
            Assert.IsFalse(form.QualifiedName.IsEmpty);

            Assert.IsFalse(formRef.IsAbstract); // Notice this
            Assert.AreEqual(form.QualifiedName, formRef.QualifiedName);
            Assert.AreEqual(formRef.QualifiedName, formRef.RefName);

            Assert.AreSame(form.ElementSchemaType, formRef.ElementSchemaType);


            var schema = new Schema(xsd);

            var gen = schema.Arbitrary(new XmlQualifiedName("Formula")).Generator;
            var samples = gen.Sample(5, 500);

            samples.ToList().ForEach(Console.WriteLine);
            var allvalid = samples.All(x => schema.Validate(x).IsSuccess);
            Assert.IsTrue(allvalid);
        }


        [TestMethod]
        public void FareGen()
        {
            var pattern = "[a-z]+";
            var items1 = Xegers(pattern);
            var items2 = Xegers(pattern);
            var same = Enumerable.SequenceEqual(items1, items2);
            Assert.IsFalse(same);
        }

        [TestMethod]
        public void FareGenRnd()
        {
            var pattern = "[a-z]+";
            var rnd = new System.Random();
            var items1 = Xegers(pattern, rnd);
            var items2 = Xegers(pattern, rnd);
            var same = Enumerable.SequenceEqual(items1, items2);
            Assert.IsFalse(same);
        }

        static IEnumerable<string> Xegers(string pattern)
        {
            var xeger = new Xeger(pattern);
            var result = Enumerable.Range(0, 10).Select(x => xeger.Generate());
            result.ToList().ForEach(Console.WriteLine);
            return result;
        }

        static IEnumerable<string> Xegers(string pattern, System.Random rnd)
        {
            var xeger = new Xeger(pattern, rnd);
            var result = Enumerable.Range(0, 10).Select(x => xeger.Generate());
            result.ToList().ForEach(Console.WriteLine);
            return result;
        }
    }
}
