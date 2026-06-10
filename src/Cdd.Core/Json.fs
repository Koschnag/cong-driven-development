namespace Cdd.Core

/// Zentrale JSON-Konfiguration. FSharp.SystemTextJson serialisiert DUs, Options
/// und Records idiomatisch — Single-Case-Unions (EntityId) werden zu nackten Strings.
module Json =

    open System.Text.Json
    open System.Text.Json.Serialization

    let options =
        let o = JsonSerializerOptions(WriteIndented = true)
        let encoding =
            JsonUnionEncoding.AdjacentTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.UnwrapOption
            ||| JsonUnionEncoding.UnwrapSingleCaseUnions
            ||| JsonUnionEncoding.UnwrapFieldlessTags  // Convergence/Level als nackte Strings
        o.Converters.Add(JsonFSharpConverter(encoding))
        o

    let serialize (value: 'T) = JsonSerializer.Serialize(value, options)

    let deserialize<'T> (json: string) = JsonSerializer.Deserialize<'T>(json, options)
