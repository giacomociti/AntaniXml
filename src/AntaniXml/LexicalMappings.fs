namespace AntaniXml

module LexicalMappings =
    open System.Xml
    open System.Text.RegularExpressions

    let replaceWhitespace x =
        Regex.Replace(x, pattern = "[\t\r\n]", replacement = " ")
    
    let collapseWhitespace x =
        let trimmed = (replaceWhitespace x).Trim()
        Regex.Replace(trimmed, pattern = " +", replacement = " ")

    let isNormalized x = replaceWhitespace x = x
    let isCollapsed x = collapseWhitespace x = x



    /// values may have multiple representations
    type LexMap<'a> = { parse: string -> 'a; format: 'a -> string list }

    // lexical mappings for built-in datatypes

    let XsdString = { parse = id; format = fun x -> [x] }

    let XsdBool = 
        { parse = XmlConvert.ToBoolean
          format = function 
            | true  -> ["true";  "1"] 
            | false -> ["false"; "0"] }
        
    let XsdDecimal =
        { parse = XmlConvert.ToDecimal
          format = fun x -> 
            let canonical = XmlConvert.ToString x
            if x >= 0M 
            then [ canonical
                   "00" + canonical
                   "+" + canonical ]
            else [ canonical ] }

    let XsdFloat = 
        { parse = XmlConvert.ToSingle 
          format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations (e.g. prefix +) 
            ] }

    let XsdDouble = 
        { parse = XmlConvert.ToDouble
          format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations (e.g. prefix +) 
            ] }

    let XsdDuration = 
        { parse = XmlConvert.ToTimeSpan
          format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations 
            ] }

    let XsdDateTime = 
        { parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          format = fun x -> 
            XmlDateTimeSerializationMode.GetValues(typeof<XmlDateTimeSerializationMode>) 
            |> Seq.cast<XmlDateTimeSerializationMode>
            |> Seq.map (fun serMode -> XmlConvert.ToString(x, serMode))
            |> List.ofSeq } // todo other representations?
        
    let XsdTime = 
        { parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          format = fun x -> 
            [ XmlConvert.ToString(x, "HH:mm:ss.fff")
            // todo other representations?
            ] } 

    let XsdDate = 
        { parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          format = fun x -> 
            [ XmlConvert.ToString(x, "yyyy-MM-dd")
            // todo other representations?
            ] } 

    let XsdGYearMonth = 
        { parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          format = fun x -> 
            [ XmlConvert.ToString(x.Date, "yyyy-MM")
            // todo other representations?
            ] } 

    let XsdGYear = 
        { parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          format = fun x -> 
            [ XmlConvert.ToString(x.Date, "yyyy")
            // todo other representations?
            ] } 

    let XsdGMonthDay = 
        { parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          format = fun x -> 
            [ XmlConvert.ToString(x.Date, "--MM-dd")
            // todo other representations?
            ] } 
            
    let XsdGDay = 
        { parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          format = fun x -> 
            [ XmlConvert.ToString(x.Date, "---dd")
            // todo other representations?
            ] } 
            
    let XsdGMonth = 
        { parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          format = fun x -> 
            [ XmlConvert.ToString(x.Date, "--MM")
            // todo other representations?
            ] } 

    // we use strings
    let XsdHexBinary = { parse = id; format = fun x -> [x] }


    // lexical mappings for primitive datatypes derived from built-in datatypes

    let XsdInt = 
        { parse = XmlConvert.ToInt32
          format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations?
            ] } 

    let XsdUInt = 
        { parse = XmlConvert.ToUInt32
          format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations?
            ] } 

    let XsdLong = 
        { parse = XmlConvert.ToInt64
          format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations?
            ] } 

    let XsdULong = 
        { parse = XmlConvert.ToUInt64
          format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations?
            ] } 

   

    // todo all the other datatypes
