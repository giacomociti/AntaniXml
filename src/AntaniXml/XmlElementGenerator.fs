namespace AntaniXml

// Note: the API provided by the following types may change in future releases.

open XsdDomain
open XsdFactory
open XmlGenerator
open System.Xml
open System.Xml.Linq
open System.Xml.Schema

// an OO API suitable also for c# client code
/// Random generator of xml elements based on a schema definition
type IXmlElementGenerator = 
    abstract Generate : n:int -> array<XElement>
    abstract GenerateInfinite : unit -> seq<XElement>


/// This is the public API of AntaniXml.
/// It provides random generators for global elements defined in the given Xml Schema.
type Schema(xmlSchemaSet: XmlSchemaSet) =
    
    /// Factory method to load a schema from its Uri.
    static member CreateFromUri schemaUri = Schema(xmlSchemaSetFromUri schemaUri)
    /// Factory method to parse a schema from plain text.
    static member CreateFromText schemaText = Schema(xmlSchemaSet schemaText)

    /// Helper method providing validation.
    member x.Validate element = validate xmlSchemaSet element
    /// Helper method providing validation.
    member x.Validate element = validateElement xmlSchemaSet element

    /// Global elements defined in the given schema.
    member x.GlobalElements = 
        xmlSchemaSet.GlobalElements.Names
        |> ofType<XmlQualifiedName>

    /// The object created embeds a random generator of xml elements and it is
    /// suitable for property based testing with FsCheck.
    /// 
    /// ## Parameters
    ///
    /// - `elementName` - Qualified name of the element for which to 
    /// create an istance of `Arbitrary`. A corresponding global  
    /// element definition is expected in the schema.
    /// - `customizations` - Custom generators to override the default behavior.
    member x.Arbitrary (elementName, (customizations: CustomGenerators)) =
        xmlSchemaSet.GlobalElements.Values
        |> ofType<System.Xml.Schema.XmlSchemaElement>
        |> Seq.tryFind (fun e -> e.QualifiedName = elementName)
        |> function 
        | None -> failwithf "element %A not found" elementName
        | Some e -> e
        |> xsdElement
        |> genElementCustom (customizations.ToMaps())
        |> FsCheck.Arb.fromGen

    /// The object created embeds a random generator of xml elements and it is
    /// suitable for property based testing with FsCheck.
    /// 
    /// ## Parameters
    ///
    /// - `elementName` - Qualified name of the element for which to 
    /// create an istance of `Arbitrary`. A corresponding global  
    /// element definition is expected in the schema.
    member x.Arbitrary elementName =
        x.Arbitrary (elementName, CustomGenerators())


    /// Creates a random generator of xml elements with the given qualified name.
    /// A corresponding global element definition is expected in the schema.
    /// 
    /// ## Parameters
    ///
    /// - `elementName` - Qualified name of the element for which to 
    /// create an instance of `IXmlElementGenerator`.
    member x.Generator elementName =
        let generator = x.Arbitrary(elementName).Generator
        let size = 5 // todo variable size
        { new IXmlElementGenerator with
              
              member x.Generate n = 
                  generator
                  |> FsCheck.Gen.sample size n
                  |> Array.ofList
              
              member x.GenerateInfinite() = 
                  seq { while true do yield! x.Generate 100 } }
        
    
        

/// Factory for random generators of xml elements
[<System.ObsoleteAttribute>]
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
