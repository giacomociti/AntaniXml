namespace AntaniXml

/// This module provides types to represent a schema.
/// F# data types like discriminated unions allow for very clear definitions.
/// The W3C XML Schema specification http://www.w3.org/XML/Schema is rich and complex, 
/// so this module provides a simplified view.
/// Nevertheless, we cover a few specific (and sometimes tricky) concepts, like nillable values, 
/// fixed values, union and list for simple types, whitespace handling etc.

module XsdDomain =

    // We define our own type for xml names although the BCL already
    // defines 'System.Xml.Linq.XName' and 'System.Xml.XmlQualifiedName'.
    // The main conceptual reason is to avoid any kind of dependency on 
    // specific XML libraries. One technical reason is they don't support 
    // the 'System.IComparable' interface required for use as a key in F# Map. 
    // Both reasons are relatively weak (a dependency on a BCL library is not 
    // a big issue and both XName and XmlQualifiedName have structural equality 
    // hence may be used in hashtables) so in the future we may get rid of this type.

    /// Qualified name for XML elements and types.
    [<StructuralEquality; StructuralComparison>]
    type XsdName = { Namespace: string; Name: string } 

    type MinOccurs = Min of int
    type MaxOccurs = Max of int | Unbounded
    type Occurs = MinOccurs * MaxOccurs

    /// Enumeration of the built-in types defined by W3C XML Schema.
    /// A few ones are not supported: 
    /// Notation, NmTokens, Id, Idref, Idrefs, Entity and Entities.
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

    /// Whitespace handling options provide some formatting flexibility.
    /// see http://www.w3.org/TR/xmlschema11-2/#rf-whiteSpace
    type WhitespaceHandling = 
        /// No normalization is done, the value is not changed
        | Preserve
        /// All occurrences of #x9 (tab), #xA (line feed) and #xD (carriage return)
        /// are replaced with #x20 (space)
        | Replace 
        /// After the processing implied by replace, contiguous sequences of #x20's 
        /// are collapsed to a single #x20, and leading and trailing #x20's are removed.
        | Collapse 
    
    /// Wildcards provide extensibility.
    /// see http://www.w3.org/TR/xmlschema11-1/#Wildcards
    type Wildcard = 
        /// `##any` Any well-formed XML from any namespace (default)
        | Any 
        /// `##local` Any well-formed XML that is not qualified, 
        /// i.e. not declared to be in a namespace
        | Local 
        /// `##other` Any well-formed XML that is from a namespace 
        /// other than the target namespace of the type being defined 
        /// (unqualified elements are not allowed)
        | Other 
        /// `##targetNamespace` Any well-formed XML belonging to any
        /// namespace in the (whitespace separated) list; `##targetNamespace`
        /// is shorthand for the target namespace of the type being defined
        | Target of string list 

    /// Facets are constraints on values of simple types
    type Facets = 
        { Length    : int option
          MinLength : int option
          MaxLength : int option
          MaxInclusive : string option // the actual type depends on the context
          MaxExclusive : string option // the actual type depends on the context
          MinInclusive : string option // the actual type depends on the context
          MinExclusive : string option // the actual type depends on the context
          TotalDigits :    int option
          FractionDigits : int option
          Enumeration : string list
          /// modeled as a list of lists because, even though a type definition may
          /// specify only one list of patterns (indicating that valid values must
          /// match at least one of them), derived types may add more. And
          /// patterns added in derivations are meant to be in logical AND.
          /// For example if a base type defines patterns [p1;p2] and a derivation
          /// adds [p3;p4], valid values for the resulting [[p1;p2];[p3;p4]]
          /// must match (p1 OR p2) AND (p3 OR p4)
          Patterns : string list list
          WhiteSpace : WhitespaceHandling option }

    /// Most simple types are atomic, but there are also list and union types.
    /// To represent their nested structure we need recursive types.
    type XsdSimpleTypeVariety = 
        | XsdAtom  of XsdAtomicType  
        /// in a value of type list, items are separated by a single space.
        /// A list of union is allowed, list of list not.
        | XsdList  of XsdSimpleType 
        | XsdUnion of XsdSimpleType list 

    and XsdSimpleType = 
        { //SimpleTypeName: XsdName option // None if anonymous
          Facets: Facets
          Variety: XsdSimpleTypeVariety }
    
    type XsdAttribute = 
        { AttributeName: XsdName
          Type: XsdSimpleType
          FixedValue: string option }

    type XsdAttributeUse = Required | Optional | Prohibited

    [<ReferenceEquality; NoComparison>]
    type XsdElement = 
        { ElementName: XsdName
          Type: XsdType
          IsNillable: bool
          IsAbstract: bool
          IsRecursive: bool
          SubstitutionGroup: XsdElement seq
          FixedValue: string option }

    and XsdType = 
        | Simple  of XsdSimpleType
        | Complex of XsdComplexType
        

    and XsdComplexType = 
        { ComplexTypeName: XsdName option // None if anonymous
          Attributes: (XsdAttribute * XsdAttributeUse) list
          Contents: XsdContent 
          IsMixed: bool }

    and XsdContent =
        | SimpleContent  of XsdSimpleType
        | ComplexContent of XsdParticle

    and XsdParticle = 
        | Empty
        | Any      of Occurs * Wildcard
        | Element  of Occurs * XsdElement
        | All      of Occurs * seq<XsdParticle> // lazyness of seq allows circular definitions
        | Choice   of Occurs * seq<XsdParticle> // lazyness of seq allows circular definitions
        | Sequence of Occurs * seq<XsdParticle> // lazyness of seq allows circular definitions
