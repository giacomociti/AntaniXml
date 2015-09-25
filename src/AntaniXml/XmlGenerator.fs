namespace AntaniXml

#nowarn "40"

/// This is where elements and types (simple and complex) defined in a schema 
/// are mapped to random generators. Complex generators are composed using FsCheck combinators.
module XmlGenerator =
    open System.Xml.Linq
    open FsCheck
    open XsdDomain
    open AtomicGenerators
    open ConstrainedGenerators
    open FacetBasedGenerators
    open XsdFactory

    let genAtom = function 
        // primitive types:
        | AnyAtomicType -> genString
        | String -> genString
        | Boolean -> genBool
        | Decimal -> genDecimal
        | Float -> genFloat
        | Double -> genDouble
        | Duration -> genDuration
        | DateTime -> genDateTime
        | Time -> genTime
        | Date -> genDate
        | GYearMonth -> genGYearMonth
        | GYear -> genGYear
        | GMonthDay -> genGMonthDay
        | GDay -> genGDay
        | GMonth -> genGMonth
        | HexBinary -> genHexBinary
        | Base64Binary -> genBase64Binary
        | AnyUri -> genAnyURI
        | QName -> genQName
        // derived types:
        | NormalizedString -> genNormalizedString
        | Token -> genToken
        | Name -> genName
        | NmToken -> genNmToken
        | Language -> genLanguage
        | NCName -> genNcName
