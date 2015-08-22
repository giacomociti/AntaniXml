namespace AntaniXml.Tests


module XsdFactoryTest =
    open NUnit.Framework
    open AntaniXml
    open XsdDomain
    open XsdFactory

    let unconstrained   = Min 0, Unbounded
    let singleMandatory = Min 1, Max 1
    let anyAtomicType = { SimpleTypeName = None; Facets = emptyFacets; Variety = XsdAtom(AnyAtomicType) }

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

    [<Test>]
    let ``empty schema contains only anyType``() =
        let xsd = fromText <| makeXsd ""
        Assert.AreEqual([], xsd.Elements)
        Assert.AreEqual([], xsd.Attributes)
        Assert.IsTrue(xsd.Types.ContainsKey anyTypeName)
        
    [<Test>]
    let ``unconstrained element is parsed``() =
        let xsd = fromText <| makeXsd """<xs:element name="foo"/>"""
        match xsd.Elements with
            | [e] -> 
                Assert.AreEqual(foo, e.ElementName)
                Assert.IsFalse e.IsNillable
                Assert.AreEqual(None, e.FixedValue)
            | _ -> failwith "unexpected"
        

    [<Test>]
    let ``unconstrained attribute has anyAtomicType``() =
        let xsd = fromText <| makeXsd """<xs:attribute name="foo"/>"""
        match xsd.Attributes with
        | [a] -> Assert.AreEqual({ AttributeName = foo
                                   Type = anyAtomicType
                                   FixedValue = None }, a)
        | _ -> failwith "unexpected"

    [<Test>]
    let ``string type is correctly assigned to element``() =
        let xsd = fromText <| makeXsd """
            <xs:element name="foo" type="xs:string" />"""
        match xsd.Elements with
        | [e] -> 
            Assert.AreEqual(foo, e.ElementName)
            Assert.AreEqual(Simple { SimpleTypeName = None // should be xsd: string!!
                                     Facets = emptyFacets
                                     Variety = XsdAtom String }, e.Type)
            Assert.IsFalse e.IsNillable
            Assert.AreEqual(None, e.FixedValue)
        | _ -> failwith "unexpected"
        
    [<Test>]
    let ``elements may have attributes``() = 

        let xsd = fromText <| makeXsd """
	        <xs:element name="foo">
		        <xs:complexType>
			        <xs:attribute name="bar"/>
		        </xs:complexType>
	        </xs:element>""" 
        match xsd.Elements with 
        | [e] -> 
            Assert.AreEqual(foo, e.ElementName)
            match e.Type with
            | Complex(t) -> 
                match t.Attributes with
                | [a, u] -> 
                    Assert.AreEqual(bar, a.AttributeName)
                    Assert.AreEqual(anyAtomicType, a.Type)
                    Assert.AreEqual(Optional, u)
                | _ -> failwith "unexpected"
            | _ -> failwith "unexpexted"
        | _ -> failwith "unexpected"

    [<Test>]
    let ``elements may have attributes and child elements``() = 
        let xsd = fromText <| makeXsd """
    	    <xs:element name="foo">
		        <xs:complexType>
			        <xs:sequence>
				        <xs:element name="bar"/>
			        </xs:sequence>
			        <xs:attribute name="baz"/>
		        </xs:complexType>
	        </xs:element>
        """ 
        match xsd.Elements with 
        | [e] -> 
            Assert.AreEqual(foo, e.ElementName)
            match e.Type with
            | Complex(t) -> 
                match t.Attributes with
                | [a, u] -> 
                    Assert.AreEqual(baz, a.AttributeName)
                    Assert.AreEqual(anyAtomicType, a.Type)
                    Assert.AreEqual(Optional, u)
                | _ -> failwith "unexpected"
                match t.Contents with
                | ComplexContent(par) -> 
                    match par with
                    | Sequence ((Min 1, Max 1), items) ->
                        match items |> Seq.exactlyOne with
                        | Element ((Min 1, Max 1), elm) ->
                            Assert.AreEqual(bar, elm.ElementName)
                        | _ -> failwith "unexpexted"
                    | _ -> failwith "unexpexted"
                | _ -> failwith "unexpexted"
            | _ -> failwith "unexpexted"
        | _ -> failwith "unexpected"

        

    [<Test>]
    let ``facets are parsed``() =
        let xsd = fromText <| makeXsd """
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
        | Simple {SimpleTypeName = _; Facets = facets; Variety = XsdAtom(String)}  -> 
            Assert.AreEqual(Some 2, facets.MinLength)
            Assert.AreEqual(Some 5, facets.MaxLength)
            Assert.AreEqual(Some Replace, facets.WhiteSpace)
            Assert.True([["[abc]*";"[ab]+"]] = facets.Patterns)
            Assert.True(["aaa";"bbb"] = facets.Enumeration)
        | _ -> Assert.False true
        match xsd.Types.[name "y"] with
        | Simple {SimpleTypeName = _; Facets = facets; Variety = XsdAtom(Decimal)} -> 
            Assert.AreEqual(Some "2", facets.MinExclusive)
            Assert.AreEqual(Some "3", facets.MaxExclusive)
            Assert.AreEqual(Some 5, facets.TotalDigits)
            Assert.AreEqual(Some 4, facets.FractionDigits)
        | _ -> Assert.False true
        match xsd.Types.[name "z"] with
        | Simple {SimpleTypeName = _; Facets = facets; Variety = XsdAtom(String)} -> 
            Assert.AreEqual(Some 2, facets.Length)
            Assert.AreEqual(Some Collapse, facets.WhiteSpace)
        | _ -> Assert.False true

    [<Test>]
    let ``Entity is not supported``() =
        try
            """<xs:element name="e" type="xs:ENTITY"/>""" 
            |> makeXsd |> fromText |> ignore
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
        let isValid x = (validate xsd x).Valid
        isValid  "<e>aaa</e>"    |> Assert.True
        isValid  "<e>aaaa</e>"   |> Assert.False
        isValid  "<e>a  a</e>"   |> Assert.True // becomes 'a a'
        isValid  """<e>   
            a  a   </e>"""       |> Assert.True // becomes 'a a'