using AntaniXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CSharp.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            //[firstExample]
            var gen = XmlElementGenerator.CreateFromSchemaUri("po.xsd",
                elmName: "purchaseOrder", elmNs: string.Empty);
            XElement[] samples = gen.Generate(10);
            //[/firstExample]
        }


    }
}
