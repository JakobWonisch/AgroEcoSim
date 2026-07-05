# Converting a C# tick segment to behavior nodes

Step-by-step guide for translating a block of legacy `AboveGroundAgent` tick logic into one behavior graph. Read [behavior-graph.md](./behavior-graph.md) first for system architecture, node catalog, and runtime semantics.

**Working reference:** [Agro/BehaviorGraph/DefaultSpeciesGraphBuilder.cs](../Agro/BehaviorGraph/DefaultSpeciesGraphBuilder.cs) — bootstrap graphs for the Default species, one graph per `TickDefault` topic.

---

## What you are building

Each conversion produces **one named graph** in the species graph list, for example `"Life support"` or `"Photosynthesis"`.

```
TickDefault (C#)                    Behavior graphs (nodes)
─────────────────                   ─────────────────────────
life support drain          →       Graph 1: "Life support"
photosynthesis block          →       Graph 2: "Photosynthesis"
petiole age / bud RNG         →       Graph 3: "Petiole age bud"
…                             →       …
```

At runtime, `AboveGroundAgent.Tick` runs **all graphs sequentially** for that species. Side effects (`Delta Energy`, `Growth`, `Death`, …) accumulate across graphs in list order.

Each graph must contain **exactly one `Active` node**. Phase 1 of the interpreter evaluates everything upstream of `Active.isActive`; if that boolean is false, the graph returns without running phase-2 effect nodes.

---

## Workflow overview

| Phase | Goal | Main output |
|-------|------|-------------|
| 1. Research | Understand the C# segment precisely | Notes: guards, formulas, inputs, effects, gaps |
| 2. Plan | Design the graph before wiring | Sketch: Active condition, data flow, config keys |
| 3. Implement | Encode in `DefaultSpeciesGraphBuilder` (or editor) | `Build*Subgraph()` + config entries + tests |
| 4. Verify | Prove compile + parity | Compiler tests, trace comparison, UI smoke test |

Work **one segment at a time**. Enable only the graphs you are actively converting (comment out the rest in `BuildDefaultSpeciesSubgraphs()` until ready).

---

## Phase 1 — Research

### 1.1 Locate the source segment

Default species logic lives in:

- [Agro/Plant/AboveGroundAgent.cs](../Agro/Plant/AboveGroundAgent.cs) — `TickDefault`
- Helper methods on `AboveGroundAgent` (e.g. `LifeSupportPerHour`, `EnoughEnergy`)
- Occasionally species-specific files (`AboveGroundAgent.GeraniumSanguineum.cs`, …) for non-Default behaviors

Copy the **exact line range** into your subgraph method's XML summary comment. Future readers use this as a cross-reference.

### 1.2 Classify the segment

For each logical block, fill in this table:

| Question | Example (life support) | Example (photosynthesis) |
|----------|------------------------|--------------------------|
| **When does it run?** | Every tick, all organs | Leaf only, water > 0, irradiance > 0.01 |
| **What is read?** | `Length`, `Radius`, `WoodFactor`, `Organ`, `world.HoursPerTick` | `Water_g`, irradiance, dimensions, organ type |
| **What is written?** | `Energy -= cost` | `Energy +=`, `Water_g -=`, production accumulators |
| **Branching?** | `Organ == Leaf ? … : …` | Nested `if` guards + `Math.Min` |
| **RNG?** | No | No (but petiole bud uses `NextFloatAccum`) |
| **Formation reads?** | No | No (but auxin twig reads parent auxins) |

### 1.3 Map reads to existing input nodes

Use this lookup before inventing new nodes:

| C# source | Node |
|-----------|------|
| `agent.Organ == OrganTypes.*` | `Agent Type Input` → `leaf`, `stem`, … |
| `agent.Energy`, `Water_g`, `Length`, `Radius`, `WoodFactor`, `ageHours`, … | `Agent State Input` |
| `formation.GetPhase` flags | `Phase Input` |
| Parent organ / parent wood | `Parent Input` |
| `world.Irradiance.GetIrradiance(...)` | `Irradiance Input` |
| `world.HoursPerTick` | `Simulation Settings Input` → `hoursPerTick` |
| Species-tunable constants (`LeafThickness`, thresholds, probabilities) | **Configuration Value Input** (see below) |
| `plant.RNG.NextFloat(0,1) < p` | `Random Chance Input` (note: **not** identical to `NextFloatAccum`) |

