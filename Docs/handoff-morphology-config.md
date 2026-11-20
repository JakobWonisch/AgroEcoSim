# Handoff: Morphology via `behaviorConfiguration`

**For:** next agent completing the morphology-config cleanup plan  
**Branch:** `PredefinedSpeciesNodesRefinement` (WIP, uncommitted at handoff time)  
**Plan:** `.cursor/plans/morphology_config_cleanup_5464718f.plan.md` — do **not** edit the plan file  

**Hard rule:** `.cursor/rules/legacy-implementation.mdc` — never change legacy tick formulas; only signatures, call sites, and graph/config/interpreter paths.

---

## Environment notes

- **Next agent runs in Docker** — `dotnet build` / `dotnet test` are OK; the host was crashing on long/full test runs, so be **efficient** (targeted filters first, full suite last).
- **No access to `/home/jakob`** — do not reference host-only paths (agent transcripts, temp files, terminal logs). Everything you need is in the repo.
- **Workspace root:** clone root (e.g. `/workspace` or wherever the container mounts the repo).

---

## Goal

Single source of truth for node-graph plants: **`behaviorConfiguration`** drives both `Plant.Parameters` at init and wired graph effect inputs. Remove the old HUD/catalog overlay (`ResolveForNodeGraphs`, snapshots, dual frontend morphology sync).

Target flow:

```
behaviorConfiguration
  → SpeciesSettingsFromConfiguration → Plant.Parameters (Init in PlantFormation2 ctor)
  → Configuration Value Input nodes → effect nodes (Create Leaves, spawn, crown pitch)
  → parameterized legacy helpers (LeafLayoutParams, TwigOrientationParams, RhizomeSpawnParams)
```

Legacy `Tick*` / `SeedAgent` still call helpers with explicit args from local `species` / `plant.Parameters`. Legacy `SpeciesMorphology.Resolve` unchanged for non-graph plants.

---

## What is done

### Backend — config → morphology (overlay removed)

| Item | Path |
|------|------|
| Config mapper | `Agro/BehaviorGraph/SpeciesSettingsFromConfiguration.cs` (**new**, was untracked) |
| Profile switch | `Agro/PlantSpeciesProfile.cs` uses `SpeciesSettingsFromConfiguration.Build` |
| Overlay deleted | `Agro/SpeciesMorphology.cs` — only `Resolve()` for legacy |
| Snapshots removed | `SpeciesSettings.cs` static ctor (no `CapturePredefinedSnapshots`) |
| Config-only tick math | `GraphTickInterpreter.ResolveConfigNumber` — no `Plant.Parameters` / overlay fallbacks |

### Backend — parameterized legacy helpers

| Item | Path |
|------|------|
| Structs + factories | `Agro/Plant/MorphologyParams.cs` (**new**) |
| Refactored helpers | `CreateLeavesBase`, `RandomOrientation`, `TrySpawnRhizome`, flower leaf paths in `AboveGroundAgent.cs`, `SpawnEffects.cs`, `OrientationEffects.cs` |
| Legacy call sites | `AboveGroundAgent.cs`, `AboveGroundAgent.Bergania.cs`, Geranium tick files, `SeedAgent.cs`, `FlowerHelper.cs` |

### Backend — graph wiring

| Item | Path |
|------|------|
| Wired input reader | `Agro/BehaviorGraph/MorphologyGraphInputs.cs` (**new**) — unwired socket → matching `ConfigId` via `ResolveConfigNumber` |
| Bootstrap wiring | `TickDefaultGraphTemplates.cs`: `WireLeafLayoutConfig`, `WireTwigOrientationConfig`, `WireRhizomeSpawnConfig` |
| Config catalogs | `DefaultSpeciesGraphBuilder.cs`, `BerganiaTickGraphBuilder.cs`, `PerseaSpeciesGraphBuilder.cs` — new IDs: `Height`, `LeafLengthVar`, `LeafRadiusVar`, `LeafGrowthTime`, `LeafGrowthTimeVar`, `PetioleLengthVar`, `PetioleRadiusVar`, `LeafPitchVar` |
| Interpreter | `GraphCreateLeaves`, spawn nodes, `ApplyCrownPitch` use wired inputs |
| Frontend Rete nodes | `ThreeFrontend/src/components/hud/nodes/output/CreateLeavesNode.ts`, `spawn/SpawnTriggerNode.ts`, `spawn/SpawnNodes.ts` |

