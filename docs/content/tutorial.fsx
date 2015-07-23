(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/AntaniXml"
#r "FsCheck.dll"
#r "System.Xml.Linq"

(**
Introducing your project
========================

Say more

*)
#r "AntaniXml.dll"
open AntaniXml

let gen = XmlElementGenerator.CreateFromSchemaUri("po.xsd", "PurchaseOrder", "")
let samples = gen.Generate 10

(**
Some more info
*)
