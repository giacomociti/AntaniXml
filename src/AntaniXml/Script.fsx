//#load "Scripts\load-references.fsx"
//#load "Scripts\load-project.fsx"

#I @"..\..\..\FSharp.Data\bin"
#I @"..\..\..\AntaniXml\bin\AntaniXml"

#r "FsCheck.dll"
#r "AntaniXml.dll"
#r "FSharp.Data.dll"
#r "System.Xml.Linq"

open AntaniXml
open System.IO
open FSharp.Data
open System.Xml.Linq
open System.Xml

[<Literal>]
let dir = @"C:\temp"

[<Literal>]
let xmlSamplesFile = "samples.xml"

let xsdUri = Path.Combine(dir, "po.xsd")
let samplesUri = Path.Combine(dir, xmlSamplesFile)

let samples = 
    XmlElementGenerator
        .CreateFromSchemaUri(xsdUri, "purchaseOrder", "")
        .Generate(5)
XElement(XName.Get("root"), samples).Save(samplesUri)

type po = XmlProvider<xmlSamplesFile, SampleIsList = true, ResolutionFolder = dir>

open System.Xml
let x = new System.DateTime(1992, 3, 29, 2, 0, 0)
let local = XmlConvert.ToString(x, XmlDateTimeSerializationMode.Local)                  // "1992-03-29T02:00:00+01:00"
let utc = XmlConvert.ToString(x, XmlDateTimeSerializationMode.Utc)                      // "1992-03-29T02:00:00Z"
let unspecified = XmlConvert.ToString(x, XmlDateTimeSerializationMode.Unspecified)      // "1992-03-29T02:00:00"
let roundtripKind = XmlConvert.ToString(x, XmlDateTimeSerializationMode.RoundtripKind)  // "1992-03-29T02:00:00"

let d = XmlConvert.ToDateTime(local, XmlDateTimeSerializationMode.Local) // 3/29/1992 3:00:00 AM
let d' = XmlConvert.ToDateTime(local, XmlDateTimeSerializationMode.Utc) // 3/29/1992 1:00:00 AM
let d''' = XmlConvert.ToDateTime(local, XmlDateTimeSerializationMode.Unspecified) // 3/29/1992 3:00:00 AM
let d'''' = XmlConvert.ToDateTime(roundtripKind, XmlDateTimeSerializationMode.RoundtripKind) // 3/29/1992 3:00:00 AM


