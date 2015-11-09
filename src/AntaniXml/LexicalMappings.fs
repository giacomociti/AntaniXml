namespace AntaniXml

/// This module contains whitespace handling and also parse and format functions for simple datatypes.
/// In W3C terms, parse is a map from the lexical space to the value space of a given datatype.
/// Usually we implement it relying on `System.XmlConvert`.
/// The format function instead maps values to lexical representations. 
/// It returns a string list because the same value may have multiple representations 
/// (e.g. a plus sign may optionally prefix a number).
/// Note: this module is not complete yet, furthermore its design may change in future versions.
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
    type LexMap<'a> = { Parse: string -> 'a; Format: 'a -> string list }

    // lexical mappings for built-in datatypes

    let XsdString = { Parse = id; Format = fun x -> [x] }

    let XsdBool = 
        { Parse = XmlConvert.ToBoolean
          Format = function 
            | true  -> ["true";  "1"] 
            | false -> ["false"; "0"] }
        
    let XsdDecimal =
        { Parse = XmlConvert.ToDecimal
          Format = fun x -> 
            let canonical = XmlConvert.ToString x
            if x >= 0M 
            then [ canonical
                   "00" + canonical
                   "+" + canonical ]
            else [ canonical ] }

    let XsdFloat = 
        { Parse = XmlConvert.ToSingle 
          Format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations (e.g. prefix +) 
            ] }

    let XsdDouble = 
        { Parse = XmlConvert.ToDouble
          Format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations (e.g. prefix +) 
            ] }

    let XsdDuration = 
        { Parse = XmlConvert.ToTimeSpan
          Format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations 
            ] }

    let XsdDateTime = 
        { Parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          Format = fun x -> 
            XmlDateTimeSerializationMode.GetValues(typeof<XmlDateTimeSerializationMode>) 
            |> Seq.cast<XmlDateTimeSerializationMode>
            |> Seq.map (fun serMode -> XmlConvert.ToString(x, serMode))
            |> List.ofSeq } // todo other representations?
        
    let XsdTime = 
        { Parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          Format = fun x -> 
            [ XmlConvert.ToString(x, "HH:mm:ss.fff")
            // todo other representations?
            ] } 

    let XsdDate = 
        { Parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          Format = fun x -> 
            [ XmlConvert.ToString(x, "yyyy-MM-dd")
            // todo other representations?
            ] } 

    let XsdGYearMonth = 
        { Parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          Format = fun x -> 
            [ XmlConvert.ToString(x.Date, "yyyy-MM")
            // todo other representations?
            ] } 

    let XsdGYear = 
        { Parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          Format = fun x -> 
            [ XmlConvert.ToString(x.Date, "yyyy")
            // todo other representations?
            ] } 

    let XsdGMonthDay = 
        { Parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          Format = fun x -> 
            [ XmlConvert.ToString(x.Date, "--MM-dd")
            // todo other representations?
            ] } 
            
    let XsdGDay = 
        { Parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          Format = fun x -> 
            [ XmlConvert.ToString(x.Date, "---dd")
            // todo other representations?
            ] } 
            
    let XsdGMonth = 
        { Parse = fun x ->
            XmlConvert.ToDateTime(x, XmlDateTimeSerializationMode.RoundtripKind)
          Format = fun x -> 
            [ XmlConvert.ToString(x.Date, "--MM")
            // todo other representations?
            ] } 

    // we use strings
    let XsdHexBinary = { Parse = id; Format = fun x -> [x] }


    // lexical mappings for primitive datatypes derived from built-in datatypes

    let XsdInt = 
        { Parse = XmlConvert.ToInt32
          Format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations?
            ] } 

    let XsdUInt = 
        { Parse = XmlConvert.ToUInt32
          Format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations?
            ] } 

    let XsdLong = 
        { Parse = XmlConvert.ToInt64
          Format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations?
            ] } 

    let XsdULong = 
        { Parse = XmlConvert.ToUInt64
          Format = fun x -> 
            [ XmlConvert.ToString x
            // todo other representations?
            ] } 

   

    // todo all the other datatypes