If a read has **no input node**, record it as a **gap** (see §1.5).

### 1.4 Map writes to effect nodes

| C# mutation | Node |
|-------------|------|
| `Energy += x` / `Energy -= x` | `Delta Energy` (`amount`; use `0 - x` via `Subtract` for decrements) |
| `Water_g += x` / `Water_g -= x` | `Delta Water` |
| `WoodFactor` delta / assign | `Delta Wood` / `Set Wood` |
| `Length` / `Radius` growth | `Growth` |
| `Energy = 0` | `Set Energy` or `Delta Energy` from current value |
| `MakeBud`, `Death`, spawn helpers | Matching `Make Bud`, `Death`, `Spawn *` nodes |
| `CurrentDayProductionInv += …` | `Accumulate Production` |

If a write has **no effect node**, record it as a **gap** and add a `comment` on the nearest planned node (see `BuildPhotosynthesisSubgraph` accumulate stub).

### 1.5 Document gaps explicitly

Partial conversions are expected. Use one of:

1. **`AddBoolStub(true, "MISSING: …")`** in the builder — forces Active gate open until the real condition exists; comment documents the missing capability.
2. **`GraphNodePayload.FromComment("MISSING: …")`** on an effect node — graph compiles; behavior incomplete.
3. **Issue / checklist item** — block enabling the graph in `BuildDefaultSpeciesSubgraphs()` until resolved.

Never silently omit behavior. Either match the C# or mark what is missing.

### 1.6 Note comparison operators carefully

C# `>`, `>=`, `<`, `<=` must match the correct node label:

| C# | Node label |
|----|------------|
| `a > b` | `Greater Than` |
| `a >= b` | `Greater Than or Equal` |
| `a < b` | `Less Than` |
| `a <= b` | `Less Than or Equal` |

Wrong inclusivity is a common parity bug (e.g. `Water_g > 0` must **not** use `Greater Than or Equal`).

---

## Phase 2 — Plan

### 2.1 Choose the graph boundary

One graph = one **coherent tick concern** that shares a single activation story:

- Good: `"Photosynthesis"` — one guard (`leaf && water && light`) and one bundle of effects.
- Good: `"Life support"` — unconditional maintenance cost.
- Avoid: combining unrelated `switch (Organ)` cases unless they truly share one gate.

Name the graph after the topic (verb or noun phrase), matching the list in `DefaultSpeciesGraphBuilder`.

### 2.2 Design the Active gate

```
Phase 1 (upstream of Active)  →  compute isActive boolean
Phase 2 (downstream)          →  effect nodes (only if isActive was true)
```

| Segment style | Active wiring |
|---------------|---------------|
| Always runs (life support) | `Boolean Input(true)` → `Active.isActive` |
| Conditional (photosynthesis) | `And` chain of guards → `Active.isActive` |
| Organ-specific `switch` case | Organ bool + extra conditions → `Active.isActive` |

Remember: **missing wire defaults to false**. Conditional graphs need explicit `Boolean Input(true)` or real guard logic — never assume an unwired `isActive` is true.

### 2.3 Classify every constant

**Rule:** Any tunable species constant that is not supplied by a simulation/agent input node must be a **configuration value**, not a `Number Input`.

| Kind | Where it lives | Example |
|------|----------------|---------|
| Species parameter | `Configuration Value Input` + `BuildDefaultConfiguration()` entry | `LeafThickness` = `0.0001` |
| Simulation parameter | `Simulation Settings Input` | `HoursPerTick` |
| Pure arithmetic literal | `Number Input` OK | `0` for subtraction identity, `2` for surface factor if fixed by formula |
| Boolean literal | `Boolean Input` | `true` for always-active gate |

