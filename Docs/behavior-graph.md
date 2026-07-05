# Behavior Graph Editor

Reference for how the per-species behavior graph is authored, transported, compiled, and executed in the simulation. Captures the state of the system today; intended as a starting point when continuing work on the editor or its runtime.

## Overview

Each species owns an **ordered list** of behavior graphs. Each graph is a node graph describing part of what an above-ground agent does on each tick. Authors edit graphs in the `Species` tab (list on the left, Rete editor on the right). Each exported graph is compiled separately; at runtime `AboveGroundAgent.Tick` runs every compiled graph **sequentially** for that species (side effects such as `Growth` accumulate). Each graph must contain exactly one **`Active`** node; its `isActive` input gates whether the rest of that graph runs (`Boolean Input(true)` wired by default in new graphs).

```
ThreeFrontend (Rete editor)
   |  toJSON / ExportedGraph per NamedGraph
   v
SimulationRequest.SpeciesGraphs: Record<speciesName, { Id, Name, Graph }[]>
   |
   v
Agro/PlantSpeciesProfile.Resolve  -->  BehaviorGraphCompiler.TryCompile (per entry)
                                                |
                                                v
                                       CompiledBehaviorGraph[]
                                                |
                       PlantFormation2.BehaviorGraphs (per plant)
                                                |
                                                v
AboveGroundAgent.Tick -> foreach GraphTickInterpreter.Execute (fresh outs each graph)
```

## Frontend

### UI entry point

- The `Species` tab is wired in [ThreeFrontend/src/components/app.tsx](../ThreeFrontend/src/components/app.tsx) and renders `SpeciesList` from [ThreeFrontend/src/components/hud/Species.tsx](../ThreeFrontend/src/components/hud/Species.tsx).
- `SpeciesList` is a `<select>` of species plus an "Add new species" button. Picking a species populates a module-level signal `selectedSpecies` and renders `SpeciesItem`.
- `SpeciesItem` shows the species detail form, a **Graphs** list (add / rename / reorder / remove), and `<BehaviorEditor key={speciesName:graphId} species={species} namedGraph={...} />` for the selected entry. The `key` remounts Rete when the selected graph or species changes.

### Editor stack

[ThreeFrontend/src/components/hud/nodes/BehaviorEditor.tsx](../ThreeFrontend/src/components/hud/nodes/BehaviorEditor.tsx) builds a Rete editor with these plugins:

