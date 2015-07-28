namespace AntaniXml.Tests

module XmlGeneratorTest =
    open NUnit.Framework
    open FsCheck
    open FsCheck.NUnit
    open AntaniXml
    open XsdDomain
    open XsdFactory
    open XmlGenerator
    open Microsoft.FSharp.Reflection


    let generateSamples xsdSimpleType = 
        genSimple xsdSimpleType
        |> Gen.sample 10 5
        //|> printfn "%A"

    [<Test>]
    let ``samples of atomic types are generated``() =
        FSharpType.GetUnionCases(typeof<XsdAtomicType>)
        |> Seq.map (fun x -> FSharpValue.MakeUnion(x, [||]))
        |> Seq.cast<XsdAtomicType>
        |> Seq.iter (fun x -> 
            printfn "%A" x
            XsdAtom (x, emptyFacets) 
            |> generateSamples 
            |> printfn "%A")

    [<Test>]
    let ``samples of lists are generated``() =  
        XsdList(XsdAtom (AnyAtomicType, emptyFacets), emptyFacets)
        |> generateSamples |> ignore
        XsdList(XsdAtom (Boolean, emptyFacets), emptyFacets)
        |> generateSamples |> ignore 
        XsdList(XsdAtom (Date, emptyFacets), emptyFacets)
        |> generateSamples |> ignore


    [<Test>]
    let ``samples of union types are generated``() =  
        XsdUnion([XsdAtom (Date, emptyFacets)], emptyFacets)
        |> generateSamples |> ignore
        XsdUnion([XsdAtom (Decimal, emptyFacets)
                  XsdAtom (Date, emptyFacets)], emptyFacets)
        |> generateSamples |> ignore



    let isValid schemaSet (e: System.Xml.Linq.XElement) =
        let valid = validate schemaSet
        match e.ToString() |> valid  with
        | true, _ -> true
        | false, msg ->
            printfn "%s %A" msg e
            false

    let checkSchema xsd =
        let samples = 
            (xsdSchema xsd).Elements 
            |> List.map genElement
            |> List.collect (Gen.sample 5 10)
        samples
        |> List.map (isValid xsd)
        |> List.forall id
        

    let makeSchema =
        sprintf """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" 
            elementFormDefault="qualified" attributeFormDefault="unqualified">
            %s
        </xs:schema>""" 
        >> xmlSchemaSet

    let check = makeSchema >> checkSchema >> Assert.True

    [<Test>]
    let ``valid samples are generated for schema file``()  =
        @"samples.xsd"
        |> xmlSchemaSetFromUri 
        |> checkSchema  
        |> Assert.True
  
    [<Test>]
    let ``valid samples are generated for gpx.xsd``()  =
        //@"http://www.topografix.com/GPX/1/1/gpx.xsd"
        "gpx.xsd"
        |> xmlSchemaSetFromUri 
        |> checkSchema  
        |> Assert.True


    let getSimpleTypes() =
        // available at http://www.w3.org/2001/XMLSchema-datatypes.xsd
        let simpleTypesXsd = xmlSchemaSetFromUri "XMLSchema-datatypes.xsd"
        let unsupportedTypes = Set.ofList ["NOTATION";"ID";"IDREF";"IDREFS";"ENTITY";"ENTITIES"; "NMTOKENS"]
        simpleTypesXsd.GlobalTypes.Values
        |> ofType<System.Xml.Schema.XmlSchemaSimpleType>
        |> Seq.map (fun x -> x.Name)
        |> Set.ofSeq
        |> fun x -> Set.difference x unsupportedTypes

    [<Test>]
    let ``valid samples are generated for attributes of all simple types``() =
        let attributes = 
            getSimpleTypes()
            |> Set.map (fun x ->
                sprintf """<xs:attribute name="%s" type="xs:%s" use="required"/>""" x x)
            |> String.concat System.Environment.NewLine
        sprintf """
        <xs:element name="e">
            <xs:complexType>
            %s
            </xs:complexType>
        </xs:element>""" attributes
        |> check

    [<Test>]
    let ``valid samples are generated for elements of all simple types``() =
        getSimpleTypes()
        |> Set.map (fun x ->
            sprintf """<xs:element name="%s" type="xs:%s" />""" x x)
        |> String.concat System.Environment.NewLine
        |> check

        
    // little schema with only an element of the given type
    let elm =
        sprintf """<xs:element name="e" type="%s" />""" 
        >> makeSchema
        

    // arbitrary xml element for a given schema (likely with a single global element definition)
    let arb schemaSet =
        let xsd = xsdSchema schemaSet
        xsd.Elements |> List.head |> genElement |> Arb.fromGen
        
    // a few examples with schemas having a single element of simple type;
    // these tests are subsumed by checkSimpleTypes;
    // anyway they can make troubleshooting easier by focusing on 
    // a single specific simple type at a time 
    // They also show how testing of real schemas may be done with fscheck:
    // define a type with a static member returning an Arbitrary instance
    // built from the schema; use it in the property attribute to generate
    // random XElement inputs based on the given schema.

    let xsdAnySimpleType = elm "xs:anySimpleType"
    type GenAnySimpleType = static member Elm() = arb xsdAnySimpleType
    [<Property(Verbose=false, Arbitrary = [| typeof<GenAnySimpleType> |]) >]
    let genAnySimpleType x = x |> isValid xsdAnySimpleType

    let xsdDateTime = elm "xs:dateTime"
    type GenDateTime = static member Elm() = arb xsdDateTime
    [<Property(Verbose=false, Arbitrary = [| typeof<GenDateTime> |]) >]
    let genDateTime x = x |> isValid xsdDateTime

    let xsdInt = elm "xs:int"
    type GenInt = static member Elm() = arb xsdInt
    [<Property(QuietOnSuccess=true, Verbose=false, Arbitrary = [| typeof<GenInt> |]) >]
    let genInt x = x |> isValid xsdInt

    let sampleXsd = makeSchema """
	    <xs:element name="e" type="xs:int">
        </xs:element>"""
    type GenSample = static member Elm() = arb sampleXsd
    [<Property(QuietOnSuccess=true, Verbose=false, Arbitrary = [| typeof<GenSample> |]) >]
    let genSample x = x |> isValid sampleXsd

    // example schema provided by w3c
    let purchaseOrder = xmlSchemaSetFromUri @"po.xsd"
    type GenPurchaseOrder = static member Elm() = arb purchaseOrder
    [<Property(QuietOnSuccess=true, Arbitrary = [| typeof<GenPurchaseOrder> |]) >]
    let ``purchase order samples are valid`` x = x |> isValid purchaseOrder

    [<Test>]
    let ``print purchase order sample``() =
        (arb purchaseOrder).Generator 
        |> Gen.sample 5 1
        |> List.iter (printfn "%A")

    [<Test>]
    let ``mixed, fixed and nillable``() = check """
	    <xs:element name="e">
		    <xs:complexType mixed="1">
			    <xs:sequence maxOccurs="2">
				    <xs:element name="fixed" type="xs:string" fixed="aaa" minOccurs="0"/>
				    <xs:element name="nillable" type="xs:date" nillable="true" minOccurs="2" maxOccurs="3"/>
			    </xs:sequence>
		    </xs:complexType>
	    </xs:element>
        """

    [<Test>]
    let ``lists can have facets``() = check """
	    <xs:simpleType name="nums">
		    <xs:list itemType="xs:int" />
	    </xs:simpleType>
	    <xs:element name="e">
		    <xs:simpleType>
			    <xs:restriction base="nums">
				    <xs:maxLength value="2" />
			    </xs:restriction>
		    </xs:simpleType>
	    </xs:element>
        """

    [<Test>]
    let ``patterns can be restricted``() = check """
	    <xs:simpleType name="nums">
		    <xs:restriction base="xs:string">
			    <xs:pattern value="[0-8]{1,3}" />
		    </xs:restriction>
	    </xs:simpleType>
	    <xs:element name="e">
		    <xs:simpleType>
			    <xs:restriction base="nums">
				    <xs:pattern value="[0-9]+" />
			    </xs:restriction>
		    </xs:simpleType>
	    </xs:element>
        """

    [<Test>]
    let ``complex types can reference attribute groups``() = check """
	    <xs:attributeGroup name="myAttributes">
		    <xs:attribute name="myNr" type="xs:int"/>
		    <xs:attribute name="available" type="xs:boolean"/>
	    </xs:attributeGroup>
	    <xs:element name="foo">
		    <xs:complexType>
			    <xs:sequence>
				    <xs:element name="bar" type="xs:string"/>
			    </xs:sequence>
			    <xs:attributeGroup ref="myAttributes"/>
			    <xs:attribute name="lang" type="xs:language"/>
		    </xs:complexType>
	    </xs:element>
        """

    [<Test>]
    let ``complex types can reference groups``() = check """
	    <xs:group name="name">
		    <xs:choice>
			    <xs:element name="name" type="xs:string"/>
			    <xs:sequence>
				    <xs:element name="first-name" type="xs:string"/>
				    <xs:element name="last-name"  type="xs:string"/>
			    </xs:sequence>
		    </xs:choice>
	    </xs:group>
	    <xs:element name="author">
		    <xs:complexType>
			    <xs:sequence>
				    <xs:group ref="name"/>
				    <xs:element name="born" type="xs:string"/>
			    </xs:sequence>
		    </xs:complexType>
	    </xs:element>
        """   
        
    [<Test>]
    let ``complex content can be extended``() = check """
        <xs:complexType name="myBaseType">
	        <xs:sequence>
		        <xs:element name="tok" type="xs:token"/>
	        </xs:sequence>
	        <xs:attribute name="id" type="xs:int"/>
        </xs:complexType>
        <xs:element name="myExtension">
	        <xs:complexType>
		        <xs:complexContent>
			        <xs:extension base="myBaseType">
				        <xs:sequence>
					        <xs:element name="date" type="xs:date"/>
				        </xs:sequence>
			        </xs:extension>
		        </xs:complexContent>
	        </xs:complexType>
        </xs:element>
        """


    [<Test>]
    let ``simple content of complex type can be extended``() = check """
	    <xs:element name="e">
		    <xs:complexType>
			    <xs:simpleContent>
				    <xs:extension base="xs:int">
					    <xs:attribute name="d" type="xs:date" use="required"/>
				    </xs:extension>
			    </xs:simpleContent>
		    </xs:complexType>
	    </xs:element>
        """

    [<Test>]
    let ``simple content can be restricted``() = check """
        <xs:complexType name="tokenWithLangAndNote">
	        <xs:simpleContent>
		        <xs:extension base="xs:token">
			        <xs:attribute name="lang" type="xs:language"/>
			        <xs:attribute name="note" type="xs:token"/>
		        </xs:extension>
	        </xs:simpleContent>
        </xs:complexType>
        <xs:element name="title">
	        <xs:complexType>
		        <xs:simpleContent>
			        <xs:restriction base="tokenWithLangAndNote">
				        <xs:maxLength value="2"/>
				        <xs:minLength value="0"/>
				        <xs:attribute name="lang">
					        <xs:simpleType>
						        <xs:restriction base="xs:language">
							        <xs:enumeration value="en"/>
							        <xs:enumeration value="es"/>
						        </xs:restriction>
					        </xs:simpleType>
				        </xs:attribute>
				        <xs:attribute name="note" type="xs:token" use="prohibited"/>
			        </xs:restriction>
		        </xs:simpleContent>
	        </xs:complexType>
        </xs:element>
        """

    [<Test>]
    let ``substitution groups are supported``() = check """
        <xs:element name="name" type="xs:string"/>
        <xs:element name="navn" substitutionGroup="name"/>
        <xs:complexType name="custinfo">
          <xs:sequence>
            <xs:element ref="name"/>
          </xs:sequence>
        </xs:complexType>
        <xs:element name="customer" type="custinfo"/>
        <xs:element name="kunde" substitutionGroup="customer"/>
        """

    [<Test>]
    let ``cyclic elements are not supported``() = 
        let xsd = makeSchema """
	    <xs:complexType name="TextType" mixed="true">
		    <xs:choice minOccurs="0" maxOccurs="unbounded">
			    <xs:element ref="bold"/>
			    <xs:element ref="italic"/>
			    <xs:element ref="underline"/>
		    </xs:choice>
	    </xs:complexType>
	    <xs:element name="bold" type="TextType"/>
	    <xs:element name="italic" type="TextType"/>
	    <xs:element name="underline" type="TextType"/>
        """
        try 
            xsdSchema xsd |> ignore
            Assert.True false 
        with e -> Assert.True(e.Message = "Recursive schemas are not supported. Element 'bold' has cycles.")

    [<Test>]
    let ``cyclic types are not supported``() = 
        let xsd = makeSchema """
	    <xs:complexType name="SectionType">
          <xs:sequence>
            <xs:element name="Title" type="xs:string" />
            <xs:element name="Section" type="SectionType" minOccurs="0"/>
          </xs:sequence>
        </xs:complexType>
        """
        try 
            xsdSchema xsd |> ignore
            Assert.True false 
        with e -> Assert.True(e.Message = "Recursive schemas are not supported. Element 'Section' has cycles.")

       

