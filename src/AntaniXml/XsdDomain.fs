namespace AntaniXml

module XsdDomain =

    type XsdName = { Namespace: string; Name: string } 

    type MinOccurs = Min of int
    type MaxOccurs = Max of int | Unbounded
    type Occurs = MinOccurs * MaxOccurs

    type XsdAtomicType = 
        | AnyAtomicType // anySimpleType?
        // primitive types:
        | String | Decimal | Duration | DateTime | Time | Date 
        | GYearMonth | GYear | GMonthDay | GDay | GMonth 
        | Boolean | HexBinary | Base64Binary | Float | Double 
        | AnyUri | QName // | Notation 

        // types derived from String:
        | NormalizedString // derived from String
        | Token  // derived from NormalizedString
        | Name | NmToken | Language // derived from Token
        | NCName // derived from Name

        // Id and Idref are for uniqueness and subset constraints
        //| Id | Idref are derived from NCName

        // Entity is derived from NCName and must match an unparsed entity defined in a DTD
        // We don't support it because it is rarely used and also source of security issues
        //| Entity 

        // NmTokens is derived by list from NmToken, 
        // Idrefs and Entities are derived by list from Idref and Entity respectively
        // Not sure if they are even supported in the .Net BCL

        // types derived from Decimal
        | Integer // derived from Decimal
        | NonPositiveInteger | Long | NegativeInteger // derived from Integer
        | NonNegativeInteger // derived from NonPositiveInteger
        | Int   // derived from Long
        | Short // derived from Int
        | Byte  // derived from Short
        | UnsignedLong | PositiveInteger // derived from NonNegativeInteger
        | UnsignedInt   // derived from UnsignedLong
        | UnsignedShort // derived from UnsignedInt
        | UnsignedByte  // derived from UnsignedShort

    /// see http://www.w3.org/TR/xmlschema-2/#rf-whiteSpace
    type WhitespaceHandling = 
        /// No normalization is done, the value is not changed
        | Preserve
        /// All occurrences of #x9 (tab), #xA (line feed) and #xD (carriage return)
        /// are replaced with #x20 (space)
        | Replace 
        /// After the processing implied by replace, contiguous sequences of #x20's 
        /// are collapsed to a single #x20, and leading and trailing #x20's are removed.
        | Collapse 
    

        type AnyNs =
        /// ##any Any well-formed XML from any namespace (default)
        | Any 
        /// ##local Any well-formed XML that is not qualified, 
        /// i.e. not declared to be in a namespace
        | Local 
        /// ##other Any well-formed XML that is from a namespace other than the target
        /// namespace of the type being defined (unqualified elements are not allowed)
        | Other 
        /// "http://www.w3.org/1999/xhtml ##targetNamespace" Any well-formed XML belonging 
        /// to any namespace in the (whitespace separated) list; ##targetNamespace is 
        /// shorthand for the target namespace of the type being defined
        | Target of string list 

    type Facets = {
        Length: int option;
        MinLength: int option;
        MaxLength: int option;
        MaxInclusive: string option; // the actual type depends on the context
        MaxExclusive: string option; // the actual type depends on the context
        MinInclusive: string option; // the actual type depends on the context
        MinExclusive: string option; // the actual type depends on the context
        TotalDigits: int option;
        FractionDigits: int option;
        Enumeration: string list;
        Patterns: string list list; 
        WhiteSpace: WhitespaceHandling option }
    
    // list of union is allowed, list of list not
    type XsdSimpleType = 
        | XsdAtom  of XsdAtomicType * Facets 
        | XsdList  of XsdSimpleType * Facets
        | XsdUnion of XsdSimpleType list * Facets
        
    type XsdAttribute = 
        { AttributeName: XsdName
          Type: XsdSimpleType
          FixedValue: string option }

    type XsdAttributeUse = Required | Optional | Prohibited

    type XsdElement = 
        { ElementName: XsdName
          Type: XsdType
          IsNillable: bool
          FixedValue: string option }

    and XsdType = 
        | Simple  of XsdSimpleType
        | Complex of XsdComplexType

    and XsdComplexType = 
        { Attributes: (XsdAttribute * XsdAttributeUse) list
          Contents: XsdContent 
          IsMixed: bool }

    and XsdContent =
        | SimpleContent  of XsdSimpleType
        | ComplexContent of XsdParticle

    and XsdParticle = 
        | Empty
        | Any      of Occurs * AnyNs
        | Element  of Occurs * XsdElement
        | All      of Occurs * seq<XsdParticle> // lazyness of seq allows circular definitions
        | Choice   of Occurs * seq<XsdParticle> // lazyness of seq allows circular definitions
        | Sequence of Occurs * seq<XsdParticle> // lazyness of seq allows circular definitions

    type XsdSchema = 
        { Types: Map<XsdName, XsdType>
          Elements: XsdElement list
          Attributes: XsdAttribute list }


