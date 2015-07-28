namespace AntaniXml

module AtomicGenerators =
    open System
    open System.Globalization
    open FsCheck
    open XsdDomain
    open LexicalMappings
    open ConstrainedGenerators
    open FacetBasedGenerators

    // primitive datatypes

    // http://www.w3.org/TR/xmlschema-2/#string
    let genString facets =
        boundedStringGen facets 0 100
        |> applyTextFacets facets Preserve

    let genNormalizedString facets =
        boundedStringGen facets 0 100
        |> applyTextFacets facets Replace

    let genToken facets =
        boundedStringGen facets 0 100
        |> applyTextFacets' facets Collapse


    let genName facets =
        let pattern = "[a-zA-Z_:]([a-zA-Z0-9_:.])*"
        genToken { facets with Patterns = [pattern] :: facets.Patterns }

    let genNcName facets = // name without colon
        let pattern = "[a-zA-Z_]([a-zA-Z0-9_.])*"
        genToken { facets with Patterns = [pattern] :: facets.Patterns }

    let genNmToken facets =
        let pattern = "([a-zA-Z0-9_:.])+"
        genToken { facets with Patterns = [pattern] :: facets.Patterns }

    

    let genLanguage facets =
        let languages = 
            CultureInfo
                .GetCultures(CultureTypes.NeutralCultures ||| 
                             CultureTypes.SpecificCultures)
                |> Array.filter ((<>) CultureInfo.InvariantCulture)
                |> Array.map (fun x -> x.Name)
        { gen = Gen.elements languages
          description = "languages"
          prop = fun _ -> true }
        |> applyTextFacets facets Collapse

    // http://www.w3.org/TR/xmlschema-2/#boolean
    let genBool facets = 
        { gen = Gen.elements [true; false]
          description = "booleans"
          prop = fun _ -> true }
        |> lexMap XsdBool
        |> applyTextFacets facets Collapse

    // http://www.w3.org/TR/xmlschema-2/#decimal
    let genDecimal facets = 
        // todo totalDigits fractionDigits
        boundedGen (Arb.Default.Decimal().Generator) XsdDecimal facets
            Decimal.MinValue Decimal.MaxValue 0.01M
        |> applyTextFacets facets Collapse

    //http://www.w3.org/TR/xmlschema-2/#float
    let genFloat facets =
        boundedGen (Arb.Default.Float32().Generator) XsdFloat facets 
            Single.MinValue Single.MaxValue 0.01f
        |> applyTextFacets facets Collapse
          
    // http://www.w3.org/TR/xmlschema-2/#double
    let genDouble facets =
        boundedGen (Arb.Default.Float().Generator) XsdDouble facets 
            Double.MinValue Double.MaxValue 0.01
        |> applyTextFacets facets Collapse

    // http://www.w3.org/TR/xmlschema-2/#duration
    // System.TimeSpan is ok but lacks Year, Month and Day components
    // The advantage is that it is totally ordered (we cannot compare a month with 30 days)
    // but does not cover the full xsd specs. 
    // TDB investigate if full specs for duration are supported in common tools (serialization, codegen...)
    let genDuration facets =
        boundedGen (Arb.Default.TimeSpan().Generator) XsdDuration facets 
            TimeSpan.MinValue TimeSpan.MaxValue (TimeSpan(0, 0, 1))
        |> applyTextFacets facets Collapse

    // http://www.w3.org/TR/xmlschema-2/#dateTime
    let genDateTime facets =
        boundedGen (Arb.Default.DateTime().Generator) XsdDateTime facets 
            DateTime.MinValue DateTime.MaxValue (TimeSpan(0, 0, 1))
        |> applyTextFacets facets Collapse

    // http://www.w3.org/TR/xmlschema-2/#time
    let genTime facets =
        boundedGen (Arb.Default.DateTime().Generator) XsdTime facets 
            DateTime.MinValue DateTime.MaxValue (TimeSpan(0, 0, 1))
        |> applyTextFacets facets Collapse
          
    // http://www.w3.org/TR/xmlschema-2/#date
    let genDate facets = 
        boundedGen (Arb.Default.DateTime().Generator) XsdDate facets 
            DateTime.MinValue DateTime.MaxValue (TimeSpan(1, 0, 0, 0))
        |> applyTextFacets facets Collapse
           
    // http://www.w3.org/TR/xmlschema-2/#gYearMonth
    let genGYearMonth facets = //CCYY-MM
        let epsilon = TimeSpan(32, 0, 0, 0) // 32 days is enough to change month
        boundedGen (Arb.Default.DateTime().Generator) XsdGYearMonth facets 
            DateTime.MinValue DateTime.MaxValue epsilon
        |> applyTextFacets facets Collapse
           
    // http://www.w3.org/TR/xmlschema-2/#gYear
    let genGYear facets = 
        let epsilon = TimeSpan(367, 0, 0, 0) // 367 days is enough to change year
        boundedGen (Arb.Default.DateTime().Generator) XsdGYear facets 
            DateTime.MinValue DateTime.MaxValue epsilon
        |> applyTextFacets facets Collapse
       
    // http://www.w3.org/TR/xmlschema-2/#gMonthDay
    let genGMonthDay facets = 
        let epsilon = TimeSpan(1, 0, 0, 0) // 1 day
        boundedGen (Arb.Default.DateTime().Generator) XsdGMonthDay facets 
            DateTime.MinValue DateTime.MaxValue epsilon
        |> applyTextFacets facets Collapse
       
    // http://www.w3.org/TR/xmlschema-2/#gDay
    let genGDay facets = 
        let epsilon = TimeSpan(1, 0, 0, 0) // 1 day
        boundedGen (Arb.Default.DateTime().Generator) XsdGDay facets 
            DateTime.MinValue DateTime.MaxValue epsilon
        |> applyTextFacets facets Collapse
       
    // http://www.w3.org/TR/xmlschema-2/#gMonth
    let genGMonth facets = 
        let epsilon = TimeSpan(32, 0, 0, 0) // 32 days is enough to change month
        boundedGen (Arb.Default.DateTime().Generator) XsdGMonth facets 
            DateTime.MinValue DateTime.MaxValue epsilon
        |> applyTextFacets facets Collapse


    // http://www.w3.org/TR/xmlschema-2/#hexBinary
    let genHexBinary facets =
        let min, max = lengthBounds facets 0 20
        let pattern = sprintf "([0-9a-fA-F]{2}){%i,%i}" min max
        { gen = genPattern [pattern]
          description = "hexBinary"
          prop = hasPattern [pattern] }
        |> applyTextFacets facets Collapse
           

           

    // http://www.w3.org/TR/xmlschema-2/#base64Binary
    // the above specs are quite complex, better postpone a proper implementation
    let genBase64Binary facets = 
