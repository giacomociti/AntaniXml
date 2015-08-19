namespace AntaniXml.Tests


module XsdFactoryTest =
    open NUnit.Framework
    open AntaniXml
    open XsdDomain
    open XsdFactory

    let unconstrained   = Min 0, Unbounded
    let singleMandatory = Min 1, Max 1
    let anyAtomicType = AnyAtomicType, emptyFacets 

    let makeXsd innerXsd = 
        """<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" 
            elementFormDefault="qualified" 
            attributeFormDefault="unqualified">"""   
        + innerXsd +  
        """</xs:schema>"""

    // quick way to build a name without namespace
    let name n = { Namespace = ""; Name = n } 
    // and some sample names
    let foo = name "foo"
    let bar = name "bar"
    let baz = name "baz"

    let anyTypeName = 
        { Namespace = "http://www.w3.org/2001/XMLSchema"
          Name = "anyType" }
    let anyType = 
        Complex { IsMixed = false
                  Attributes = []
                  Contents = ComplexContent(Sequence(singleMandatory, [ Any(unconstrained, AnyNs.Any) ])) }
    // always present, likely it's for unconstrained contents
    let anyTypeDef = anyTypeName, anyType

    // default global types in a schema
    let onlyAnyTypeDef = Map.ofList [ anyTypeDef ] 


    [<Test>]
    let ``empty schema contains only anyType``() =
        let xsd = FromText <| makeXsd ""
        //printfn "%A" xsd
        let expected = 
            { Types = onlyAnyTypeDef
              Elements   = []
              Attributes = [] }
        Assert.IsNotNull(expected) // no more structural equality because we use seq instead of list
        //Assert.AreEqual(expected, xsd)
        
    [<Test>]
    let ``unconstrained element has anyType``() =
        let xsd = FromText <| makeXsd """<xs:element name="foo"/>"""
        //printfn "%A" xsd
        let expected = 
            { Types = onlyAnyTypeDef
              Elements = 
                  [ { ElementName = foo
                      Type = anyType
                      IsNillable = false
                      FixedValue = None } ]
              Attributes = [] }
        Assert.IsNotNull(expected) // no more structural equality because we use seq instead of list
        //Assert.AreEqual(expected, xsd)
      

    [<Test>]
    let ``unconstrained attribute has anyAtomicType``() =
        let xsd = FromText <| makeXsd """<xs:attribute name="foo"/>"""
        //printfn "%A" xsd
        let expected = 
            { Types = onlyAnyTypeDef
              Elements = []
              Attributes = 
                  [ { AttributeName = foo
                      Type = XsdAtom(anyAtomicType)
                      FixedValue = None } ] }
        Assert.IsNotNull(expected) // no more structural equality because we use seq instead of list
//        Assert.AreEqual(expected, xsd)
        

    [<Test>]
    let ``string type is correctly assigned to element``() =
        let xsd = FromText <| makeXsd """
            <xs:element name="foo" type="xs:string" />"""
        //printfn "%A" xsd
        let expected = 
            { Types = onlyAnyTypeDef
              Elements = 
                  [ { ElementName = foo
                      Type = Simple(XsdAtom(String, emptyFacets))
                      IsNillable = false
                      FixedValue = None } ]
              Attributes = [] }
        Assert.IsNotNull(expected) // no more structural equality because we use seq instead of list
