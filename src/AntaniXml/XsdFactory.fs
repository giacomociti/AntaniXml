namespace AntaniXml

#nowarn "40"

/// This module is in charge of parsing xsd and creating models according to XsdDomain data types.
/// Parsing xsd is a complex task, and for this we rely on the .NET BCL library (namespace `System.Xml.Schema`).
/// Converting the Schema Object Model (SOM) provided by the BCL library into our own 
/// simpler model allows decoupling of parsing xsd and building generators.
module XsdFactory =

    open System.IO
    open System.Xml
    open System.Xml.Schema
    open Microsoft.FSharp.Reflection
    open XsdDomain

    /// naive, but we are not concerned with thread safety nor memory footprint
    let memoize f = 
        let cache = System.Collections.Generic.Dictionary()
        fun x ->
            let ok, res = cache.TryGetValue x
            if ok then res
            else let res = f x
                 cache.[x] <- res
                 res

    /// just an alias for `System.Linq.Enumerable.OfType`
    let inline ofType<'a> sequence = System.Linq.Enumerable.OfType<'a> sequence

    let private getFacet<'a when 'a :> XmlSchemaFacet> (facets: seq<XmlSchemaFacet>) = 
        facets 
        |> Seq.tryFind(fun x -> x :? 'a) 
        |> Option.map (fun x -> x.Value)   
        
    let emptyFacets = {
        Length         = None
        MinLength      = None
        MaxLength      = None
        MaxInclusive   = None
        MaxExclusive   = None
        MinInclusive   = None
        MinExclusive   = None
        TotalDigits    = None  
        FractionDigits = None
        Enumeration    = []
        Patterns       = []
        WhiteSpace     = None }          

    let private hasCycles x = 
        let items = System.Collections.Generic.HashSet<XmlSchemaObject>()
        let rec closure (obj: XmlSchemaObject) =
            let nav innerObj =
                if items.Add innerObj then closure innerObj
            match obj with
            | :? XmlSchemaElement as e -> 
                nav e.ElementSchemaType 
            | :? XmlSchemaComplexType as c -> 
                nav c.ContentTypeParticle
            | :? XmlSchemaGroupRef as r -> 
                nav r.Particle
            | :? XmlSchemaGroupBase as x -> 
                x.Items 
                |> ofType<XmlSchemaObject> 
                |> Seq.iter nav
            | _ -> ()
        closure x
        items.Contains x
        

    let xsdName (x: XmlQualifiedName) = 
        { Namespace = x.Namespace; Name = x.Name }

    let rec private getSchema (obj : XmlSchemaObject) = 
        match obj with
        | :? XmlSchema as res -> res
        | _ -> getSchema obj.Parent


    // base type (either global or anonymous) for a restriction.
    // None when the base type is primitive
    let private getBaseType (restr: XmlSchemaSimpleTypeRestriction) =
        if restr.BaseType <> null then Some restr.BaseType
        elif restr.BaseTypeName.Namespace = "http://www.w3.org/2001/XMLSchema"
        then None // restriction on a primitive type
        else // lookup global type definitions in the schema
            let item = (getSchema restr).SchemaTypes.[restr.BaseTypeName] 
            Some (item :?> XmlSchemaSimpleType)
        
    
    let private xsdFacets facets = {
        Length         = facets |> getFacet<XmlSchemaLengthFacet>    |> Option.map int
        MinLength      = facets |> getFacet<XmlSchemaMinLengthFacet> |> Option.map int
        MaxLength      = facets |> getFacet<XmlSchemaMaxLengthFacet> |> Option.map int
        MaxInclusive   = facets |> getFacet<XmlSchemaMaxInclusiveFacet>
        MaxExclusive   = facets |> getFacet<XmlSchemaMaxExclusiveFacet>
        MinInclusive   = facets |> getFacet<XmlSchemaMinInclusiveFacet>
        MinExclusive   = facets |> getFacet<XmlSchemaMinExclusiveFacet>
        TotalDigits    = facets |> getFacet<XmlSchemaTotalDigitsFacet>    |> Option.map int
        FractionDigits = facets |> getFacet<XmlSchemaFractionDigitsFacet> |> Option.map int
        Enumeration    = facets |> ofType<XmlSchemaEnumerationFacet> 
                                |> Seq.map (fun x -> x.Value) |> List.ofSeq
        Patterns = match facets |> ofType<XmlSchemaPatternFacet> 
                                |> Seq.map (fun x -> x.Value) |> List.ofSeq with
                                | [] -> []
                                | xs -> [xs]
        WhiteSpace = facets |> getFacet<XmlSchemaWhiteSpaceFacet> 
            |> Option.map (function                
                | "collapse" -> Collapse
                | "preserve" -> Preserve
                | "replace"  -> Replace
                | x -> failwithf "unknown whitespace option: %s" x)
        }


    // when deriving simple content by restriction,
    // the facets of the derived type (derivedFacets) must be combined
    // with those in the base type (baseFacets). We rely on the general
    // rule (enforced by the BCL parser) that a type derived by restriction
    // is more constrained than its base type. For example minLength
    // can only be increased. See also 
    // http://www.xfront.com/XML-Schema-library/papers/Algorithm-for-Merging-a-simpleType-Dependency-Chain.pdf
    let private combine baseFacets derivedFacets =
        let firstWithValue = List.tryPick id
        let len = firstWithValue [derivedFacets.Length; baseFacets.Length]

        let minInc, minExc = 
            match baseFacets.MinInclusive,    baseFacets.MinExclusive, 
               derivedFacets.MinInclusive, derivedFacets.MinExclusive with
            |  _, _, _, Some _ -> None, derivedFacets.MinExclusive
            | _, _, Some _, _  -> derivedFacets.MinInclusive, None
            |  _, Some _, _, _ -> None, baseFacets.MinExclusive
            | Some _, _, _, _  -> baseFacets.MinInclusive, None
            | _ -> None, None
        let maxInc, maxExc = 
            match baseFacets.MaxInclusive,    baseFacets.MaxExclusive, 
               derivedFacets.MaxInclusive, derivedFacets.MaxExclusive with
            |  _, _, _, Some _ -> None, derivedFacets.MaxExclusive
            | _, _, Some _, _  -> derivedFacets.MaxInclusive, None
            |  _, Some _, _, _ -> None, baseFacets.MaxExclusive
            | Some _, _, _, _  -> baseFacets.MaxInclusive, None
            | _ -> None, None
        {
            Length         = len
            MinLength      = firstWithValue [len; derivedFacets.MinLength; baseFacets.MinLength]
            MaxLength      = firstWithValue [len; derivedFacets.MaxLength; baseFacets.MaxLength]
            MaxInclusive   = maxInc
            MaxExclusive   = maxExc
            MinInclusive   = minInc
            MinExclusive   = minExc
            TotalDigits    = firstWithValue [derivedFacets.TotalDigits; baseFacets.TotalDigits]  
            FractionDigits = firstWithValue [derivedFacets.FractionDigits; baseFacets.FractionDigits] 
            Enumeration    = 
                match baseFacets.Enumeration, derivedFacets.Enumeration with
                | enums, [] | _ , enums -> enums 
            Patterns = baseFacets.Patterns @ derivedFacets.Patterns
            WhiteSpace = List.tryPick id [derivedFacets.WhiteSpace; baseFacets.WhiteSpace]
        }


    let rec private xsdSimpleType  =
        memoize <|
        fun (simpleType: XmlSchemaSimpleType) ->

        // we may have zero, one or even multiple dervivations by restriction.
        // we collects facets introduced by such restrictions  until we reach
        // a simple type whose content is not derived by restriction anymore.
        let rec collectFacets (simpleType: XmlSchemaSimpleType) =
            
            seq {
                match simpleType.Content with
                | :? XmlSchemaSimpleTypeRestriction as restr ->
                    yield simpleType,
                        restr.Facets 
                        |> ofType<XmlSchemaFacet>
                        |> xsdFacets 
                    match getBaseType restr with
                    | Some x -> yield! collectFacets x
                    | None -> () // reached a primitive type
                | _ -> yield simpleType, emptyFacets }

        let combineFacets simpleType =
            let types, facets = 
                collectFacets simpleType |> List.ofSeq |> List.unzip
            types |> Seq.last, facets |> List.reduce combine
            
        let xsdAtomicType (simpleType: XmlSchemaSimpleType) =
            // shadowing simpleType after collecting facets
            let simpleType, facets = combineFacets simpleType
            let atomicType = // use reflection because XsdAtomicType correspond to TypeCode
                match FSharpType.GetUnionCases(typeof<XsdAtomicType>)
                    |> Seq.tryFind (fun x -> x.Name = string simpleType.Datatype.TypeCode)
                    |> Option.map (fun x -> FSharpValue.MakeUnion(x, [||]) :?> XsdAtomicType)
                    with
                | None -> failwithf "unsupported type %A" simpleType.Datatype.TypeCode
                | Some x -> x
            atomicType, facets


        match simpleType.Datatype.Variety with
        | XmlSchemaDatatypeVariety.Atomic ->
            let atomicType, facets = xsdAtomicType simpleType 
            { //SimpleTypeName = None // TODO
              Facets = facets
              Variety = XsdAtom atomicType }
        | XmlSchemaDatatypeVariety.List ->
            let simpleType, facets = combineFacets simpleType
            match simpleType.Content with
            | :? XmlSchemaSimpleTypeList as xsdList ->
                let item = 
                    match xsdList.ItemType, xsdList.BaseItemType with
                    | null, null -> failwith "cannot find list item type"
                    | null, x | x, _ -> x
                { //SimpleTypeName = None // TODO
                  Facets = facets
                  Variety = XsdList (xsdSimpleType item) }
            | _ -> failwith "expected XmlSchemaSimpleTypeList"
        | XmlSchemaDatatypeVariety.Union ->
            let simpleType, facets = combineFacets simpleType
            match simpleType.Content with
            | :? XmlSchemaSimpleTypeUnion as xsdUnion ->
                let baseTypes = // xsdUnion.BaseMemberTypes may be null?
                    xsdUnion.BaseMemberTypes
                    |> Array.map xsdSimpleType
                    |> List.ofArray
                { //SimpleTypeName = None // TODO
                  Facets = facets
                  Variety = XsdUnion(baseTypes) }

            | _ -> failwith "expected XmlSchemaSimpleTypeUnion"
        | _ -> failwithf "unexpected variety %A" simpleType.Datatype.Variety

    let private xsdAttributeUse = 
        function
        | XmlSchemaUse.None
        | XmlSchemaUse.Optional -> Optional
        | XmlSchemaUse.Prohibited -> Prohibited
        | XmlSchemaUse.Required -> Required
        | x -> failwithf "unknown use: %A" x

    let private xsdAttribute (x: XmlSchemaAttribute) =
        { AttributeName = xsdName x.QualifiedName 
          Type = 
              if x.AttributeSchemaType = null 
              then // seems like it is null when Prohibited
                  assert (xsdAttributeUse x.Use = XsdAttributeUse.Prohibited)
                  // return a fake value that will be ignored
                  {  //SimpleTypeName = None
                     Facets = emptyFacets
                     Variety = XsdAtom XsdAtomicType.AnyAtomicType }
              else xsdSimpleType x.AttributeSchemaType
          FixedValue = if x.FixedValue = null then None else Some x.FixedValue }

    let rec xsdElement = 
        memoize <| 
        fun (elm: XmlSchemaElement) ->
//        if hasCycles elm 
//        then failwithf "Recursive schemas are not supported. \
//            Element '%A' has cycles." elm.QualifiedName
        { ElementName = xsdName elm.QualifiedName
          Type = xsdType elm.ElementSchemaType
          IsNillable = elm.IsNillable
          FixedValue = if elm.FixedValue = null then None else Some elm.FixedValue }

    and private xsdType = 
        memoize <| 
        function
        | :? XmlSchemaSimpleType  as simple  -> simple |> xsdSimpleType |> Simple
        | :? XmlSchemaComplexType as complex -> 

            let rec xsdParticle = 
                memoize <|
                fun (par: XmlSchemaParticle) ->
                
                let occurs = Min (int par.MinOccurs), 
                             if (par.MaxOccursString = "unbounded") 
                             then Unbounded
                             else Max (int par.MaxOccurs)

                let xsdParticles (grp: XmlSchemaGroupBase) =
                    let particles = 
                        grp.Items
                        |> ofType<XmlSchemaParticle> 
                        |> Seq.map xsdParticle
                    // particles is not materialized to avoid  
                    // loops in case of recursive schemas
                    match grp with
                    | :? XmlSchemaAll      -> All (occurs, particles)
                    | :? XmlSchemaChoice   -> Choice (occurs, particles)
                    | :? XmlSchemaSequence -> Sequence (occurs, particles)
                    | _ -> failwithf "unknown group base: %A" grp

                match par with
                | :? XmlSchemaAny as any -> 
                    let ns = 
                        match any.Namespace with
                        | null | "" | "##any" -> Wildcard.Any
                        | "##other" -> Wildcard.Other
                        | "##local" -> Wildcard.Local
                        | "##targetNamespace" -> Wildcard.Local // TODO check this
                        | x -> x.Split [|' '|] |> List.ofArray |> Wildcard.Target
                    Any (occurs, ns)
                | :? XmlSchemaGroupBase as grp -> xsdParticles grp
                | :? XmlSchemaGroupRef as grpRef -> xsdParticle grpRef.Particle
                | :? XmlSchemaElement as elm -> Element (occurs, xsdElement elm)
                | _ -> Empty // XmlSchemaParticle.EmptyParticle

            let simpleContent (complexType: XmlSchemaComplexType) = 
                let rec getSimpleType (xmlSchemaType : XmlSchemaType) = 
                    match xmlSchemaType.BaseXmlSchemaType with
                    | :? XmlSchemaSimpleType as result -> result
                    | x -> getSimpleType x.BaseXmlSchemaType

                let simpleType = complexType |> getSimpleType |> xsdSimpleType

                match complex.ContentModel.Content with
                | :? XmlSchemaSimpleContentRestriction as r -> 
                    let f' = r.Facets |> ofType<XmlSchemaFacet> |> xsdFacets
                    { simpleType with Facets = combine simpleType.Facets f' }
                // XmlSchemaSimpleContentExtension is ignored because its only
                // use is for adding attributes, but we already get them via the
                // AttributeUses collection
                | _ -> simpleType

            Complex { 
                ComplexTypeName = 
                    if complex.QualifiedName.IsEmpty then None 
                    else Some(xsdName complex.QualifiedName) 
                Attributes = 
                    complex.AttributeUses.Values
                    |> ofType<XmlSchemaAttribute>
                    |> Seq.map (fun x -> xsdAttribute x, xsdAttributeUse x.Use)
                    |> List.ofSeq
                Contents = 
                    match complex.ContentType with
                    | XmlSchemaContentType.TextOnly -> 
                        complex |> simpleContent |> SimpleContent
                    | XmlSchemaContentType.Mixed 
                    | XmlSchemaContentType.Empty 
                    | XmlSchemaContentType.ElementOnly -> 
                        complex.ContentTypeParticle
                        |> xsdParticle
                        |> ComplexContent
                    | _ -> failwith "unexpected content type: %A." complex.ContentType
                IsMixed = complex.IsMixed } 
        | xmlSchemaType -> failwithf "unknown type: %A" xmlSchemaType

    let xsdSchema (xsd : XmlSchemaSet) = 
        { Types = 
              xsd.GlobalTypes.Values
              |> ofType<XmlSchemaType>
              |> Seq.map (fun x -> xsdName x.QualifiedName, xsdType x)
              |> Map.ofSeq
          Elements = 
              xsd.GlobalElements.Values
              |> ofType<XmlSchemaElement>
              |> Seq.map xsdElement
              |> List.ofSeq
          Attributes = 
              xsd.GlobalAttributes.Values
              |> ofType<XmlSchemaAttribute>
              |> Seq.map xsdAttribute
              |> List.ofSeq }

    
    let createSchemaSet (xmlReader: XmlReader) =
        let schemaSet = new XmlSchemaSet()
        use reader = xmlReader
        XmlSchema.Read(reader, null) |> schemaSet.Add |> ignore
        schemaSet.Compile()
        schemaSet

    let xmlSchemaSet xsdText = 
        new XmlTextReader(new StringReader(xsdText))
        |> createSchemaSet

    let xmlSchemaSetFromUri schemaUri =
        let settings = XmlReaderSettings()
        settings.DtdProcessing <- DtdProcessing.Ignore
        XmlReader.Create(inputUri = schemaUri, settings = settings)
        |> createSchemaSet

    let fromText = xmlSchemaSet >> xsdSchema

    type ValidationResult =
        Success | Failure of XmlSchemaException
        with member x.Errors = match x with 
                               | Failure e -> Seq.singleton e 
                               | _ -> Seq.empty
            
    let validate xmlSchemaSet inputXml =
        let settings = XmlReaderSettings(ValidationType = ValidationType.Schema)
        settings.Schemas <- xmlSchemaSet
        //settings.IgnoreWhitespace <- true
        use reader = XmlReader.Create(new StringReader(inputXml), settings)
        try
            while reader.Read() do ()
            ValidationResult.Success
        with :? XmlSchemaException as e -> 
            ValidationResult.Failure e
            
    let validateElement xmlSchemaSet (element: System.Xml.Linq.XElement) =
        element.ToString() |> validate xmlSchemaSet 



   


    




        