// todo facets (notice that length here is not the string length)
//length
//minLength
//maxLength
//pattern
//enumeration
//whiteSpace
        Arb.Default.NonEmptyString().Generator
        |> Gen.map (fun x -> x.Get)
        |> Gen.map (Text.Encoding.Unicode.GetBytes >> Convert.ToBase64String)


    // http://www.w3.org/TR/xmlschema-2/#anyURI
    let genAnyURI facets = 
        //todo better pattern
        let pattern = "[a-zA-Z]+"
        { gen = genPattern [pattern]
          description = "anyURI"
          prop = hasPattern [pattern] }
        |> applyTextFacets facets Collapse
            

    // http://www.w3.org/TR/xmlschema-2/#QName
    // QName represents XML qualified names. The ·value space· of QName is the set of tuples {namespace name, local part}, 
    // where namespace name is an anyURI and local part is an NCName. The ·lexical space· of QName is the set of strings 
    // that ·match· the QName production of [Namespaces in XML].
    let genQName facets = 
        // we avoid generating the prefix because it should match a real prefix declaration
        let pattern = "[a-zA-Z]([a-zA-Z0-9_.])*"
        { gen = genPattern [pattern]
          description = "QName"
          prop = hasPattern [pattern] }
        |> applyTextFacets facets Collapse


    
    // build it datatypes (derived from primitive datatypes)

    // we may use BigInteger but we keep it simple with Int64
    let genInteger facets =
        // todo digits facets
        boundedGen (Arb.Default.Int64().Generator) XsdLong facets 
            Int64.MinValue Int64.MaxValue 1L
        |> applyTextFacets facets Collapse

    // we may use BigInteger but we keep it simple with Int64
    let genNonPositiveInteger facets =
        // todo digits facets
        boundedGen (Arb.Default.Int64().Generator) XsdLong facets 
            Int64.MinValue 0L 1L
        |> applyTextFacets facets Collapse

    // we may use BigInteger but we keep it simple with Int64
    let genNegativeInteger facets =
        // todo digits facets
        boundedGen (Arb.Default.Int64().Generator) XsdLong facets 
            Int64.MinValue -1L 1L
        |> applyTextFacets facets Collapse

    let genLong facets =
        // todo digits facets
        boundedGen (Arb.Default.Int64().Generator) XsdLong facets 
            Int64.MinValue Int64.MaxValue 1L
        |> applyTextFacets facets Collapse

    let genInt facets =
        // todo digits facets
        boundedGen (Arb.Default.Int32().Generator) XsdInt facets 
            Int32.MinValue Int32.MaxValue 1
        |> applyTextFacets facets Collapse

    let genShort facets =
        // todo digits facets
        boundedGen (Arb.Default.Int32().Generator) XsdInt facets 
            (int Int16.MinValue) (int Int16.MaxValue) 1
        |> applyTextFacets facets Collapse

    let genByte facets =
        // todo digits facets
        boundedGen (Arb.Default.Int32().Generator) XsdInt facets 
            (int Byte.MinValue) (int Byte.MaxValue) 1
        |> applyTextFacets facets Collapse

    // we may use BigInteger but we keep it simple with Int64
    let genNonNegativeInteger facets =
        // todo digits facets
        boundedGen (Arb.Default.Int64().Generator) XsdLong facets 
            0L Int64.MaxValue 1L
        |> applyTextFacets facets Collapse

    let genUnsignedLong facets =
        // todo digits facets
        boundedGen (Arb.Default.UInt64().Generator) XsdULong facets 
            UInt64.MinValue UInt64.MaxValue 1UL
        |> applyTextFacets facets Collapse

    let genUnsignedInt facets =
        // todo digits facets
        boundedGen (Arb.Default.UInt32().Generator) XsdUInt facets 
            UInt32.MinValue UInt32.MaxValue 1u
        |> applyTextFacets facets Collapse

    let genUnsignedShort facets =
        // todo digits facets
        boundedGen (Arb.Default.UInt32().Generator) XsdUInt facets 
            (uint32 UInt16.MinValue) (uint32 UInt16.MaxValue) 1u
        |> applyTextFacets facets Collapse

    let genUnsignedByte facets =
        // todo digits facets
        boundedGen (Arb.Default.UInt32().Generator) XsdUInt facets 
            (uint32 Byte.MinValue) (uint32 Byte.MaxValue) 1u
        |> applyTextFacets facets Collapse

    // we may use BigInteger but we keep it simple with Int64
    let genPositiveInteger facets =
        // todo digits facets
        boundedGen (Arb.Default.Int64().Generator) XsdLong facets 
            1L Int64.MaxValue 1L
        |> applyTextFacets facets Collapse


