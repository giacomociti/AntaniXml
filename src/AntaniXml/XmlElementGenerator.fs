namespace AntaniXml

// Note: the API provided by the following types may change in future releases.

open XsdDomain
open XsdFactory
open XmlGenerator
open System.Xml
open System.Xml.Linq
open System.Xml.Schema
open System.Collections.Generic

// an OO API suitable also for c# client code
/// Random generator of xml elements based on a schema definition
type IXmlElementGenerator = 
    abstract Generate : n:int -> array<XElement>
    abstract GenerateInfinite : unit -> seq<XElement>


/// This is the public API of AntaniXml.
/// It provides random generators for global elements defined in the given Xml Schema.
type Schema(xmlSchemaSet: XmlSchemaSet) =

    let getElm name =
        xmlSchemaSet.GlobalElements.Item name :?> XmlSchemaElement

    let subst' =
        let alt =
            xmlSchemaSet.GlobalElements.Values
            |> ofType<XmlSchemaElement>
            |> Seq.filter (fun e -> not e.SubstitutionGroup.IsEmpty)
            |> Seq.groupBy (fun e -> e.SubstitutionGroup)
            |> Seq.map (fun (name, values) -> getElm name, values |> List.ofSeq)
            |> dict
        fun e -> if alt.ContainsKey e then alt.Item e else []

    let subst = 
        memoize <| fun element ->
        let items = HashSet()
        let rec collect elm =
            for x in subst' elm do 
                if items.Add x then collect x 
        collect element 
        items |> List.ofSeq

    let hasCycles = 
        memoize <| fun element ->
        let items = HashSet<XmlSchemaObject>()
        let rec closure (obj: XmlSchemaObject) =
            let nav innerObj =
                if items.Add innerObj then closure innerObj
            match obj with
            | :? XmlSchemaElement as e -> 
                if e.RefName.IsEmpty then
                    nav e.ElementSchemaType
                    (subst e) |> Seq.iter nav
                else nav (getElm e.RefName)
            | :? XmlSchemaComplexType as c -> 
                nav c.ContentTypeParticle
            | :? XmlSchemaGroupRef as r -> 
                nav r.Particle
            | :? XmlSchemaGroupBase as x -> 
                x.Items 
                |> ofType<XmlSchemaObject> 
                |> Seq.iter nav
            | _ -> ()
        closure element
        items.Contains element


    /// Factory method to load a schema from its Uri.
    static member CreateFromUri schemaUri = Schema(xmlSchemaSetFromUri schemaUri)
    /// Factory method to parse a schema from plain text.
    static member CreateFromText schemaText = Schema(xmlSchemaSet schemaText)

    /// Helper method providing validation.
    member x.Validate element = validate xmlSchemaSet element
    /// Helper method providing validation.
    member x.Validate element = validateElement xmlSchemaSet element
    /// Helper method providing validation.
    member x.IsValid element = 
        match validateElement xmlSchemaSet element with
        | Success -> true
        | _ -> false

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

        if xmlSchemaSet.GlobalElements.Contains elementName then
            xsdElement (getElm elementName) getElm subst hasCycles
            |> genElementCustom (customizations.ToMaps()) 
            |> FsCheck.Arb.fromGen
        else failwithf "element %A not found" elementName

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
              
//              member x.Generate n = 
//                  generator
//                  |> FsCheck.Gen.sample size n
//                  |> Array.ofList
              
//              member x.GenerateInfinite() = 
//                  seq { while true do yield! x.Generate 100 } }
              
              member x.Generate n = 
                  x.GenerateInfinite()
                  |> Seq.take n
                  |> Array.ofSeq

              member x.GenerateInfinite() = 
                  seq { while true do 
                            yield! (FsCheck.Gen.sample 50 10 generator)
                            yield! (FsCheck.Gen.sample 10 10 generator)
                            yield! (FsCheck.Gen.sample 90 10 generator)
                            yield! (FsCheck.Gen.sample 20 10 generator)
                            yield! (FsCheck.Gen.sample 80 10 generator)
                            yield! (FsCheck.Gen.sample 40 10 generator)
                            yield! (FsCheck.Gen.sample 60 10 generator)
                            yield! (FsCheck.Gen.sample 50 10 generator)
                            yield! (FsCheck.Gen.sample 10000 5 generator) } }
