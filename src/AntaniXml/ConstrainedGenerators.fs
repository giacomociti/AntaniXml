namespace AntaniXml

/// This module provides support for simple types featuring multiple facets.
/// For example when a simple type has both a Length facet and a pattern.
module ConstrainedGenerators =
    open FsCheck
    open LexicalMappings

    /// a generator paired with a property satisfied by its samples
    type ConstrGen<'a> = { Gen: Gen<'a>; Prop: 'a -> bool; Description: string }

    /// logical and of the properties of all generators in a list
    let all gens x = gens |> List.forall (fun gen -> gen.Prop x)

    /// each generator is paired with two lists: the first list with
    /// the samples satisfying the properties of all the generators,
    /// the other list with the remaining samples
    let probe samplesNo gens =
        // size of each sample (we may instead use a varying size?)
        let samplesSize = 10 
        // probes generators to determine how many samples satisfy
        // also the properties of the other generators in the list
        gens
        |> List.map (fun x -> 
            x,
            x.Gen
            |> Gen.sample samplesSize samplesNo 
            |> List.partition (all gens))
        
    /// mix generators by choosing the best (if can be reasonably built)
    /// and enforcing on it also the properties of the other ones
    let mix gens =
        match gens with
        | [] -> failwith "unexpected empty list of generators"
        | [x] -> // only an optimization, but really worth it:
           // with a single generator we do not bother with probing it
           x.Gen //, []
        | _  ->
            let samplesNo, thresholdPercentage = 100, 60
            let probeResults = probe samplesNo gens
            let totalSuccesses = snd >> fst >> List.length
            let best = probeResults |> List.maxBy totalSuccesses
            let mixedGenerator =
                if (totalSuccesses best) * 100 / samplesNo > thresholdPercentage 
                then (fst best).Gen |> Gen.suchThat (all gens)
                else 
//                    gens 
//                    |> List.map (fun g -> g.Description) 
//                    |> printfn "cannot mix constraints for %A" 
                    (fst best).Gen // likely invalid
                    //None
            mixedGenerator//, probeResults


//    let mix gens =
//        match probeAndMix gens with
//        | Some g, _ -> g
//        | None, ranks -> failwithf "cannot mix constraints: %A" ranks


    let tryParse parse x =
        try Some (parse x)
        with :? System.FormatException -> None

    let canParse parse x =
        match tryParse parse x with None -> false | _ -> true

    let lexMap m g =
        { Gen = 
            gen { let! x = g.Gen
                  let! f = x |> m.Format |> Gen.elements 
                  return f } 
          Description = "lexical mappings of " + g.Description
          Prop = fun x -> 
            match tryParse m.Parse x with
            | None -> false
            | Some x -> g.Prop x } 

    let map f g = { g with Gen = g.Gen |> Gen.map f }


    
