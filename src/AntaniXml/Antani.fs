namespace AntaniXml


open System.Xml
open System.Xml.Linq
open System.Xml.Schema
open FsCheck
open System.Collections.Generic

type map<'a> = 'a -> 'a

type internal inner = 
    { // the following maps may be obtained by customizations
      basicTypes : Dictionary<XsdDomain.XsdAtomicType, Arbitrary<string>>
      globalSimpleTypes : Dictionary<XsdDomain.XsdName, Arbitrary<string>>
      globalComplexTypes : Dictionary<XsdDomain.XsdName, Arbitrary<XElement>>
      globalElements : Dictionary<XsdDomain.XsdName, Arbitrary<XElement>>
      // the following maps are for memoization
      simpleTypes : Dictionary<XmlSchemaSimpleType, Arbitrary<string>>
      complexTypes : Dictionary<XmlSchemaComplexType, Arbitrary<XElement>>
      elements : Dictionary<XmlSchemaElement, Arbitrary<XElement>> }
    static member empty = 
        { basicTypes = Dictionary() // todo fill
          globalSimpleTypes = Dictionary()
          globalComplexTypes = Dictionary()
          globalElements = Dictionary()
          simpleTypes = Dictionary()
          complexTypes = Dictionary()
          elements = Dictionary() }


/// Work in progress!
type internal Antani(xsd: XmlSchemaSet) =

    // this type Antani should be a kind of builder so it's using dictionaries
    // to configure customizations
    // once complete we may return another object, immutable or at least no more
    // exposing ways to further change dictionaries.
    member private this.maps = inner.empty
    
    static member CreateFromText(xsdText: string) =
        Antani <| XsdFactory.xmlSchemaSet xsdText

    static member CreateFromUri(xsdUri: string) =
        Antani <| XsdFactory.xmlSchemaSetFromUri xsdUri

    member this.Validate (xml: XElement) = 
        XsdFactory.validate xsd (xml.ToString())

    member this.GlobalElements =
        xsd.GlobalElements.Values
        |> XsdFactory.ofType<XmlSchemaElement>
        |> Seq.map (fun x -> x.QualifiedName)
    
    member this.ArbitraryElement(_name: XmlQualifiedName) : Arbitrary<XElement> =
        failwith "TODO"

    member this.ArbitraryBasicValue(_atomicType: XsdDomain.XsdAtomicType) : Arbitrary<string> =
        failwith "TODO"

    member this.ArbitrarySimpleValue(_name: XmlQualifiedName) : Arbitrary<string> =
        failwith "TODO"

    member this.ArbitraryComplexValue(_name: XmlQualifiedName) : Arbitrary<XElement> =
        failwith "TODO"

    member this.CustomizeElement(name: XmlQualifiedName, arb: Arbitrary<XElement>) : Antani =
        this.maps.globalElements.[XsdFactory.xsdName name] <- arb
        this

    member this.CustomizeElement(_name: XmlQualifiedName, _map: map<Arbitrary<XElement>>) : Antani =
        let key = XsdFactory.xsdName _name
        //let existing = this.maps.globalElements.[key]
        failwith "TODO"

    member this.CustomizeBasicType(_atomicType: XsdDomain.XsdAtomicType, _arb: Arbitrary<string>) : Antani =
        failwith "TODO"

    member this.CustomizeBasicType(_atomicType: XsdDomain.XsdAtomicType, _map: map<Arbitrary<string>>) : Antani =
        failwith "TODO"

    member this.CustomizeSimpleType(_name: XmlQualifiedName, _arb: Arbitrary<string>) : Antani =
        failwith "TODO"

    member this.CustomizeSimpleType(_name: XmlQualifiedName, _map: map<Arbitrary<string>>) : Antani =
        failwith "TODO"

    member this.CustomizeComplexType(_name: XmlQualifiedName, _arb: Arbitrary<XElement>) : Antani =
        failwith "TODO"

    member this.CustomizeComplexType(_name: XmlQualifiedName, _map: map<Arbitrary<XElement>>) : Antani =
        failwith "TODO"
