# Node-graph vs. legacy parity: handoff

## Goal and rules

Node-graph plants must reproduce the **baseline C#** (commit `7b80ad2`, where this fork branched) exactly.

- Never change legacy simulation logic (`Tick*`, `SeedAgent`, formation code). Fix graphs, graph config, or the graph interpreter instead (see `.cursor/rules/legacy-implementation.mdc`).
- Don't add special-purpose nodes without asking the user first and explaining why.
- Editing the predefined species graphs (`*GraphBuilder.cs`) is allowed.

## The reference: the baseline app as it really ran

Legacy never ran on the raw C# presets. In the baseline app, species went from the server to the old web UI and back, and that round trip changed values:

- Dominance was sent as 0, because the UI never initialized the field.
- Apical bending was sent as `1 − x`.
- Wood growth was multiplied by 24.
- Auxins reach was sent as the misspelled key `AusinsReach`, so the server ignored it.
- `RootsSparsity` was sent, which isn't a property.
- Rhizome, chaining and crown fields weren't sent at all.
- **The server used a .NET 8 source-generated deserializer.** It sets every init-only `SpeciesSettings` property missing from the JSON to 0, not to its C# default. So the real app ran with `RootsDensity` = 0 and `AuxinsReach` = 0. Any harness must deserialize the same way.

Graph-side constants that encode this round trip:

- `DefaultTickConstants` (in `DefaultSpeciesGraphBuilder.cs`)
- `BerganiaTickGraphBuilder.UiRoundTrip`
- `LegacyDefaultPChaining` and `LegacyDefaultPFlowering`

Tests feed legacy the same payload: `Agro.Tests/HistoricalUiSpecies.cs` builds it from `TestData/baseline-predefined-species.json`, and a module initializer sets `SimulationHarness.LegacySpeciesPayload`.

## Current state (uncommitted working tree)

- All five predefined species are bit-identical to baseline through 2400 h. The five-plant scene is identical through 8760 h.
- `dotnet test Agro.Tests`: 322 passed, 4 skipped, 0 failed.
- The frontend typechecks. `ThreeFrontend/tsconfig.json` has pre-existing option conflicts, so typecheck with a temporary config that extends it and unsets `jsxFactory` and `jsxFragmentFactory`, with `"jsx": "react-jsx"` and `"types": []`.

### Graph features added for parity (approved by the user)

- Spawn Meristem and Spawn Flower Meristem have `keepParentResources` and `resourcesFromProduction` inputs. Create Leaves has `keepParentResources`. These emulate legacy passing the agent by value.
- The **Bend Ancestor Petioles** node (`OrientationEffects.BendAncestorPetioles`) is a copy of legacy `bendPetiol`.
- The config entries "Auxins reach" and "Roots density" (both 0) are mapped in `SpeciesSettingsFromConfiguration` and `BehaviorConfigParameterFallback`.

## Comparison harness (outside the repo)

- Baseline worktree: `/home/jakob/Uni/PRplants.worktrees/baseline`. Keep it clean.
- Harness: `/home/jakob/Uni/PRplants.worktrees/geomdump`.
  - `Program.cs` is compiled against both trees. It uses a source-generated `HarnessJsonContext`, like the server.
  - `./rebuild.sh` rebuilds the current harness and regenerates the catalog and requests (`node mkreq.js`). Run it after any graph or config change, because the requests embed the graphs.
  - `./run.sh <tag> [every] [maxHours]` runs `base_`, `cur_` and `curlegacy_` for a tag. Tags: `default`, `persea_americana`, `geranium_cantabrigiense`, `geranium_macrorrhizum`, `bergenia_cordifolia`, `scene5`.
  - `node cmp.js out/base_X.jsonl out/cur_X.jsonl` prints the first divergence (or "none").
  - Requests cap the run at their `TotalHours` (1440). For longer runs, regenerate `req/*_long.json` from fresh requests with a larger `TotalHours`. Stale `_long` files contain old graphs.
  - Debugging: set `GEOM_TRACE_STEP` for per-step traces and `GEOM_FULL_STEPS` for full-field dumps.
- If you rebuild the baseline harness: `dotnet build baseline -c Release`.

## Known limitations / possible next steps

- Spring crown: if the `pNewCrown` roll failed, the graph would still draw one extra random number. This is irrelevant with the historical value 1. It only matters if someone lowers it.
- For Default, the config entry `PetioleAgeBudMinHours` maps to the species field `MaxLeaveAge`. This is harmless at current values.
- There are no dedicated unit tests yet for the new spawn options or the bend node. They're only covered by the Bergania parity runs.
- Not verified: other `HoursPerTick` values beyond 1 and 4, and non-predefined or user-edited species.
