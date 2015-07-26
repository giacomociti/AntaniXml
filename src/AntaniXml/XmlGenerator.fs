namespace AntaniXml

#nowarn "40"

module XmlGenerator =
    open System.Xml.Linq
    open System.Collections.Generic
    open FsCheck
    open XsdDomain
    open AtomicGenerators
    open ConstrainedGenerators
    open FacetBasedGenerators

    // naive, but neither thread safety nor memory footprint are a concern
    let memoize f =
        let cache = Dictionary<_, _>()
        fun x ->
            let ok, res = cache.TryGetValue x
            if ok then res
            else let res = f x
                 cache.[x] <- res
                 res

    let genAtom (xsdAtomicType, facets) =
        match xsdAtomicType with
        // primitive types:
        | AnyAtomicType -> genString facets
        | String -> genString facets
        | Boolean -> genBool facets
        | Decimal -> genDecimal facets
        | Float -> genFloat facets
        | Double -> genDouble facets
        | Duration -> genDuration facets
        | DateTime -> genDateTime facets
        | Time -> genTime facets
        | Date -> genDate facets
        | GYearMonth -> genGYearMonth facets
        | GYear -> genGYear facets
        | GMonthDay -> genGMonthDay facets
        | GDay -> genGDay facets
        | GMonth -> genGMonth facets
        | HexBinary -> genHexBinary facets
        | Base64Binary -> genBase64Binary facets
        | AnyUri -> genAnyURI facets
        | QName -> genQName facets
        // derived types:
        | NormalizedString -> genNormalizedString facets
        | Token -> genToken facets
        | Name -> genName facets
        | NmToken -> genNmToken facets
        | Language -> genLanguage facets
        | NCName -> genNcName facets
//        | Id
//        | Idref // what about Idrefs?
//        | Entity // what about Entities?
        | Integer -> genInteger facets
        | NonPositiveInteger -> genNonPositiveInteger facets
        | NegativeInteger -> genNegativeInteger facets     
        | Long -> genLong facets 
        | Int  -> genInt facets    
        | Short -> genShort facets 
        | Byte -> genByte facets
        | NonNegativeInteger -> genNonNegativeInteger facets
        | UnsignedLong -> genUnsignedLong facets 
        | UnsignedInt -> genUnsignedInt facets  
        | UnsignedShort -> genUnsignedShort facets
        | UnsignedByte -> genUnsignedByte facets
        | PositiveInteger -> genPositiveInteger facets    
    

    let rec genSimple = memoize (function
        | XsdAtom (t, facets) -> genAtom (t, facets)
        | XsdList (t, facets) -> 
            // List facets:  length, minLength, maxLength, pattern, and enumeration
            let length = facets.Length
            let minLen = facets.MinLength
            let maxLen = facets.MaxLength 
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
            } |> applyTextFacets facets WhitespaceHandling.Preserve
        | XsdUnion (ts, facets) -> // Union facets: pattern and enumeration
            {
                gen = ts |> List.map genSimple |> Gen.oneof
                description = "union"
                prop = fun x -> true
            } |> applyTextFacets facets WhitespaceHandling.Preserve )
        
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


    let rec genElement xsdElement =
        let getMax = function
        | Max n -> n
        | Unbounded -> 5 // avoid huge numbers

        let rec genParticle xsdParticle : Gen<seq<XElement>> =
            match xsdParticle with
            | Empty -> Gen.constant Seq.empty

            | Any ((Min min, maxOccurs), ns) -> 
                let elmName, elmNs =
                    match ns with
                    | AnyNs.Local -> "anyElement", "" 
                    | AnyNs.Other // hope we have no clashes
                    | AnyNs.Any -> "anyElement", "anyNs"
                    | AnyNs.Target (h :: _) -> "anyElement", h
                    | AnyNs.Target [] -> failwith "Empty Target"
                let elm = XElement(XName.Get(elmName, elmNs))
                gen { let! n = Gen.choose(min, getMax maxOccurs)
                      let! elms = Gen.constant elm |> Gen.listOfLength n 
                      return Seq.ofList elms }
                
            | Element ((Min min, maxOccurs), e) -> 
                gen { let! n = Gen.choose(min, getMax maxOccurs)
                      let! elms = genElement e |> Gen.listOfLength n 
                      return Seq.ofList elms }

            | Choice ((Min min, maxOccurs), items) -> 
                let choiceGen = items |> Seq.map genParticle |> Gen.oneof
                gen { let! n = Gen.choose(min, getMax maxOccurs)
                      let! elms = 
                        [1..n] 
                        |> List.map (fun _ -> choiceGen) 
                        |> Gen.sequence
                      return elms |> Seq.concat }

            | Sequence ((Min min, maxOccurs), items) -> 
                let seqGen = 
                    items 
                    |> List.ofSeq 
                    |> List.map genParticle 
                    |> Gen.sequence
                gen { 
                    let! n = Gen.choose(min, getMax maxOccurs)
                    let! elms = 
                        [1..n] 
                        |> List.map (fun _ -> seqGen) 
                        |> Gen.sequence
                    return elms |> List.concat |> Seq.concat }

            | All ((Min min, maxOccurs), items) -> 
                