**Wiring suffix:** `Wire*Config(..., suffix)` — use distinct suffixes (`mono`, `dicho`, `mer`, `auxin`, etc.) when calling multiple times in one subgraph to avoid duplicate node IDs.

### Backend — `GraphTickInterpreter.cs` fixes (critical)

Effect-subtree (downstream of `Active` gate) evaluation was refactored:

1. **Pass order:** `PureCompute` → `DeferredRandom` → `PureComputeRefresh` (excludes `RandomChanceInput` to avoid double RNG draws) → `Effects` → spawn passes  
2. **Auxin twig:** `WireTwigEffectChain` in `TickDefaultGraphTemplates.cs` — `become-trig` wired from `petiolePath`, **not** `death.seq` (death runs later in Effects pass; stale `seq` blocked petiole→meristem)  
3. **Double spawn fix:** `SpawnMeristem`, `SpawnDichotomousMeristems`, `CreateLeaves`, `SpawnFlowerMeristem` excluded from generic **Effects** pass via `IsOrderedSpawnKind` — they run **only** in dedicated spawn passes. Without this, meristem chain fired twice at t=44 (+5 agents, RNG drift).

**Last verified (before host stopped full-suite runs):** `RngParityDiagnosticTests` t=2, 62, 63 all passed after fix #3.

### Frontend

| Item | Status |
|------|--------|
| `ThreeFrontend/src/helpers/Species.ts` | Removed supplements / dual sync; `loadPredefined()` config-only; graph `serialize()` sends identity only |
| `SpeciesConfigurationEditor.tsx` | No hardcoded rows needed — lists `behaviorConfiguration` from API/catalog dynamically |

### Tests & docs

| Item | Status |
|------|--------|
| `Agro.Tests/SpeciesMorphologyTests.cs` | Rewritten for config assembly (no overlay tests) |
| `Agro.Tests/BehaviorGraphCompilerTests.cs` | Default config count → 54 |
| `Agro.Tests/BerganiaSpeciesParityTests.cs` | Updated incl. `RequiresSpeciesConfigurationForChaining` |
| `Docs/behavior-graph.md` | Updated for config mapper + interpreter passes |
| `.cursor/rules/legacy-implementation.mdc` | Updated |

### Untracked files to include in commit

```
Agro/BehaviorGraph/MorphologyGraphInputs.cs
Agro/BehaviorGraph/SpeciesSettingsFromConfiguration.cs
Agro/Plant/MorphologyParams.cs
```

---

## What likely remains

Full suite was **not** re-run after the RNG double-spawn fix. Last known failures **before** that fix:

| Area | Tests | Notes |
|------|-------|-------|
| RNG parity | `RngParityDiagnosticTests` @ t=62, 63 | **Believed fixed** — re-verify first |
| Agent diagnostics | `AgentTypeParityTests.SingleAgent_FullParity_Diagnostic` (Bergenia/Geranium leaf, meristem) | Structural/RNG drift in traces under `ignore/parity-traces/agent-type/` |
| Bergania chaining | `Geranium_Node_At1440Hours_HoursPerTick4_GrowsPastInitialLeaves` | Node stuck at 2 leaves; check `pChaining` config + meristem chain graph |
| Persea | `PerseaSpecies_UserSettings_1440Hours_*` | ~30s each; run last |

Smoke tests (`SingleAgent_OrganSpecificGraphs_Smoke`, `SingleAgent_StaysAlone`) were passing after interpreter fixes (LengthVar / petiole organ).

---

## Efficient test strategy (Docker)

Build once, then filter:

