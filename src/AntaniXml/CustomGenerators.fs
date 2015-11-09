namespace AntaniXml
    
    open System
    open System.Xml
    open System.Xml.Linq
    open System.Collections.Generic
    open FsCheck
    open XsdDomain
    open XsdFactory

    module Customization =
        /// FSharp friendly customizations
        type Maps = {
            ElementGens: Map<XsdName, Gen<XElement>>
            ElementMaps: Map<XsdName, XElement -> XElement>
            ComplexGens: Map<XsdName, Gen<XElement>> 
            ComplexMaps: Map<XsdName, XElement -> XElement> }
            with static member empty = { ElementGens = Map.empty
                                         ElementMaps = Map.empty
                                         ComplexGens = Map.empty 
                                         ComplexMaps = Map.empty }

    open Customization

    type CustomGenerators() =
        
        let elementGenerators = Dictionary<XmlQualifiedName, Gen<XElement>>()
        let elementGeneratorMaps = Dictionary<XmlQualifiedName, Func<XElement, XElement>>()
        let complexTypeGenerators = Dictionary<XmlQualifiedName, Gen<XElement>>()
        let complexTypeGeneratorMaps = Dictionary<XmlQualifiedName, Func<XElement, XElement>>()

        member this.ForElement(elementName, generator) =
            elementGenerators.Add(elementName, generator)
            this
        member this.ForElement(elementName, mapping) =
            elementGeneratorMaps.Add(elementName, mapping)
            this
        member this.ForComplexType(complexTypeName, generator) =
            complexTypeGenerators.Add(complexTypeName, generator)
            this
        member this.ForComplexType(complexTypeName, mapping) =
            complexTypeGeneratorMaps.Add(complexTypeName, mapping)
            this

        member internal this.ToMaps() =
            {  ElementGens = 
                    elementGenerators.Keys
                    |> Seq.map (fun x -> xsdName x, elementGenerators.Item x)
                    |> Map.ofSeq
               ElementMaps =
                    elementGeneratorMaps.Keys
                    |> Seq.map (fun x -> 
                        xsdName x,
                        let func = elementGeneratorMaps.Item x
                        fun x -> func.Invoke x)
                    |> Map.ofSeq
               ComplexGens = 
                    complexTypeGenerators.Keys
                    |> Seq.map (fun x -> xsdName x, complexTypeGenerators.Item x)
                    |> Map.ofSeq
               ComplexMaps = 
                    complexTypeGeneratorMaps.Keys
                    |> Seq.map (fun x -> 
                        xsdName x,
                        let func = complexTypeGeneratorMaps.Item x
                        fun x -> func.Invoke x)
                    |> Map.ofSeq
            }

    