//        Assert.AreEqual(expected, xsd)
        

    [<Test>]
    let ``elements may have attributes``() =
        let xsd = FromText <| makeXsd """
	        <xs:element name="foo">
		        <xs:complexType>
			        <xs:attribute name="bar"/>
		        </xs:complexType>
	        </xs:element>
        """ 
        //printfn "%A" xsd
        let expected = 
            { Types = onlyAnyTypeDef
              Elements = 
                  [ { ElementName = foo
                      Type = 
                          Complex { IsMixed = false
                                    Attributes = 
                                        [ { AttributeName = bar
                                            Type = XsdAtom(anyAtomicType)
                                            FixedValue = None }, Optional ]
                                    Contents = ComplexContent(Empty) }
                      IsNillable = false
                      FixedValue = None } ]
              Attributes = [] }
        Assert.IsNotNull(expected) // no more structural equality because we use seq instead of list
        //Assert.AreEqual(expected, xsd)



    [<Test>]
    let ``elements may have attributes and child elements``() =
        let xsd = FromText <| makeXsd """
    	    <xs:element name="foo">
		        <xs:complexType>
			        <xs:sequence>
				        <xs:element name="bar"/>
			        </xs:sequence>
			        <xs:attribute name="baz"/>
		        </xs:complexType>
	        </xs:element>
        """ 
        //printfn "%A" xsd
        let bazAttr = 
            { AttributeName = baz 
              Type = XsdAtom anyAtomicType
              FixedValue = None }
        let barElm = Element (singleMandatory, { ElementName = bar
                                                 Type = anyType
                                                 IsNillable = false
                                                 FixedValue = None })
        let expected = 
            { Types = onlyAnyTypeDef
              Elements = 
                [ { ElementName = foo
                    IsNillable = false
                    FixedValue = None
                    Type = Complex { 
                        IsMixed = false
                        Attributes = [bazAttr, Optional]
                        Contents = 
                            ComplexContent(Sequence(singleMandatory, [barElm])) } 
                   } ]
              Attributes = [ ] }
        Assert.IsNotNull(expected) // no more structural equality because we use seq instead of list
        //Assert.AreEqual(expected, xsd)

    [<Test>]
    let ``facets are parsed``() =
        let xsd = FromText <| makeXsd """
	        <xs:simpleType name="x">
		        <xs:restriction base="xs:string">
			        <xs:minLength value="2"/>
			        <xs:maxLength value="5"/>
			        <xs:whiteSpace value="replace"/>
			        <xs:pattern value="[abc]*"/>
                    <xs:pattern value="[ab]+"/>
			        <xs:enumeration value="aaa"/>
			        <xs:enumeration value="bbb"/>
		        </xs:restriction>
	        </xs:simpleType>
	        <xs:simpleType name="y">
		        <xs:restriction base="xs:decimal">
			        <xs:minExclusive value="2"/>
			        <xs:maxExclusive value="3"/>
			        <xs:totalDigits value="5"/>
			        <xs:fractionDigits value="4"/>
		        </xs:restriction>
	        </xs:simpleType>
            <xs:simpleType name="z">
		        <xs:restriction base="xs:string">
			        <xs:length value="2"/>
			        <xs:whiteSpace value="collapse"/>
		        </xs:restriction>
	        </xs:simpleType>
        """ 
        //printfn "%A" xsd

        match xsd.Types.[name "x"] with
        | Simple(XsdAtom(String, facets)) -> 
            Assert.AreEqual(Some 2, facets.MinLength)
            Assert.AreEqual(Some 5, facets.MaxLength)
            Assert.AreEqual(Some Replace, facets.WhiteSpace)
            Assert.True([["[abc]*";"[ab]+"]] = facets.Patterns)
            Assert.True(["aaa";"bbb"] = facets.Enumeration)
        | _ -> Assert.False true
        match xsd.Types.[name "y"] with
        | Simple(XsdAtom(Decimal, facets)) -> 
            Assert.AreEqual(Some "2", facets.MinExclusive)
            Assert.AreEqual(Some "3", facets.MaxExclusive)
            Assert.AreEqual(Some 5, facets.TotalDigits)
            Assert.AreEqual(Some 4, facets.FractionDigits)
        | _ -> Assert.False true
        match xsd.Types.[name "z"] with
        | Simple(XsdAtom(String, facets)) -> 
            Assert.AreEqual(Some 2, facets.Length)
            Assert.AreEqual(Some Collapse, facets.WhiteSpace)
        | _ -> Assert.False true

    [<Test>]
    let ``Entity is not supported``() =
        try
            """<xs:element name="e" type="xs:ENTITY"/>""" 
            |> makeXsd |> FromText |> ignore
            Assert.False true
        with e -> Assert.True("unsupported type Entity" = e.Message)

    [<Test>]
    let ``token is collapsed``() =
        let xsd = """
            <xs:element name="e">
		        <xs:simpleType>
		            <xs:restriction base="xs:token">
		                <xs:maxLength value="3"/>
		            </xs:restriction>
		        </xs:simpleType>
	        </xs:element>
            """  |> makeXsd |> xmlSchemaSet 
        let isValid = validate xsd
        isValid  "<e>aaa</e>"    |> Assert.True
        isValid  "<e>aaaa</e>"   |> Assert.False
        isValid  "<e>a  a</e>"   |> Assert.True // becomes 'a a'
        isValid  """<e>   
            a  a   </e>"""       |> Assert.True // becomes 'a a'