```bash
# From repo root
dotnet build Agro.Tests/Agro.Tests.csproj

# 1. Quick sanity (~2s)
dotnet test Agro.Tests/Agro.Tests.csproj --no-build \
  --filter "FullyQualifiedName~RngParityDiagnosticTests"

# 2. Smoke parity (~2s)
dotnet test Agro.Tests/Agro.Tests.csproj --no-build \
  --filter "FullyQualifiedName~SingleAgent_OrganSpecificGraphs_Smoke|FullyQualifiedName~SingleAgent_StaysAlone"

# 3. Morphology + compiler
dotnet test Agro.Tests/Agro.Tests.csproj --no-build \
  --filter "FullyQualifiedName~SpeciesMorphologyTests|FullyQualifiedName~BehaviorGraphCompilerTests"

# 4. Bergania (skip 1440h initially)
dotnet test Agro.Tests/Agro.Tests.csproj --no-build \
  --filter "FullyQualifiedName~BerganiaSpeciesParityTests&FullyQualifiedName!~At1440Hours"

# 5. Full suite excluding long Persea
dotnet test Agro.Tests/Agro.Tests.csproj --no-build \
  --filter "FullyQualifiedName!~PerseaSpecies_UserSettings_1440Hours"

# 6. Long Persea last
dotnet test Agro.Tests/Agro.Tests.csproj --no-build \
  --filter "FullyQualifiedName~PerseaSpecies_UserSettings_1440Hours"
```

Avoid piping through `tail`/`rg` on long runs if you need live progress; use `--logger "console;verbosity=minimal"` instead.

---

## Pitfalls

1. **Do not put ordered spawn nodes in Effects pass** — see `IsOrderedSpawnKind` in `GraphTickInterpreter.cs`.  
2. **`become-trig` must use `petiolePath`**, not `death.seq`, in `WireTwigEffectChain`.  
3. **Deferred RNG on effect subtree** needs config evaluated before `RandomFloatVarInput`; refresh pass must not re-run `RandomChanceInput`.  
4. **`ShootsGravitaxis` in config** = post-`Init()` value; mapper stores `postInit / 0.4` before `Init()`.  
5. **DominanceFactors:** config array overrides; else predefined table.  
6. **Node-graph morphology ignores `Species[]` POST** — only `SpeciesConfiguration`.  
7. **Out of scope (plan):** `bendPetiol` Bergania visual parity; underground `RandomOrientation`; agent ctor initial length/radius from `Parameters` is OK to keep.

---

## Suggested completion order

1. Re-verify `RngParityDiagnosticTests` (confirms double-spawn fix).  
2. Fix any remaining `AgentTypeParityTests` / Bergania geranium chaining failures.  
3. Run broader parity subset, then full suite minus long Persea.  
4. Run Persea 1440h tests.  
5. Ensure three new files are tracked; commit when user asks (do not commit unless requested).  
6. Skim `Docs/behavior-graph.md` for consistency with final interpreter behavior.

---

## Git snapshot at handoff

- Branch: `PredefinedSpeciesNodesRefinement` tracking `origin/PredefinedSpeciesNodesRefinement`  
- ~28 modified files, 3 new untracked, net ~−230 lines  
- No commit created by prior agent

---

## Key code references

**SpeciesSettingsFromConfiguration** — clones predefined species via JSON, applies config entries, optional `DominanceFactors` array; `ShootsGravitaxis` pre-Init scaling.

**MorphologyGraphInputs** — maps input socket names to `DefaultSpeciesGraphBuilder.ConfigIds.*`; used by Create Leaves and spawn interpreter paths.

**PlantSpeciesProfile.Resolve** — `BehaviorConfigurationCatalog.ParseSpeciesConfiguration` → `SpeciesSettingsFromConfiguration.Build` → compile graphs.

**Predefined catalog** — `Agro/PredefinedSpecies.cs` exports graphs + `BuildDefaultConfiguration()` / Bergania / Persea builders for `GET /Simulation/species`.
