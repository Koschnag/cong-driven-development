namespace Cdd.Core

open System

/// Fail-closed capability boundary for the public web host. The static research
/// surfaces and read-only SPOT projections remain available by default; every
/// mutation and every host/memory inspection needs an explicit deployment flag.
module PublicRuntimeBoundary =

    type Capability =
        | PublicRead
        | Mutation
        | MemoryRead
        | MemoryWrite
        | InfraRead
        | WorkspaceRead

    type Policy =
        { AllowMutations : bool
          EnableMemory : bool
          EnableInfra : bool
          EnableWorkspaces : bool }

    let private isWriteMethod (methodName: string) =
        match methodName.Trim().ToUpperInvariant() with
        | "GET" | "HEAD" | "OPTIONS" -> false
        | _ -> true

    let classify (methodName: string) (path: string) : Capability =
        let normalized =
            if String.IsNullOrWhiteSpace path then "/"
            else path.Trim().ToLowerInvariant()
        if normalized.StartsWith("/api/dwh/", StringComparison.Ordinal) then
            if isWriteMethod methodName then MemoryWrite else MemoryRead
        elif normalized.StartsWith("/api/infra/", StringComparison.Ordinal) then
            InfraRead
        elif normalized.StartsWith("/api/studio/workspaces", StringComparison.Ordinal) then
            WorkspaceRead
        elif normalized.StartsWith("/api/providers", StringComparison.Ordinal) then
            Mutation
        elif isWriteMethod methodName then
            Mutation
        else
            PublicRead

    let isAllowed (policy: Policy) capability =
        match capability with
        | PublicRead -> true
        | Mutation -> policy.AllowMutations
        | MemoryRead -> policy.EnableMemory
        | MemoryWrite -> policy.EnableMemory && policy.AllowMutations
        | InfraRead -> policy.EnableInfra
        | WorkspaceRead -> policy.EnableWorkspaces

    let private enabled (value: string) =
        String.Equals(value, "true", StringComparison.OrdinalIgnoreCase)

    let fromEnvironment (getValue: string -> string) : Policy =
        { AllowMutations = getValue "CDD_ALLOW_MUTATIONS" |> enabled
          EnableMemory = getValue "CDD_ENABLE_MEMORY" |> enabled
          EnableInfra = getValue "CDD_ENABLE_INFRA" |> enabled
          EnableWorkspaces = getValue "CDD_ENABLE_WORKSPACES" |> enabled }
