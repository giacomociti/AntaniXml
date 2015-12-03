namespace AntaniXml.Tests


module XsdFactoryTest =
    open NUnit.Framework
    open AntaniXml
    open XsdDomain
    open XsdFactory

    let unconstrained   = Min 0, Unbounded
    let singleMandatory = Min 1, Max 1
    let anyAtomicType = { Facets = emptyFacets; Variety = XsdAtom(AnyAtomicType) }

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
        let isValid x = (validate xsd x) |> function Success -> true | _ -> false
        isValid  "<e>aaa</e>"    |> Assert.True
        isValid  "<e>aaaa</e>"   |> Assert.False
        isValid  "<e>a  a</e>"   |> Assert.True // becomes 'a a'
        isValid  """<e>   
            a  a   </e>"""       |> Assert.True // becomes 'a a'