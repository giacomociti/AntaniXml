namespace AntaniXml

module ConstrainedGenerators =
    open FsCheck
    open LexicalMappings

    /// a generator paired with a property satisfied by its samples
    type ConstrGen<'a> = { gen: Gen<'a>; prop: 'a -> bool; description: string }

    /// logical and of the properties of all generators in a list
    let all gens x = gens |> List.forall (fun gen -> gen.prop x)

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
            x.gen
            |> Gen.sample samplesSize samplesNo 
            |> List.partition (all gens))
        
    /// mix generators by choosing the best (if can be reasonably built)
    /// and enforcing on it also the properties of the other ones
    let probeAndMix gens =
        match gens with
        | [] -> None, []
        | [x] -> // only an optimization, but really worth it:
           // with a single generator we do not bother with probing it
           Some(x.gen) , []
        | _  ->
            let samplesNo, thresholdPercentage = 100, 60
            let probeResults = probe samplesNo gens
            let totalSuccesses = snd >> fst >> List.length
            let best = probeResults |> List.maxBy totalSuccesses
            let mixedGenerator =
                if (totalSuccesses best) * 100 / samplesNo > thresholdPercentage 
                then (fst best).gen |> Gen.suchThat (all gens) |> Some
                else None
            mixedGenerator, probeResults


    let mix gens =
        match probeAndMix gens with
        | Some g, _ -> g
        | None, ranks -> failwithf "cannot mix constraints: %A" ranks


    let tryParse parse x =
        try Some (parse x)
        with :? System.FormatException -> None   

    let canParse parse x =
        match tryParse parse x with None -> false | _ -> true

    let lexMap m g =
        { gen = 
            gen { let! x = g.gen
                  let! f = x |> m.format |> Gen.elements 
                  return f } 
          description = "lexical mappings of " + g.description
          prop = fun x -> 
            match tryParse m.parse x with
            | None -> false
            | Some x -> g.prop x } 

    let map f g = { g with gen = g.gen |> Gen.map f }


    
