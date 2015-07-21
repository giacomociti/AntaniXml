namespace AntaniXml

open XsdDomain
open XsdFactory
open XmlGenerator
open System.Xml.Linq

// an OO API suitable also for c# client code
/// Random generator of xml elements based on a schema definition
type IXmlElementGenerator = 
    abstract Generate : n:int -> array<XElement>
    abstract GenerateInfinite : unit -> seq<XElement>


/// Factory for random generators of xml elements
type XmlElementGenerator = 
    
    static member private createGen (xmlSchema, elmName, elmNs) = 
        let name = 
            { Namespace = elmNs
              Name = elmName }
        (xsdSchema xmlSchema).Elements
        |> List.tryFind (fun e -> e.ElementName = name)
        |> Option.map genElement
        |> function 
        | None -> failwithf "element %s:%s not found" elmNs elmName
        | Some g -> g
    
    static member private createGen' (xmlSchema, elmName, elmNs) = 
        let elementGenerator = XmlElementGenerator.createGen (xmlSchema, elmName, elmNs)
        let size = 5
        { new IXmlElementGenerator with
              
              member x.Generate n = 
                  elementGenerator
                  |> FsCheck.Gen.sample size n
                  |> Array.ofList
              
              member x.GenerateInfinite() = 
                  seq { while true do yield! x.Generate 100 } }
    
    /// Creates a random generator of xml elements with the given name and
    /// namespace. The element definition is expected at the top level of
    /// the provided schema.
    /// ## Parameters
    ///
    /// - `xsdUri` - Uri of the xsd schema.
    /// - `elmName` - Name of the element for which to create a generator.
    /// - `elmNs` - element namespace; may be empty.
    static member CreateFromSchemaUri(xsdUri, elmName, elmNs) = 
        XmlElementGenerator.createGen' (xmlSchemaSetFromUri xsdUri, elmName, elmNs)
    
    /// Creates a random generator of xml elements with the given name and
    /// namespace. The element definition is expected at the top level of
    /// the xsd schema provided as a text string
    /// ## Parameters
    ///
    /// - `xsdText` - xsd schema as a text string.
    /// - `elmName` - Name of the element for which to create a generator.
    /// - `elmNs` - element namespace; may be empty.
    static member CreateFromSchemaText(xsdText, elmName, elmNs) = 
        XmlElementGenerator.createGen' (xmlSchemaSet xsdText, elmName, elmNs)
    
    /// The object created embeds a random generator of xml elements and it is
    /// suitable for property based testing with FsCheck.
    static member CreateArbFromSchemaUri(xsdUri, elmName, elmNs) = 
        XmlElementGenerator.createGen (xmlSchemaSetFromUri xsdUri, elmName, elmNs) |> FsCheck.Arb.fromGen
    
    /// The object created embeds a random generator of xml elements and it is
    /// suitable for property based testing with FsCheck.
    static member CreateArbFromSchemaText(xsdText, elmName, elmNs) = 
        XmlElementGenerator.createGen (xmlSchemaSet xsdText, elmName, elmNs) |> FsCheck.Arb.fromGen
