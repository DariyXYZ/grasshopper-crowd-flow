---
title: Integration Lessons 2026-04-23
date: 2026-04-23
tags:
  - project/ghcrowdflow
  - type/postmortem
  - type/lessons
  - status/active
aliases:
  - Crowd 2026-04-23 Lessons
---

# Integration Lessons 2026-04-23

## Context

This note records the useful findings from the INDTools integration and Rhino/Grasshopper validation pass on 2026-04-23.

Active repo during this pass:

- `C:\Users\dariy.n\source\repos\INDToolsUpdate`
- branch observed earlier: `feature/crowd-engine-integration`

The working goal was to move the crowd engine into the shared INDTools repo structure, make it build/deploy as a normal plugin, and then improve the solver enough that the canonical `Flow.3dm` / `Flow.gh` test no longer stalls.

## Final confirmed state

The last validated build in Rhino/Grasshopper was:

```text
Engine build: 2026-04-23.6
Total: 21294.4 ms
Grid: 209.9 ms (134 x 134)
Path fields: 89.1 ms (3 exits)
Simulation: 20986.3 ms (2210 frames, 150 spawned, 150 completed, 0 active)
Result metrics: 6.5 ms
Simulated duration: 220.9 s / max 784.366 s
Active tail: none
Stop reason: all agents completed
```

This is the first confirmed build in this cycle where all expected agents spawned and completed in the user's canonical scene.

## What was useful

### Integration into INDToolsUpdate

- Crowd source was moved into the shared repo shape:
  - `src/Crowd`
  - `src/GrasshopperComponents/Components/Crowd`
  - `src/GrasshopperComponents/Resources/Crowd*.png`
  - `tests/grasshopper/crowd/flow-demo`
  - `docs/crowd`
- `Crowd.csproj` was added to the solution.
- `GrasshopperComponents` references `Crowd`.
- Crowd outputs are deployed as part of `INDGrasshopperComponents.gha`.
- Components follow the existing INDTools pattern and are not developer-only.

### Build/deploy reliability

- The build can compile all target frameworks.
- The Rhino-off deploy path copies fresh files into:
  - `%APPDATA%\Grasshopper\Libraries\INDTools\net48`
  - `%APPDATA%\Grasshopper\Libraries\INDTools\net7.0`
  - `%APPDATA%\Grasshopper\Libraries\INDTools\net8.0`
- The reliable verification is not "build succeeded"; it is:
  - Rhino is closed
  - build runs without `SkipGrasshopperUserDeploy`
  - deployed `.gha` / `Crowd.dll` timestamps are checked
  - Rhino is reopened
  - `Prof` confirms the expected `Engine build`

### Runtime diagnostics

The `Prof` output became the key debugging tool.

Useful fields:

- `Engine build`
- `Grid`
- `Path fields`
- `Simulation`
- `spawned / completed / active`
- `Simulated duration / max`
- `Last completion age`
- `Active tail`
- `Stop reason`

This made it possible to distinguish:

- build/deploy mismatch
- visual-only heatmap issues
- true solver stalling
- spawn accounting bugs
- long tail that is still moving
- long tail that is fully frozen

## Mistakes and false leads

### Mistake: trusting a successful build too early

A successful `dotnet build` did not mean Rhino/Grasshopper had loaded the new code.

Correct rule:

> [!warning]
> Do not interpret any Rhino-side behavior until the `Prof` output shows the expected `Engine build`.

### Mistake: treating visual smoothing as solver progress

Build `2026-04-23.4` improved the heatmap look, but the solver still failed:

```text
76 spawned, 47 completed, 29 active
avg speed 0.011 m/s
Stop reason: maximum simulation duration reached
```

The lesson is that heatmap smoothness and agent progress are separate. A prettier image can hide a still-broken solver.

### Mistake: speed tuning before identifying the zero-motion condition

Trying to adjust speed limits was not enough. The real defect was not "agents are slow"; it was that late agents could be repeatedly forced back to `agent.Position`.

Diagnostic sign:

```text
avg speed 0 m/s
Last completion age: 309 s
```

That means the active tail is not walking slowly. It is frozen.

### Mistake: losing agents during spawn

The source remainder was decremented by intended spawn count, not by actual spawned count. If the spawn point was occupied, an agent could fail to appear while the source accounting still moved on.

Observed symptom before the fix:

- roughly `75 spawned` instead of the expected `150`

Fix direction:

- subtract only `spawnedFromSource`
- do not burn the spawn quota when placement fails

