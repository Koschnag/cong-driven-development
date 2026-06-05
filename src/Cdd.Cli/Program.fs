open System

[<EntryPoint>]
let main argv =
    match argv with
    | [| "version" |] ->
        printfn "cdd 0.0.1 (seed)"
        0
    | _ ->
        printfn "cdd — cong-driven-development"
        printfn ""
        printfn "Usage:"
        printfn "  cdd version       Print version"
        printfn ""
        printfn "Status: seed scaffold. SPOT-Modell und Agent-Protokoll folgen."
        0