//        | Id
//        | Idref // what about Idrefs?
//        | Entity // what about Entities?
        | Integer -> genInteger
        | NonPositiveInteger -> genNonPositiveInteger
        | NegativeInteger -> genNegativeInteger     
        | Long -> genLong 
        | Int  -> genInt    
        | Short -> genShort 
        | Byte -> genByte
        | NonNegativeInteger -> genNonNegativeInteger
        | UnsignedLong -> genUnsignedLong 
        | UnsignedInt -> genUnsignedInt  
        | UnsignedShort -> genUnsignedShort
        | UnsignedByte -> genUnsignedByte
        | PositiveInteger -> genPositiveInteger    
    
    let rec genSimple = 
        memoize <| fun (simpleType: XsdSimpleType) ->
        match simpleType.Variety with
        | XsdAtom t -> (genAtom t) simpleType.Facets
        | XsdList t -> 
            // List facets:  length, minLength, maxLength, pattern, and enumeration
            let length = simpleType.Facets.Length
            let minLen = simpleType.Facets.MinLength
            let maxLen = simpleType.Facets.MaxLength 
            let min =
                match length, minLen with
                | Some x, _ | None, Some x -> x
                | None, None -> 0
            let max =
                match length, maxLen with
                | Some x, _ | None, Some x -> x
                | None, None -> min + 5 // we should use size instead?
            {
                gen = gen {
                    let! n = Gen.choose(min, max)
                    return! genSimple t 
                            |> Gen.listOfLength n 
                            |> Gen.map (String.concat " ") } 
                description = "list"
                prop = fun x -> 
                    let actualLength = (x.Split [|' '|]).Length
                    minLen |> Option.forall (fun l -> actualLength >= l) &&
                    maxLen |> Option.forall (fun l -> actualLength <= l) &&
                    length |> Option.forall ((=) actualLength) 
            } |> applyTextFacets simpleType.Facets WhitespaceHandling.Preserve
        | XsdUnion ts -> // Union facets: pattern and enumeration
            {
                gen = ts |> List.map genSimple |> Gen.oneof
                description = "union"
                prop = fun _ -> true
            } |> applyTextFacets simpleType.Facets WhitespaceHandling.Preserve 
        
    let mapName (xsdName: XsdName) = XName.Get(xsdName.Name, xsdName.Namespace)

    let genAttribute (xsdAttribute, xsdUse) = 
        let name = mapName xsdAttribute.AttributeName
        let attrGen = 
            match xsdAttribute.FixedValue with
            | Some fixedValue -> Gen.constant fixedValue
            | None -> genSimple xsdAttribute.Type
            |> Gen.map (fun x -> XAttribute(name, box x) |> Some)
        match xsdUse with
        | Required -> attrGen
        | Optional -> Gen.oneof [ attrGen; Gen.constant None ]
        | _ -> Gen.constant None

    let decreaseSize = float >> log >> ceil >> int


    let genElementCustom (customGenerators: Customization.Maps) xsdElement = 

        let chooseOccurs (Min x, maxOccurs) size = 
            match maxOccurs with
            | Max y when y < size -> x, y
            // choose upper based on size (but ensure min <= max)
            | _ -> x, max x size 
            |> Gen.choose

        let rec genElement' = 
            memoize <|
            fun xsdElement size ->

            let genSimpleElement (simpleType: XsdSimpleType) = 
                
                let elementName = mapName xsdElement.ElementName

                match xsdElement.FixedValue with
                | Some fixedValue -> 
                        let elm = XElement(elementName)
                        elm.Value <- fixedValue
                        Gen.constant elm
                | None -> 
                    let gen =
                        genSimple simpleType
                        |> Gen.map (fun x -> 
                            let elm = XElement(elementName)
                            elm.Value <- x
                            elm)
                    if xsdElement.IsNillable then
                        let xsi = "http://www.w3.org/2001/XMLSchema-instance"
                        let nilAttr = XAttribute(XName.Get("nil", xsi), true)
                        let nil = XElement(elementName, nilAttr)
                        Gen.frequency [8, gen; 2, Gen.constant nil]
                    else gen

            let gen() = // generator for this element definition
                match xsdElement.Type with
                | Simple simpleType -> genSimpleElement simpleType
                | Complex complexType -> genComplex complexType xsdElement.ElementName size
            
            let genSubst() = // generators for substitutes
                xsdElement.SubstitutionGroup
                |> Seq.filter (fun x -> size > 0 || not x.IsRecursive)
                |> Seq.map (fun x -> genElement' x (if x.IsRecursive then size/2 else size))
                |> Seq.toList

            let genWithSubst() = // generator for this element including substitutes
                match xsdElement.IsAbstract, genSubst() with
                | true, [] -> 
                    gen() // invalid but there's no alternative
                | true,  x  -> 
                    Gen.oneof x
                | false, [] -> gen()
                | false, x  -> Gen.oneof <| gen() :: x

            match customGenerators.elementGens.TryFind xsdElement.ElementName with
            | Some g -> g
            | None -> 
                match customGenerators.elementMaps.TryFind xsdElement.ElementName with
                | Some mapping -> genWithSubst() |> Gen.map mapping 
                | None -> genWithSubst()


        and genComplex complexType elmName size = 
            
            match complexType.ComplexTypeName with
            | Some x when customGenerators.complexGens.ContainsKey x -> 
                customGenerators.complexGens.Item x
                |> Gen.map (fun x -> x.Name <- mapName elmName; x)
            | _ ->
          
            let genNodes = 
                match complexType.Contents, complexType.IsMixed with
                | SimpleContent s, _ ->
                    assert(not complexType.IsMixed)
                    genSimple s |> Gen.map (fun x -> Seq.singleton(box x))
                | ComplexContent par, false ->
                    genParticle par size |> Gen.map (Seq.map box)
                | ComplexContent par, true -> 
                    gen {
                        let! elms = genParticle par size |> Gen.map (Seq.map box)
                        let! text = // to intersperse with child elements
                            genString XsdFactory.emptyFacets 
                            |> Gen.map box 
                            |> Gen.listOfLength (Seq.length elms)
                        let! lastText = genString XsdFactory.emptyFacets |> Gen.map box
                        return seq {
                            for t, e in Seq.zip text elms do
                                yield t
                                yield e
                            yield lastText } }
            let gen = 
                gen {   
                    let! nodes = genNodes
                    let! attrs = 
                        complexType.Attributes
                        |> List.map genAttribute
                        |> Gen.sequence
                    let boxedAttrs = attrs |> List.choose id |> List.map box
                    let items = Seq.append boxedAttrs nodes
                    return XElement(mapName elmName, items) } 

            match complexType.ComplexTypeName with
            | Some x when customGenerators.complexMaps.ContainsKey x -> 
                gen |> Gen.map(customGenerators.complexMaps.Item x)
            | _ -> gen

        and genParticle xsdParticle size : Gen<seq<XElement>> = 
            let size' = decreaseSize size
            match xsdParticle with
            | Empty -> Gen.constant Seq.empty

            | Any (occurs, ns) -> 
                let elmName, elmNs =
                    match ns with
                    | Wildcard.Local -> "anyElement", "" 
                    | Wildcard.Other // hope we have no clashes
                    | Wildcard.Any -> "anyElement", "anyNs"
                    | Wildcard.Target (h :: _) -> "anyElement", h
                    | Wildcard.Target [] -> failwith "Empty Target"
                let elm = XElement(XName.Get(elmName, elmNs))
                gen { 
                    let! n = chooseOccurs occurs size'
                    let! elms = Gen.constant elm |> Gen.listOfLength n 
                    return Seq.ofList elms }
                
            | Element (occurs, e) -> 
                gen { 
                    let! n = chooseOccurs occurs size'
                    // it's important to decrease the size in the 
                    // recursive call especially for recursive schemas
                    let! elms = genElement' e size' |> Gen.listOfLength n 
                    return Seq.ofList elms }

            | Choice (occurs, items) -> 
                gen { 
                    let! n = chooseOccurs occurs size'
                    if n = 0 then return Seq.empty else 
                    let! elms =
                        items 
                        |> Seq.map (fun x -> genParticle x size') 
                        |> Gen.oneof
                        |> Gen.listOfLength n
                    return elms |> Seq.concat }

            | Sequence (occurs, items) -> 
                gen { 
                    let! n = chooseOccurs occurs size'
                    if n = 0 then return Seq.empty else
                    let! elms = 
                        items 
                        |> Seq.map (fun x -> genParticle x size')
                        |> Gen.sequence
                        |> Gen.listOfLength n
                    return elms |> Seq.concat |> Seq.concat }

            | All (occurs, items) -> 
                gen { 
                    let! n = chooseOccurs occurs size'
                    if n = 0 then return Seq.empty else
                    let! elms =
                        items 
                        |> Seq.map (fun x -> genParticle x size')
                        |> Gen.sequence
                        |> Gen.listOfLength n
                    //TODO scramble a bit?
                    return elms |> Seq.concat |> Seq.concat }

        Gen.sized (genElement' xsdElement)
                 
    let genElement xsdElement = genElementCustom Customization.Maps.empty xsdElement
        

        
   