- `NodeEditor<Schemes>` (rete)
- `AreaPlugin<Schemes, AreaExtra>` (rete-area-plugin)
- `ConnectionPlugin` (rete-connection-plugin) with the classic preset
- `ContextMenuPlugin` (rete-context-menu-plugin) populated from the node catalog (see below)
- `ReactPlugin` (rete-react-plugin) configured to render via Preact (`createRoot` is wrapped to call Preact's `render`)

Custom controls live in [ThreeFrontend/src/components/hud/nodes/Controls.tsx](../ThreeFrontend/src/components/hud/nodes/Controls.tsx):

- `CustomInputComponent` for numeric input controls.
- `SwitchControl` / `SwitchControlComponent` for boolean toggles.
- `CustomSocketComponent` for socket rendering.
- A module-level event target `graphUpdateTrigger` that user-edited controls dispatch `update` events on. The editor listens and re-runs the in-editor preview / serialization.

Sockets are defined in [ThreeFrontend/src/components/hud/nodes/Sockets.ts](../ThreeFrontend/src/components/hud/nodes/Sockets.ts):

```ts
export const numSocket = new ClassicPreset.Socket('Number');
export const boolSocket = new ClassicPreset.Socket('Boolean');
```

A connection between two sockets with different `name` values is rejected by an `editor.addPipe` interceptor in `BehaviorEditor.tsx` (around the `connectioncreate` branch).

### Editor lifecycle

`createEditor(container, species, namedGraph)` does the following, in order:

1. Builds the editor, area, connection, render, and context-menu plugins.
2. Registers an area pipe to track the latest pointer event and an `connectiondrop` handler that opens the context menu at the drop position; if the user picks a node from that menu, it auto-wires the dropped connection to the new node's first matching socket.
3. Registers an editor pipe that:
    - Validates socket type compatibility on `connectioncreate` and restores the previous edge if mismatched.
    - On `connectioncreated`, `connectionremoved`, `nodecreated`, `noderemoved`, runs `processGraph` (preview-only) and writes the current graph back into `species.behaviorGraphs` for the matching `namedGraph.id` via `pushSpeciesGraph`. If the user removes the sole `Active` node, the editor re-injects an `Active` node.
4. Restores from `fromJSON(namedGraph.graph, ...)` when the graph has nodes. The `Active` output type is not available from the context menu (only one `Active` per graph; duplicates are dropped on `nodecreated`).
5. Registers `appstate.registerBehaviorGraphGetter(speciesName, graphId, () => toJSON(editor, area))` for live snapshots.
6. Pushes the initial state into the species signal, then `AreaExtensions.zoomAt` when possible.

The `useRete` hook returns a ref. The component mounts at:

```337:343:ThreeFrontend/src/components/hud/nodes/BehaviorEditor.tsx
const [ref] = useRete(factory);

return (
    <div style={{ width: '100%', height: '100%', background: 'rgba(0,0,0,0.1)' }}>
        <div ref={ref} style={{ width: '100%', height: '100%' }} />
    </div>
);
```

`destroy` unregisters the graph getter, removes the keyboard handler, and destroys the area.

### Node catalog

Nodes live under [ThreeFrontend/src/components/hud/nodes/](../ThreeFrontend/src/components/hud/nodes/). Every node extends `ClassicPreset.Node` and sets its label via `super('<Label>')`. The label string is the canonical contract with the backend: it is used as the dictionary key in the C# compiler and any change must be mirrored on both sides.

Categories and labels (from the context menu in `BehaviorEditor.tsx` and `nodeFactory.ts`):

- input
  - `Number Input` — control `value` (numeric); output `num: Number`.
  - `Boolean Input` — control `switch` (boolean); output `bool: Boolean`.
  - `Configuration Value Input` — binds `configId` to a species configuration entry; output `num` or `bool`.
  - `Configuration Array Input` — binds `configId` to a `number[]` configuration entry; input `index` (number); output `out` (indexed value, clamped).
  - `Agent Type Input` — organ-type flags: `leaf`, `stem`, `meristem`, `petiole`, `bud`, `flowerStem`, `flowerMeristem`, `flowerBud`, `flowerPadel`, `flowerPetiol` (display labels e.g. "Is Leaf", "Is Stem").
  - `Phase Input` — `preFlower`, `flowering`, `postFlower`, `resetPending` (bool) from `formation.GetPhase`.
  - `Agent State Input` — `energy`, `water`, `length`, `radius`, `wood`, `ageHours`, `isRizome`, `trySpawn`, `wasMeristemThisTick`, `lengthVar`, `radiusVar`, `growthTimeVar`, `dominanceLevel`, `parentRadiusAtBirth`, `previousDayEnvResources`, `previousDayProductionInv`, `energyStorageCapacity`.
  - `Formation Input` — parent/formation context including `auxinLocalMinimum`, `dailyProductionMax`, `energyProductionMax`, `agentHeightRatio`, etc.
  - `Simulation Settings Input` — `hoursPerTick`.
  - `Random Chance Input` — input `p` (0–1); output `out` (bool), `RNG.NextFloat(0,1) < p`.
  - `Random Accum Chance Input` — input `p` (0–1); output `out` (bool), `RNG.NextFloatAccum(p, hoursPerTick)`.
  - `Random Float Var Input` — input `variance`; output `out` (float), `RNG.NextFloatVar(variance)`.
- output
  - `Active` — input `isActive: Boolean`. **Gates the graph** (see interpreter section).
  - `Boolean Output` / `Number Output` — no-ops (placeholders).
  - `Growth` — inputs `Length`, `Radius`; adds deltas to agent size.
  - `Delta Energy` / `Delta Water` / `Delta Wood` — input `amount`.
  - `Set Energy` / `Set Wood` / `Set Auxins` — input `value`.
  - `Multiply Energy` / `Multiply Water` — input `factor`.
  - `Set trySpawn` — input `value` (bool).
  - `Accumulate Production` — input `amount`; adds to `CurrentDayProductionInv`.
  - `Make Bud` / `Create Leaves` — input `trigger` (bool); `Create Leaves` optional `meristemId`; output `seq` on `Create Leaves`.
  - `Death` / `Death Parent` / `Death Children` — input `trigger`; `Death Children` output `seq`.
  - `Become Meristem` / `Become Stem` / `Become Flower Stem` / `Become Flower Meristem` — input `trigger`; output `seq` on meristem/stem transforms.
  - `Set Lateral Angle` — inputs `trigger`, `value`; output `seq`.
  - `Delta Dominance` — inputs `trigger`, `count`; output `seq`.
  - `Set Length Var` — inputs `trigger`, `value`; output `seq`.
  - `Turn Upwards` — input `trigger`; output `seq`.
  - `Set Was Meristem` — input `value` (bool); sets tick-scratch `wasMeristemThisTick` on the agent.
  - `Spawn Meristem`, `Spawn Bud`, `Spawn Stem`, … — input `trigger`; `Spawn Meristem` outputs `childId` and `seq`.
- boolean
  - `And`, `Or`, `Xor` — inputs `a, b: Boolean`; output `out: Boolean`.
  - `Not` — input `a: Boolean`; output `out: Boolean`.
- numeric
  - `Add`, `Subtract`, `Multiply`, `Divide` — inputs `a, b: Number`; output `out: Number`.
  - `Divide` returns `0` when the divisor is `0`.
  - `Parent Wood Cap` — input `value`; output `out` capped to parent wood (rhizome parent exception).
  - `Clamp Max` — inputs `value`, `max`; output `Min(value, max)`.
- logic
  - `Greater Than (or Equal)`, `Less Than (or Equal)` — inputs `a, b: Number`; output `out: Boolean`. Carry an `equal` flag in their `data` (see `ReadInclusiveEqual`) that switches between strict and inclusive comparison.
  - `Equal To` — inputs `a, b: Number`; output `out: Boolean` (uses `|a-b| < 1e-6`).
  - `If / Else` — inputs `condition: Boolean`, `trueValue: Number`, `falseValue: Number`; output `out: Number`.

The single source of truth for labels is the `canonicalBehaviorNodeLabels` array in [ThreeFrontend/src/components/hud/nodes/nodeFactory.ts](../ThreeFrontend/src/components/hud/nodes/nodeFactory.ts), which also exposes:

- `creators` — map from label to factory that takes the exported `data` payload and returns a node instance.
- `createNodeFromExport({ id, label, data })` — used by `fromJSON` to reconstruct a saved graph.
- `isSupportedBehaviorExportLabel(label)` — defensive check used to ignore unknown nodes.

### Serialization shape

Defined in [ThreeFrontend/src/components/hud/nodes/Conversion.tsx](../ThreeFrontend/src/components/hud/nodes/Conversion.tsx):

```ts
export interface ExportedGraph {
    nodes: { id: string; label: string; data: any; position: { x: number; y: number } }[];
    connections: { id: string; source: string; sourceOutput: string; target: string; targetInput: string }[];
}
```

`toJSON(editor, area)` uses `editor.getNodes()` and `editor.getConnections()`. For each node it captures the `id`, `label` (`node.label || node.constructor.name`), the live position from `area.nodeViews`, and a `data` payload via `exportNodeData`:

- If `node.data` is an object, it deep-clones it.
- If `node.data` is a function (the common case for all nodes here), it persists only the primitive control values that are reliably restorable: `valueControl.value` as `value`, `switchControl.value` as `bool`. This is why `NumberInputNode` data round-trips as `{ value: number }` and `BooleanInputNode` as `{ bool: boolean }`.

`fromJSON(data, editor, area, createNode)` clears the editor, recreates each node via `createNodeFromExport`, restores its id and position, and re-adds connections (silently dropping any that reference unknown nodes or violate socket types).

### Persistence on Species

[ThreeFrontend/src/helpers/Species.ts](../ThreeFrontend/src/helpers/Species.ts) holds per-species state. Behavior graphs live in:

```ts
behaviorGraphs = signal<NamedGraph[]>([createDefaultNamedGraph("Main")]);
```

`save()` includes `graphs: structuredClone(this.behaviorGraphs.peek())`. `load(s)` reads `s.graphs` if present. `loadPredefined(entry)` reads `entry.graphs`. `NamedGraph` is `{ id, name, graph: ExportedGraph }`. Helpers `createDefaultExportedGraph` / `createDefaultNamedGraph` in [Conversion.tsx](../ThreeFrontend/src/components/hud/nodes/Conversion.tsx) add `Boolean Input(true) → Active.isActive`.

### App-level wiring

[ThreeFrontend/src/appstate.ts](../ThreeFrontend/src/appstate.ts) tracks the live editor state separately from the signal so the request body always reflects unsaved changes:

```ts
private behaviorGraphGetters = new Map<string, Map<string, () => ExportedGraph>>();
registerBehaviorGraphGetter(speciesName, graphId, getter);
unregisterBehaviorGraphGetter(speciesName, graphId);
collectSpeciesGraphs(): Record<string, { Id, Name, Graph }[]>;
```

`collectSpeciesGraphs` builds, per species, an array of `{ Id, Name, Graph }`, preferring a live getter per `graphId` when an editor is mounted. A species is included if **any** graph entry has nodes or connections. `requestBody()` adds `SpeciesGraphs` when non-empty:

```ts
...(Object.keys(speciesGraphs).length > 0 ? { SpeciesGraphs: speciesGraphs } : {}),
```

The body is `POST`ed to `/simulation/upload`; the returned id is then passed to `hubConnection.invoke("start", preparedID)` over SignalR.

## Transport

The wire format is `Record<speciesName, ExportedGraph>` keyed by species `Name`. The matching DTO on the server is in [Agro/RequestModels/SimulationRequest.cs](../Agro/RequestModels/SimulationRequest.cs):

```csharp
[JsonPropertyName("SpeciesGraphs")]
public Dictionary<string, List<SpeciesGraphUploadEntry>>? SpeciesGraphs { get; init; }
```

`SpeciesGraphUploadEntry` has `Id`, `Name`, and `Graph` (`global::ExportedGraph`). `GraphNode.Data` is a `JsonElement` for dynamic node payloads.

## Backend

### Resolution per plant

[Agro/PlantSpeciesProfile.cs](../Agro/PlantSpeciesProfile.cs) `Resolve(speciesName, settings)`:

1. Resolves morphology via `SpeciesMorphology.Resolve`.
2. If `settings.SpeciesGraphs` contains a key for this species (case-insensitive fallback if no exact match), iterates each `SpeciesGraphUploadEntry`, compiles `Graph` when non-empty, and collects successful `CompiledBehaviorGraph` instances in list order.
3. Logs to `Console.Error` for missing keys, empty entries, or per-graph compile errors.
4. Returns `PlantSpeciesProfile { Morphology, BehaviorGraphs }`.

[Agro/Initialize.cs](../Agro/Initialize.cs) passes `profile.BehaviorGraphs` into `PlantFormation2`. The same compiled list is shared for all plants of that species (immutable at runtime).

### Compiler

[Agro/BehaviorGraph/BehaviorGraphCompiler.cs](../Agro/BehaviorGraph/BehaviorGraphCompiler.cs) `TryCompile`:

1. Rejects empty graphs.
2. Builds an `id -> index` map (rejects empty or duplicate ids).
3. Builds a `List<(Source, SourceOutput, TargetInput)>` of incoming edges per node, ignoring connections whose endpoints don't exist.
4. Maps each node's label to a `GraphNodeKind` via `TryMapNode`. Reads payload data:
    - `Number Input` -> `payload.num` from `value` or `num`.
    - `Boolean Input` -> `payload.boo` from `bool` (also accepts numeric truthy).
    - `Greater Than (or Equal)` and `Less Than (or Equal)` -> `payload.inclusive` from `equal > 0`.
5. Topologically sorts the nodes (Kahn's algorithm). Cycles fail with an explanatory error.
6. Emits a `CompiledNode[]` in topological order; each carries `GraphNodeIndex` (original index, used as the keying base for the runtime output dictionary), `Id`, `Kind`, `Inputs` (a `Dictionary<string, List<(producerIndex, producerSocket)>>`), and `NumberConst`/`BoolConst`/`NumericInclusive` payloads.

The output type is `CompiledBehaviorGraph` with `NodesInOrder`, `ActiveGateTopoIndex` (topo index of the unique `Active` node), and `ActiveSubtreeMask` (topo slots that are transitive producers of `isActive`, excluding the `Active` node itself). Compile fails if there is not exactly one `Active` node.

### Interpreter

[Agro/BehaviorGraph/GraphTickInterpreter.cs](../Agro/BehaviorGraph/GraphTickInterpreter.cs) `Execute(ref agent, formation, agentId, timestep, graph)`:

- Allocates a fresh `outs` dictionary for **this** graph (no state carried to the next graph in the list).
- **Phase 1:** Walks `NodesInOrder` in order, evaluating only nodes whose topo index is set in `ActiveSubtreeMask`.
- Reads `FirstBool(gateNode.Inputs, "isActive", outs)` for the compiled `Active` node. If false, returns without running the rest.
- **Phase 2:** Walks `NodesInOrder` again, evaluating nodes **not** in `ActiveSubtreeMask` (the rest of the graph, including `Growth` and any `Active` node if present in that phase).
- Effect nodes apply agent/formation mutations when evaluated in phase 2. `Boolean Output` / `Number Output` / `Active` remain no-ops.
- Input nodes write read-only values into `outs` (editor preview returns zeros/false; server uses live agent/formation).
- Spawn/death/bud/leaves nodes require a valid `formation` and `agentId`; simple field deltas work without formation.

[Agro/BehaviorGraph/WireValue.cs](../Agro/BehaviorGraph/WireValue.cs) is a small tagged union:

- `OfBool`, `OfFloat`, `Missing` constructors.
- `TryGetBool` returns the bool, or `floatValue != 0` for a float.
- `TryGetFloat` returns the float, or `1f`/`0f` for a bool.
- `AsBool()` and `AsFloat()` are convenience accessors that return defaults on `Missing`.

When an input has no incoming connection, `FirstFloat` returns `0f` and `FirstBool` returns `false`. This is important context for any feature that wants a default-true gate.

### Tick wiring

[Agro/Plant/AboveGroundAgent.cs](../Agro/Plant/AboveGroundAgent.cs): if `formation.Plant.BehaviorGraphs` is non-empty, iterates it and calls `GraphTickInterpreter.Execute` for each, then returns (skips the legacy `Behavior` switch). [Agro/Plant/PlantFormation.cs](../Agro/Plant/PlantFormation.cs) stores `IReadOnlyList<CompiledBehaviorGraph> BehaviorGraphs` (default empty).

## Node label and socket reference (frontend ↔ backend contract)

The label strings and socket names below are the contract that must match across all three layers (Rete node class, `nodeFactory` map, and `BehaviorGraphCompiler.TryMapNode`).

- `Number Input` — out `num`.
- `Boolean Input` — out `bool`.
- `Agent Type Input` — out `leaf`, `stem`, `meristem`, `petiole`, `bud`, `flowerStem`, `flowerMeristem`, `flowerBud`, `flowerPadel`, `flowerPetiol`.
- `Phase Input` — out `preFlower`, `flowering`, `postFlower`, `resetPending`.
- `Agent State Input` — out `energy`, `water`, `length`, `radius`, `wood`, `ageHours`, `isRizome`, `trySpawn`.
- `Parent Input` — out `parentIsRhizome`, `parentWood`.
- `Irradiance Input` — out `irradiance`.
- `Random Chance Input` — in `p`; out `out`.
- `Parent Wood Cap` — in `value`; out `out`.
- `Clamp Max` — in `value`, `max`; out `out`.
- `Active` — in `isActive`.
- `Boolean Output` — in `bool`.
- `Number Output` — in `num`.
- `Growth` — in `Length`, `Radius`.
- `Delta Energy` / `Delta Water` / `Delta Wood` — in `amount`.
- `Set Energy` / `Set Wood` / `Set Auxins` — in `value`.
- `Multiply Energy` / `Multiply Water` — in `factor`.
- `Set trySpawn` — in `value`.
- `Accumulate Production` — in `amount`.
- `Make Bud` / `Create Leaves` / `Death` / `Death Parent` / `Death Children` / `Become *` / `Spawn *` — in `trigger`.
- `And`, `Or`, `Xor` — in `a`, `b`; out `out`.
- `Not` — in `a`; out `out`.
- `Add`, `Subtract`, `Multiply`, `Divide` — in `a`, `b`; out `out`.
- `Greater Than (or Equal)`, `Less Than (or Equal)` — in `a`, `b`; out `out`; data `equal: number` (>0 means inclusive).
- `Equal To` — in `a`, `b`; out `out`.
- `If / Else` — in `condition`, `trueValue`, `falseValue`; out `out`.

When adding a new node:

1. Add the Rete class under `ThreeFrontend/src/components/hud/nodes/<category>/`.
2. Register it in the context-menu list in `BehaviorEditor.tsx` and in the `creators` map and `canonicalBehaviorNodeLabels` array in `nodeFactory.ts`.
3. Add a matching case to `GraphNodeKind`, `BehaviorGraphCompiler.TryMapNode`, and `GraphTickInterpreter.Execute`.

## Tests

[Agro.Tests/BehaviorGraphCompilerTests.cs](../Agro.Tests/BehaviorGraphCompilerTests.cs) covers:

- `TryCompile_AddChain_TopologicalOrder` — basic compile of `Number Input -> Add` with two operands.
- `TryCompile_Cycle_Fails` — rejects a 2-node `Not` cycle.
- `TryCompile_UnknownLabel_Fails` — surfaces unsupported labels.
- `TryCompile_Growth_NodeOnly_Ok` — empty inputs are tolerated.
- `TryCompile_Growth_WithLengthRadiusInputs` — verifies the `Length` and `Radius` input keys are preserved.

There is no end-to-end tick test for the interpreter today; adding one means stubbing or constructing an `AboveGroundAgent` and a `PlantSubFormation<AboveGroundAgent>` and asserting against `agent.Length` / `agent.Radius` after `Execute`.

## Behaviors and limitations to be aware of

- **Multiple graphs per species**, ordered. Wire format: `SpeciesGraphs[speciesName]` is an array of `{ Id, Name, Graph }`.
- **`Active` is mandatory and gates execution** of the rest of that graph. `Boolean Output` / `Number Output` remain inert when evaluated.
- **Inputs default to falsy.** Missing connections produce `0f` / `false`. Any feature that needs a different default has to inject explicit producer nodes.
- **Multiple producers into one input.** The runtime only consults the first producer in the `Inputs[targetInput]` list. The compiler appends in connection iteration order.
- **`processGraph` is preview-only.** It runs entirely client-side, calling each node's `data(inputsData)` to populate `[value]` debug labels in the editor. It does not influence the simulation.
- **Editor preview vs. server runtime.** `Agent Type Input` outputs are forced to `false` in the editor preview but driven by the live agent on the server (`WriteAgentTypeInput`).
- **Position is purely cosmetic.** It is captured and restored, but the simulation ignores it.
- **Graph reuse across plants.** A single `CompiledBehaviorGraph` is constructed per species at resolve time and shared by every `PlantFormation2` of that species. Compiled state must remain immutable for the duration of a simulation.
- **Logging.** Compile errors and key resolution issues are written to `Console.Error.WriteLine` from `PlantSpeciesProfile`; the frontend does not currently surface them.

## Files at a glance

Frontend:

- [ThreeFrontend/src/components/app.tsx](../ThreeFrontend/src/components/app.tsx) — tab wiring.
- [ThreeFrontend/src/components/hud/Species.tsx](../ThreeFrontend/src/components/hud/Species.tsx) — Species selector and detail form.
- [ThreeFrontend/src/components/hud/nodes/BehaviorEditor.tsx](../ThreeFrontend/src/components/hud/nodes/BehaviorEditor.tsx) — Rete editor, plugins, lifecycle.
- [ThreeFrontend/src/components/hud/nodes/Conversion.tsx](../ThreeFrontend/src/components/hud/nodes/Conversion.tsx) — `ExportedGraph`, `toJSON`, `fromJSON`.
- [ThreeFrontend/src/components/hud/nodes/nodeFactory.ts](../ThreeFrontend/src/components/hud/nodes/nodeFactory.ts) — label catalog and reconstruction.
- [ThreeFrontend/src/components/hud/nodes/Sockets.ts](../ThreeFrontend/src/components/hud/nodes/Sockets.ts) — `numSocket`, `boolSocket`.
- [ThreeFrontend/src/components/hud/nodes/Controls.tsx](../ThreeFrontend/src/components/hud/nodes/Controls.tsx) — controls and `graphUpdateTrigger`.
- [ThreeFrontend/src/components/hud/nodes/NodeTypes.ts](../ThreeFrontend/src/components/hud/nodes/NodeTypes.ts) — `Schemes`, `AreaExtra` aliases.
- [ThreeFrontend/src/components/hud/nodes/input/](../ThreeFrontend/src/components/hud/nodes/input/), [output/](../ThreeFrontend/src/components/hud/nodes/output/), [util/boolean/](../ThreeFrontend/src/components/hud/nodes/util/boolean/), [util/numeric/](../ThreeFrontend/src/components/hud/nodes/util/numeric/), [util/logic/](../ThreeFrontend/src/components/hud/nodes/util/logic/) — node classes.
- [ThreeFrontend/src/helpers/Species.ts](../ThreeFrontend/src/helpers/Species.ts) — `Species.behaviorGraph` and save/load.
- [ThreeFrontend/src/appstate.ts](../ThreeFrontend/src/appstate.ts) — `registerBehaviorGraphGetter`, `collectSpeciesGraphs`, `requestBody`.

Backend:

- [Agro/RequestModels/SimulationRequest.cs](../Agro/RequestModels/SimulationRequest.cs) — `SpeciesGraphs` DTO.
- [Agro/PlantSpeciesProfile.cs](../Agro/PlantSpeciesProfile.cs) — `Resolve` and graph compilation entry point.
- [Agro/BehaviorGraph/BehaviorGraphCompiler.cs](../Agro/BehaviorGraph/BehaviorGraphCompiler.cs) — `TryCompile`, label mapping, topological sort.
- [Agro/BehaviorGraph/GraphTickInterpreter.cs](../Agro/BehaviorGraph/GraphTickInterpreter.cs) — per-tick execution.
- [Agro/BehaviorGraph/WireValue.cs](../Agro/BehaviorGraph/WireValue.cs) — wire value union.
- [Agro/Plant/PlantFormation.cs](../Agro/Plant/PlantFormation.cs) — `BehaviorGraph` storage.
- [Agro/Plant/AboveGroundAgent.cs](../Agro/Plant/AboveGroundAgent.cs) — graph short-circuit at the top of `Tick`.
- [Agro/Initialize.cs](../Agro/Initialize.cs) — passes the compiled graph into `PlantFormation2`.
- [Agro.Tests/BehaviorGraphCompilerTests.cs](../Agro.Tests/BehaviorGraphCompilerTests.cs) — compiler unit tests.