//                let swap (a: _[]) x y =
//                    let tmp = a.[x]
//                    a.[x] <- a.[y]
//                    a.[y] <- tmp

                let allGen = 
                    items 
                    |> List.ofSeq  
                    |> List.map genParticle 
                    |> Gen.sequence
                gen { 
                    let! n = Gen.choose(min, getMax maxOccurs)
                    let! elms = // todo scramble a bit
                        [1..n] 
                        |> List.map (fun _ -> allGen) 
                        |> Gen.sequence
                    return elms |> List.concat |> Seq.concat }

        let genComplex complexType elmName =
            
            let genNodes =
                match complexType.Contents, complexType.IsMixed with
                | SimpleContent s, _ ->
                    assert(not complexType.IsMixed)
                    genSimple s |> Gen.map (fun x -> Seq.singleton(box x))
                | ComplexContent par, false ->
                    genParticle par |> Gen.map (Seq.map box)
                | ComplexContent par, true -> 
                    gen {
                        let! elms = genParticle par |> Gen.map (Seq.map box)
                        let! text = // to intersperse with child elements
                            genString XsdFactory.emptyFacets 
                            |> Gen.map box 
                            |> Gen.listOfLength (Seq.length elms)
                        let! lastText = genString XsdFactory.emptyFacets |> Gen.map box
                        return seq {
                            for t, e in Seq.zip text elms do
                                yield t
                                yield e
                            yield lastText }
                    }

            gen {   
                let! nodes = genNodes
                let! attrs = 
                    complexType.Attributes
                    |> List.map genAttribute
                    |> Gen.sequence
                let boxedAttrs = attrs |> List.choose id |> List.map box
                let items = Seq.append boxedAttrs nodes
                return XElement(mapName elmName, items) } 



        match xsdElement.Type with

        // http://www.w3.org/2001/XMLSchema-instance
        | Simple simpleType ->  
            match xsdElement.FixedValue with
            | Some fixedValue -> 
                 let elm = XElement(mapName xsdElement.ElementName)
                 elm.Value <- fixedValue
                 Gen.constant elm
            | None -> 
                let gen =
                    genSimple simpleType
                    |> Gen.map (fun x -> 
                        let e = XElement(mapName xsdElement.ElementName)
                        e.Value <- x
                        e)
                if xsdElement.IsNillable then
                    let xsi = "http://www.w3.org/2001/XMLSchema-instance"
                    let nilAttr = XAttribute(XName.Get("nil", xsi), true)
                    let nil = XElement(mapName xsdElement.ElementName, nilAttr)
                    Gen.frequency [8, gen; 2, Gen.constant nil ]
                else gen
                
        | Complex complexType -> 
            genComplex complexType xsdElement.ElementName 
                 
                 

   