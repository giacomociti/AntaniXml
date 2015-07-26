// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.

#load "Scripts\load-references.fsx"
#load "Scripts\load-project.fsx"
open AntaniXml
open AntaniXml.XsdFactory
open AntaniXml.XmlGenerator
open FsCheck

let dir = @"C:\Users\372giaciti\Source\Repos\AntaniXml\tests\AntaniXml.Tests"
let (++) x y = System.IO.Path.Combine(x, y)
let xsdUri = System.IO.Path.Combine(dir, "po.xsd")
let gen = XmlElementGenerator.CreateFromSchemaUri(xsdUri, "purchaseOrder", "")
gen.Generate(10)
|> Array.iteri (fun i e -> e.Save(dir ++ "out" ++ sprintf "po%i.xml" i))



let isValid schemaSet (e: System.Xml.Linq.XElement) =
    let valid = validate schemaSet
    match e.ToString() |> valid  with
    | true, _ -> true
    | false, msg ->
        printfn "%s %A" msg e
        false    

let xsd =
    //dir ++ "bin" ++ "debug" ++ "wip.xsd"
    "http://www.topografix.com/GPX/1/1/gpx.xsd"
    |> xmlSchemaSetFromUri 

let samples = 
    (xsdSchema xsd).Elements 
    |> List.map genElement
    |> List.collect (Gen.sample 5 10)

let allValid =
    samples
    |> List.map (isValid xsd)
    |> List.forall id

let outDir = @"c:\temp\xsdTest"
samples
|> List.iteri (fun i x -> x.Save(outDir ++ (sprintf "sample%i.xml" i)))


