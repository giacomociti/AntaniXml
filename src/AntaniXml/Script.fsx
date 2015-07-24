// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.

#load "Scripts\load-references.fsx"
#load "Scripts\load-project.fsx"
open AntaniXml

let dir = @"C:\Users\372giaciti\Source\Repos\AntaniXml\tests\AntaniXml.Tests"
let xsdUri = System.IO.Path.Combine(dir, "po.xsd")
let gen = XmlElementGenerator.CreateFromSchemaUri(xsdUri, "PurchaseOrder", "")