module CombinedConstraintsTest =  
    open NUnit.Framework
    open FsCheck
    open AntaniXml
    open ConstrainedGenerators  
    open FacetBasedGenerators

    [<Test>]
    let ``pattern and length facets may be combined`` () =  
        
        let g1 = { gen = genPattern ["[1-9][0-9]"] 
                   description = "pattern"
                   prop = fun x -> 10 <= int x && int x <= 99 } 
        let intGen = Arb.Default.Int32().Generator
        let g2 = boundedGen intGen LexicalMappings.XsdInt XsdFactory.emptyFacets 8 95 1
        let g3, probeResults = probeAndMix [g1; g2] 
        //printfn "probe results: %A" probeResults
        Assert.True(g3.IsSome)
        Gen.sample 10 50 g3.Value
        |> List.map int
        |> List.iter (fun x ->
            //printf "%A " x
            Assert.True (int x >= 10 && x <= 95))

       
module TestLexicalMappings =
    open System
    open FsCheck.NUnit
    open AntaniXml
    open LexicalMappings
    
    let lexicalMappings lexMap equiv x = 
        let lexicalRepresentations = lexMap.format x
        //printfn "%A -> %A" x lexicalRepresentations
        lexicalRepresentations 
        |> List.map lexMap.parse
        |> List.forall (equiv x)

    [<Property>] 
    let LexString x = lexicalMappings XsdString (=) x

    [<Property>] 
    let LexBool x = lexicalMappings XsdBool (=) x

    [<Property>] 
    let LexDecimal x = lexicalMappings XsdDecimal (=) x

    [<Property>] 
    let LexFloat x = 
        lexicalMappings XsdFloat (fun x y -> 
            if Single.IsNaN x && Single.IsNaN y then true 
            else x = y) x

    [<Property>] 
    let LexDouble x = 
        lexicalMappings XsdDouble (fun x y ->
            if Double.IsNaN x && Double.IsNaN y then true 
            else x = y) x

    [<Property>] 
    let LexDuration x = lexicalMappings XsdDuration (=) x
    
    

    [<Property>] 
    //[<Property(Replay="1538024070,296019156", Verbose=true) >]
    // breakpoint on x == new System.DateTime(1992, 3, 29, 2, 0, 0)
    // (StdGen (1620685400,296023950)): 3/26/1922 2:00:00 AM
    // (StdGen (144954390 ,296026340)): 3/30/1952 2:00:00 AM
    let LexDateTime x = lexicalMappings XsdDateTime (=) x

    [<Property>] 
    let LexTime x = 
        lexicalMappings XsdTime 
            (fun x y -> x.TimeOfDay = y.TimeOfDay) x

    [<Property>] 
    let LexDate x = 
        lexicalMappings XsdDate (fun x y -> 
            x.Date = y.Date) x

    [<Property(QuietOnSuccess=false)>] 
    let LexGYearMonth x = 
        lexicalMappings XsdGYearMonth (fun x y -> 
            x.Year = y.Year && x.Month = y.Month) x

    [<Property>] 
    let LexGYear x = 
        lexicalMappings XsdGYear (fun x y -> 
            x.Year = y.Year) x

    [<Property>] 
    let LexGMonthDay x = 
        lexicalMappings XsdGMonthDay (fun x y -> 
            x.Month = y.Month && x.Day = y.Day) x

    [<Property(QuietOnSuccess=false)>] 
    let LexGDay x = 
        lexicalMappings XsdGDay (fun x y -> 
            x.Day = y.Day) x

    [<Property>] 
    let LexGMonth x = 
        lexicalMappings XsdGMonth (fun x y -> 
            x.Month = y.Month) x

    [<Property>] 
    let LexInt x = lexicalMappings XsdInt (=) x

    [<Property>] 
    let LexUInt x = lexicalMappings XsdUInt (=) x

    [<Property>] 
    let LexLong x = lexicalMappings XsdLong (=) x

    [<Property>] 
    let LexULong x = lexicalMappings XsdULong (=) x
    


//module TestLoops =
//    open NUnit.Framework
//    open FsCheck
//    open XsdTools
//
//    type node =
//        | Leaf of int
//        | Nodes of node*node
//
//
//    let rec unsafeTree() = 
//        [ Gen.constant (Leaf(2))
//          gen {
//            let! sub = unsafeTree() |> Gen.two
//            return Nodes sub
//          } ]
//        |> Gen.oneof 
//
//    [<Test>]
//    let loop1() =
//        //let g = Arb.generate<node>
//
//        let g = unsafeTree()
//        g 
//        |> Gen.sample 3 5
//        |> List.iter (printfn "%A")

        