### Mistake: over-strict recovery / occupancy checks

The stuck recovery path could still reject all movement due to occupancy and return the current position.

Fix direction:

- add a dedicated deadlock release move for agents whose `StuckDuration` exceeds the activation threshold
- permit a short local release step with softer occupancy constraints
- still keep the step walkable and bounded by field regression limits

## What changed in the engine during this pass

### 2026-04-23.4

- Added smoother heatmap accumulation via soft bilinear contribution.
- Increased use of continuous flow direction.
- Improved continuous flow sampling near obstacle/infinite-field samples.

Result:

- heatmap looked smoother
- solver still stalled

### 2026-04-23.5

- Relaxed route regression limits.
- Raised minimum route-limited speed for stuck agents.

Result:

- did not solve the dead tail
- `avg speed` reached `0 m/s`, which clarified the failure mode

### 2026-04-23.6

- Fixed spawn accounting so failed placement does not consume source quota.
- Added `CreateDeadlockReleaseMove`.
- Added more tail diagnostics.

Result:

- canonical test completed:
  - `150 spawned`
  - `150 completed`
  - `0 active`
  - `Stop reason: all agents completed`

## Current technical interpretation

The engine is now past the "does it finish?" blocker for the canonical 100 x 100 m test scene.

Remaining quality issues are visual/behavioral, not completion blockers:

- heatmap still has corridor-like bands and sharp peaks near corners
- trajectories may still be more grid/corridor-shaped than the Flow Grasshopper reference
- presentation output needs a separate report-friendly mode

## Rules for future iterations

1. Always close Rhino before deploy builds.
2. After every deploy, check file timestamps under `%APPDATA%\Grasshopper\Libraries\INDTools`.
3. In Grasshopper, check `Prof -> Engine build` before judging behavior.
4. Do not tune visuals until `spawned == expected`, `active == 0`, and `Stop reason` is acceptable.
5. If `avg speed` is near zero and `active > 0`, inspect movement rejection/recovery, not only speed constants.
6. If `spawned` is lower than expected, inspect source accounting and occupancy at spawn.
7. Treat `Grid` and `Path fields` as already cheap on this scene; focus performance work on `Simulation`.

## Recommended next work

### First: Presentation heatmap

Create a report-oriented heatmap mode that keeps the simulation values honest but renders closer to professional pedestrian-flow references.

Scope:

- kernel or gaussian-like scalar smoothing after accumulation
- obstacle-aware smoothing mask
- peak softening around hard corners
- optional "analysis" vs "presentation" output mode

Status update:

- Implemented on 2026-04-23 as a new `Presentation` input in `Create Crowd Heatmap`.
- The presentation path now applies:
  - additional obstacle-aware smoothing
  - line-of-walkable-cells protection so values do not blur through obstacles
  - soft corner-peak suppression
- Rhino validation confirmed that this mode produces a more report-friendly map while preserving the readable flow structure.
- Legend title now explicitly marks the mode as `(presentation)`.

### Second: smoother steering / corridor flow

Improve trajectory smoothness without reopening the whole solver.

Scope:

- smoother route corridor / flowline guidance
- less cell-neighbor stepping in the final route direction
- preserve deadlock release and spawn accounting fixes from `2026-04-23.6`

### Third: deeper profiling

Add simulation substage profiling:

- spawn
- desired velocity
- candidate selection
- collision / occupancy checks
- recovery / deadlock release
- frame recording

## Related

- [[01 - Current Status]]
- [[03 - Build and Deploy]]
- [[04 - Behavior Model and Solver]]
- [[07 - Iteration Log]]
- [[08 - Open Questions and Next Steps]]
- [[10 - Test Scenes and Validation Reports]]

## Offline library update

The offline install folder was also updated on 2026-04-23:

- `X:\CompDesign_Projects\Library\crowd_flow\plugin_library`

Practical rule:

- treat this folder as the current offline distribution source for the plugin
- it now contains the fresh `net48` plugin set copied from the current INDTools build
- verified by matching hashes for:
  - `Crowd.dll`
  - `INDGrasshopperComponents.gha`

Important note:

> [!warning]
> An attempted backup folder `plugin_library_backup_20260423_1705` was created but remained empty due to an incorrect PowerShell copy pattern. Do not treat it as a valid rollback snapshot.

The valid snapshot of the updated offline package is:

- `X:\CompDesign_Projects\Library\crowd_flow\plugin_library_snapshot_current_20260423_1705`