For each configuration value:

1. Add a **stable id** in `DefaultSpeciesGraphBuilder.ConfigIds` (e.g. `default-config-leaf-thickness`).
2. Add the default entry in `BuildDefaultConfiguration()` with label, type, and value from the C# constant.
3. Wire `Configuration Value Input` nodes to that id in the subgraph.

Stable ids survive server restarts and match saved projects.

### 2.4 Sketch the dataflow

Before coding, draw (on paper or in a comment):

```
[Agent State: length] ──┐
[Agent State: radius] ─┼→ [Multiply] → … → [Delta Energy]
[Config: Leaf thickness]┘
```

Identify:

- Forks → `If / Else` or separate boolean arms into `And`
- `Math.Min(a, b)` → `Less Than` + `If / Else` (see photosynthesis graph)
- `Math.Max` / clamps → `Clamp Max` where applicable

### 2.5 Decide if new nodes are required

Add a new node type only when **no composition** of existing nodes can express the behavior without duplicating huge subgraphs everywhere, **and** the operation will be reused.

If you need a new node, follow the checklist in [behavior-graph.md § Node label and socket reference](./behavior-graph.md#node-label-and-socket-reference-frontend--backend-contract) (Rete class → `nodeFactory.ts` → `GraphNodeKind` → compiler → interpreter → tests).

---

## Phase 3 — Implement

### 3.1 Add the subgraph method

In [DefaultSpeciesGraphBuilder.cs](../Agro/BehaviorGraph/DefaultSpeciesGraphBuilder.cs):

```csharp
/// <summary>
/// TickDefault lines N–M: short description.
/// </summary>
public static ExportedGraph BuildMyTopicSubgraph()
{
    var b = SubgraphBuilder.Create("prefix");  // unique node id prefix
    // … nodes and connections …
    return b.FinishWithActive(gateNodeId, "out").Build();
}
```

`SubgraphBuilder` helpers:

| Method | Purpose |
|--------|---------|
| `Add(...)` | Generic node |
| `AddNum(...)` | `Number Input` (arithmetic literals only) |
| `AddBool(...)` | `Boolean Input` |
| `AddConfig(...)` | `Configuration Value Input` |
| `AddBoolStub(...)` | Documented missing guard |
| `Connect(src, srcOut, tgt, tgtIn)` | Edge |
| `FinishWithActive(src, srcOut)` | Appends `Active` node |
| `GateAlwaysTrue()` | Shorthand for unconditional graphs |

### 3.2 Register configuration entries

When the subgraph uses `AddConfig`:

```csharp
public static class ConfigIds
{
    public const string MyConstant = "default-config-my-constant";
}

public static IReadOnlyList<BehaviorConfigUploadEntry> BuildDefaultConfiguration() =>
[
    // existing entries …
    new()
    {
        Id = ConfigIds.MyConstant,
        Key = "Human label",
        Label = "Human label",
        Type = "number",
        Value = JsonSerializer.SerializeToElement(AboveGroundAgent.SomeConst),
    },
];
```

[PredefinedSpecies.cs](../Agro/PredefinedSpecies.cs) already attaches `BuildDefaultConfiguration()` to the Default species `configuration` field for `GET /Simulation/species`.

### 3.3 Enable the graph in the list

```csharp
public static IReadOnlyList<(string Name, ExportedGraph Graph)> BuildDefaultSpeciesSubgraphs() =>
[
    ("Life support", BuildLifeSupportSubgraph()),
    ("My topic", BuildMyTopicSubgraph()),  // uncomment when ready
];
```

Keep unrelated graphs commented out while iterating — fewer variables when debugging parity.

### 3.4 Update tests

In [Agro.Tests/BehaviorGraphCompilerTests.cs](../Agro.Tests/BehaviorGraphCompilerTests.cs):

1. **`TryCompile_AllDefaultSpeciesSubgraphs_Ok`** — graph compiles; assert expected `GraphNodeKind`s for your segment (e.g. `DeltaEnergy`, `ConfigurationValueInput`).
2. **Binding test** — verify config id on the `Configuration Value Input` node matches `ConfigIds`.
3. **`BuildDefaultConfiguration`** — new config entry present with correct default value.
4. Adjust expected subgraph **count** if you enabled/disabled entries.

Run:

```bash
dotnet test Agro.Tests/Agro.Tests.csproj --filter "DefaultSpecies|TryCompile_AllDefault"
```

### 3.5 Optional: build in the editor instead

You may prototype in the Species tab Rete editor, then transcribe into `DefaultSpeciesGraphBuilder` for the predefined catalog. The editor is useful for layout; the builder is the **source of truth** for Default species bootstrap and repeatable tests.

Use **Auto-layout** in the editor for readable positioning. Export shape must match `ExportedGraph` (see [Conversion.tsx](../ThreeFrontend/src/components/hud/nodes/Conversion.tsx)).

---

## Phase 4 — Verify

### 4.1 Compile check

Every graph must pass `BehaviorGraphCompiler.TryCompile` with exactly one `Active` node and no cycles.

### 4.2 Parity trace (recommended)

[Agro/Testing/SimulationHarness.cs](../Agro/Testing/SimulationHarness.cs) records per-tick plant snapshots:

```csharp
SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, "trace-legacy.jsonl");
SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, "trace-node.jsonl");
```

`PrepareForParity` strips `SpeciesGraphs` for legacy mode. Compare with [TraceComparer.cs](../Agro/Testing/TraceComparer.cs).

Parity checklist for your segment:

- [ ] Same organs affected
- [ ] Same energy/water deltas per tick (within float tolerance)
- [ ] Guards fire on the same ticks
- [ ] Configuration values match C# constants at default settings

### 4.3 Manual UI smoke test

1. Start AgroServer + ThreeFrontend.
2. Select **Default** species → verify graph appears in graph list.
3. Open **Configuration** sidebar → verify config labels/values.
4. Open the graph → verify node types, bindings, comments on gaps.
5. Run a short simulation; watch server stderr for compile errors.

### 4.4 Mark completion status

In the subgraph XML summary, state one of:

- **Complete** — full parity with C# segment
- **Partial** — list missing nodes/capabilities
- **Gate only** — Active condition wired; effects not yet implemented

---

## Reference examples

### Life support (unconditional, complete)

C#: `Energy -= LifeSupportPerTick` for every organ.

| Piece | Implementation |
|-------|----------------|
| Active | `Boolean Input(true)` |
| Formula | `length×radius × (leaf ? configLeafThickness : radius×wood)` |
| Time scale | `× hoursPerTick` from `Simulation Settings Input` |
| Effect | `Subtract(0, perTick)` → `Delta Energy` |
| Config | `Leaf thickness` via `Configuration Value Input` |

### Photosynthesis (conditional, partial)

C#: leaf + `Water_g > 0` + `irradiance > 0.01` → energy gain, water loss, production stats.

| Piece | Implementation |
|-------|----------------|
| Active | `And(leaf, water>0, light>0.01)` |
| `Math.Min` | `Less Than` + `If / Else` |
| Effects | `Delta Energy`, `Delta Water`, `Accumulate Production` |
| Gaps | `CurrentDayEnvResources` increments — commented stub, no effect node yet |

### Dominance factor lookup (resolved)

Legacy `DominanceFactors[DominanceLevel]` maps to:

| Piece | Implementation |
|-------|----------------|
| Table | `number[]` config `Dominance factors` (17 entries from `BuildDominanceFactorsTable`) |
| Index | `Agent State Input` → `dominanceLevel` → `Configuration Array Input` |
| Growth | Meristem/stem multiply chains use array `out` instead of scalar config |

### Known gaps (multi-species rollout)

| Legacy | Status |
|--------|--------|
| `bendPetiol` | Not implemented — document in graph comments |
| `FlowerHelper` | Bergenia flower organs inactive in node mode (`Flower organs gap` graph) |
| Bergania `growthFactor` / `MaxRadius` on meristem growth | Config entries exist; full multiply wiring partial |
| Rhizome collision / `rizomeInfo` flags | Simplified `Spawn Rhizome` + random chance only |
| Config debt | `mPhotoEfficiency`, irradiance threshold, surface factor still `Number Input` — migrate to configuration |

---

## Common pitfalls

| Pitfall | Consequence | Prevention |
|---------|-------------|------------|
| Wrong comparison inclusivity | Guard fires one tick early/late | Match `>` vs `>=` to node label |
| `Number Input` for species constants | Not editable in Configuration sidebar; violates project rule | Use `Configuration Value Input` + `BuildDefaultConfiguration()` |
| Hardcoded `HoursPerTick` | Wrong drain when simulation uses ≠1 hour steps | `Simulation Settings Input` |
| Missing config entry | Runtime reads `0` for config | Add entry to `BuildDefaultConfiguration()` with same id as graph node |
| `Random Chance Input` vs `NextFloatAccum` | Statistical behavior differs | Document gap; add dedicated node if needed |
| Multiple producers into one input | Only **first** wire counts | Avoid fan-in; use explicit `If / Else` |
| Effect in phase-1 subtree | Runs even when Active is false | Effects must be **downstream** of Active (not feeding `isActive`) |
| Enabling all graphs at once | Hard to debug which graph caused drift | Enable one new graph at a time |

---

## Checklist (copy per segment)

### Research
- [ ] C# line range identified and cited in summary
- [ ] Reads mapped to input nodes (or gaps logged)
- [ ] Writes mapped to effect nodes (or gaps logged)
- [ ] Comparison operators classified (`>` vs `>=`)
- [ ] RNG / formation / parent dependencies noted

### Plan
- [ ] Graph name chosen
- [ ] Active gate designed
- [ ] Constants classified (config vs simulation input vs literal)
- [ ] Dataflow sketched
- [ ] New node types justified (if any)

### Implement
- [ ] `Build*Subgraph()` added
- [ ] `ConfigIds` + `BuildDefaultConfiguration()` updated
- [ ] Graph registered in `BuildDefaultSpeciesSubgraphs()`
- [ ] Compiler tests added/updated
- [ ] Missing behavior documented (stubs/comments)

### Verify
- [ ] `dotnet test` passes
- [ ] Parity trace compared (or manual spot-check documented)
- [ ] UI shows graph + configuration
- [ ] Summary comment marks Complete / Partial

---

## Related files

| File | Role |
|------|------|
| [Agro/Plant/AboveGroundAgent.cs](../Agro/Plant/AboveGroundAgent.cs) | Legacy `TickDefault` source |
| [Agro/BehaviorGraph/DefaultSpeciesGraphBuilder.cs](../Agro/BehaviorGraph/DefaultSpeciesGraphBuilder.cs) | Bootstrap graphs + config |
| [Agro/BehaviorGraph/GraphTickInterpreter.cs](../Agro/BehaviorGraph/GraphTickInterpreter.cs) | Runtime semantics |
| [Agro/BehaviorGraph/BehaviorConfiguration.cs](../Agro/BehaviorGraph/BehaviorConfiguration.cs) | Config wire format |
| [Agro/PredefinedSpecies.cs](../Agro/PredefinedSpecies.cs) | `GET /Simulation/species` catalog |
| [Agro/Testing/SimulationHarness.cs](../Agro/Testing/SimulationHarness.cs) | Legacy vs node traces |
| [ThreeFrontend/.../behaviorConfiguration.ts](../ThreeFrontend/src/components/hud/nodes/behaviorConfiguration.ts) | Frontend config types |
| [ThreeFrontend/.../SpeciesConfigurationEditor.tsx](../ThreeFrontend/src/components/hud/SpeciesConfigurationEditor.tsx) | Configuration UI |
| [Docs/behavior-graph.md](./behavior-graph.md) | Full system reference |
| [Docs/node_proposal.md](./node_proposal.md) | Design rationale (paths, Active gating) |
