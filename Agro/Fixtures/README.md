# Parity test fixtures

Capture a `SimulationRequest` JSON from the UI to replay the same scene and seed in the CLI parity harness.

## Capture from the UI

1. Disable **Randomize** and set a fixed **Init number** (simulation seed).
2. Configure species, seeds, field, and behavior graphs as needed.
3. Click **Run** and copy the JSON printed in the browser console (`requestBody()` is logged in `appstate.run`).
4. Save as e.g. `geranium-ui.json` in this folder.

Use explicit `Plants[].P` positions so plant placement does not depend on extra RNG during `Initialize`.

## CLI usage

From the repository root (or this worktree):

```bash
dotnet run --project Agro -- \
  -i Agro/Fixtures/your-fixture.json \
  --record-legacy /tmp/legacy.jsonl \
  --max-hours 168

dotnet run --project Agro -- \
  -i Agro/Fixtures/your-fixture.json \
  --record-node /tmp/node.jsonl \
  --max-hours 168

dotnet run --project Agro -- \
  --compare /tmp/legacy.jsonl /tmp/node.jsonl
```

Legacy runs strip `SpeciesGraphs` and use the `Behavior` enum from `Species[]`. Node runs compile `SpeciesGraphs` and ignore the legacy tick switch.

Optional flags: `--tolerance 1e-5`, `--ignore-rng` (skip per-plant RNG state in the diff).

## Git worktree

This harness was developed on branch `species-parity-harness`. Use a worktree to keep parity work isolated:

```bash
git worktree add ../PRplants.worktrees/species-parity-harness -b species-parity-harness
```
