---
title: Test Scenes and Validation Reports
tags:
  - project/ghcrowdflow
  - type/test
  - type/validation
  - regression
---

# Test Scenes and Validation Reports

## Purpose

This note accumulates scene-by-scene validation results for GhCrowdFlow so product evolution can be tracked over time.
Use it to answer:
- what changed visually
- what improved
- what regressed
- which code iteration likely caused the change
- whether a later rollback or re-merge is justified

## 2026-04-18 visual regression report

### Source

- User supplied a new batch of Rhino / Grasshopper screenshots after the latest build.
- Exact image files were not provided as local paths, so this entry preserves the visual analysis and conclusions from the screenshots shared in chat.

### Overall verdict

The latest iteration appears noticeably more realistic than the immediately previous state.
This is not a tiny cosmetic change — the screenshots suggest a meaningful directional improvement in route quality and human-like spread.

### Main improvements observed

1. Obstacle hugging looks reduced in several scenes.
The strongest route bands now tend to keep a more believable offset from obstacle faces instead of riding directly along object edges for the full path.

2. Long routes look smoother and less grid-like.
In the clearer diagonal and corridor scenes, the main path family reads more like human movement through a preferred corridor and less like a rigid shortest-path raster trace.

3. Approach variation improved.
Several scenes show a more believable fan-in before bottlenecks or turns, with multiple nearby approach lines instead of immediate collapse into one perfectly shared path.

4. Turning quality improved in some bottleneck entries.
Around several corner entries the trajectories bend earlier and look less mechanically angular than before.

5. Some scenes now show a more convincing distinction between a dominant route and secondary route families.
That is closer to real pedestrian behavior in open public-space navigation: not everyone chooses the same line, but the crowd still expresses a preferred corridor.

### Remaining defects still visible

1. Local scribble / micro-loop behavior still appears near decision points.
A few trajectories still show unstable local wandering near bottlenecks, route merges, or just before/after attractor points.
This is better than before, but not yet clean enough for a mature solver.

2. Bottleneck resolution is still too "sticky" in some scenes.
Agents sometimes accumulate too tightly at a neck or turning apex before releasing into the next corridor.
The main corridor may be more realistic now, but the conflict-resolution patch near the pinch point can still look synthetic.

3. Some attractor neighborhoods still over-focus into a compact knot.
Instead of smoothly distributing approach choices over a usable local area, the trajectories sometimes compress into a noisy convergence spot.
That suggests local target absorption and near-target conflict handling still need refinement.

4. A few outlier paths remain too exploratory or unstable.
There are isolated paths with visible wobble or unnecessary detours that still read as solver indecision rather than purposeful human variation.

### Scene-level reading of this batch

#### Scene family A — corridor with right-side neck and lower attractor

Observed pattern:
- route to the lower-right attractor is much more corridor-shaped than in worse earlier iterations
- agents keep a cleaner offset from the tall narrow obstacle
- some local tangling still occurs at the final neck / target cluster

Interpretation:
- global route field looks better
- local near-target conflict handling is still noisy

#### Scene family B — upper-left to rightward / downward diversion scenes

Observed pattern:
- trajectories now show a stronger dominant route plus secondary alternatives
- fan-out and re-merge feel more plausible than the old all-on-one-line behavior
- there is still visible local turbulence near the major decision apex

Interpretation:
- this is a meaningful improvement in realism
- next work should reduce apex turbulence rather than re-open the entire routing architecture

#### Scene family C — L-shaped obstacle scenes with left sources and right destinations

Observed pattern:
- these scenes show some of the clearest progress
- approach lines spread more naturally before the turn
- cornering around the big L-shape feels more human than before
- some traces still show wandering beneath the main corridor or excess local noise

Interpretation:
- obstacle-respecting global guidance is improving
- local variation is now closer to useful heterogeneity, but still needs damping where it becomes scribble rather than believable individuality

#### Scene family D — tight vertical-gate scenes on the right

Observed pattern:
- agents still over-compress in the local gate / pinch area
- route choice outside the pinch is more readable than before
- near the gate, the crowd still forms a noisy conflict knot before stabilizing onto the preferred outflow

Interpretation:
- remaining issue is no longer just "shortest path looks fake"
- the remaining issue is specifically local gate negotiation, merge behavior, and target-area stabilization

### What likely improved this batch

Based on the recent solver changes, the visible gains are consistent with:
- stronger obstacle penalty in the global path field
- less distance-dominated exit / attractor utility
- more route families surviving before final convergence
- cheaper open-space local logic reducing some over-fitted wall behavior

### What still likely needs work next

1. Near-target stabilization
- reduce scribble and knotting near final attractor / exit absorption areas
- introduce cleaner target-zone arrival logic and local deconfliction

2. Bottleneck merge behavior
- improve local negotiation at pinches so agents do not form a noisy conflict cluster before choosing the same corridor

3. Variation filtering
- keep beneficial route diversity
- suppress low-value wandering that looks like numerical indecision rather than human behavior

4. Runtime logging alongside screenshots
- this batch is visually better, but should be paired with explicit runtime numbers next time
- future reports should always store:
  - runtime
  - agent count
  - timestep
  - profile settings
  - screenshot set
  - short verdict

### Regression status

Current judgment for this batch:
- realism: improved
- corridor quality: improved
- obstacle offset behavior: improved
- route diversity: improved
- local bottleneck stability: still problematic
- near-target stability: still problematic
- final solver verdict: promising iteration worth preserving, not a rollback candidate

## Suggested reporting format for future runs

For each future validation run, record:
- date
- build / iteration description
- runtime range
- scene description
- agent count
- notable parameter settings
- screenshots or file paths to screenshots
- what improved
- what regressed
- keep / rollback / refine decision

## Links

- [[04 - Behavior Model and Solver]]
- [[07 - Iteration Log]]
- [[08 - Open Questions and Next Steps]]

## Artifact workflow

Canonical artifact workflow note:
- [[11 - Artifact Storage and Naming]]

Reusable report template:
- [[validation-report-template]]
## 2026-04-18 final validation reading after second stabilization pass

Latest visual batch suggests a meaningful improvement over the previous pass.

Main conclusions:
- the narrow local merge behavior is substantially cleaner than before
- the solver now commits into the main outgoing corridor earlier instead of spending as much time in the pinch on noisy local alternative testing
- target-side route bundles look more stable and less tangled in the stronger scenes
- the remaining defects are now more localized and intermittent rather than scene-wide

What still remains:
- some apex-style knotting still appears near sharp corner entries or local route redirections
- a few long outlier branches still survive and read as low-value late divergence rather than purposeful route diversity
- one or two lower-corner scenes still show residual wobble before agents fully settle into the exit corridor

Overall judgment for this iteration:
- realism: improved
- bottleneck merge behavior: clearly improved
- target absorption / stabilization: improved
- useless scribble: reduced, but not fully eliminated
- rollback status: not a rollback candidate
- practical status: good evolutionary step; remaining work is now local apex/outlier cleanup rather than broad solver rework

Code direction taken for this pass:
- stronger local commitment in bottleneck / target / conflict zones
- explicit penalty for low-progress candidate steps in constrained regions
- stronger damping of lane-bias, random spread, noise, and wander when a sufficiently clear local route already exists
- slightly stronger blending back toward the main navigational direction during conflict-heavy local negotiation

## 2026-04-20 screenshot batch - current solver behavior reading

### Source

- User supplied a larger post-update screenshot batch showing trajectory overlays together with the new flow heatmap legend.
- Exact local file paths were not provided, so this entry preserves the visual interpretation from the screenshots shared in chat.

### Overall verdict

The solver is currently in a mixed state:
- several scenes now produce clean, believable corridor-scale routes with useful dominant path families
- but a recurring subset still collapses into localized knotting, orbiting, or late indecision near apexes, pinch points, and target-adjacent turns

This is no longer a whole-system failure.
The dominant remaining issue appears concentrated in local constrained negotiation rather than in the global route field alone.

### Strong patterns observed

1. Global route readability is often good.
- Many scenes show a clear preferred corridor with smooth long-range guidance.
- The heatmap bands usually align with believable macro-routes instead of noisy raster-like wandering.

2. Local failure is clustered around geometric events.
- The worst behavior repeatedly appears at:
  - concave turns
  - pinch entries
  - near-target cornering
  - obstacle shoulder transitions where one corridor opens into another

3. Failure mode is not just "too much noise".
- The local scribble often looks like unresolved choice competition between a few nearly equivalent micro-routes.
- In several scenes the agent reaches the correct macro-area, then spends too long oscillating, looping, or compressing before committing.

4. Some scenes are already close to acceptable.
- A few routes show only mild spread and good commitment through the whole corridor.
- This suggests the current architecture is directionally right and that broad rollback would be the wrong move.

### Repeating defects in this batch

#### 1. Apex knotting and local orbiting

Observed pattern:
- agents bunch near sharp turns or redirection points and draw tight scribbles before settling
- some paths briefly orbit or re-enter the same local neighborhood before escaping

Interpretation:
- local candidate scoring still leaves too many near-equal sideways or low-progress alternatives alive in constrained geometry
- target / turn commitment is still not strong enough once the solver is already inside the correct local funnel

#### 2. Sticky merge pockets

Observed pattern:
- several scenes show a compact conflict cloud where a stream should simply merge and continue
- the cloud is smaller than in worse earlier iterations, but still visible and repeated

Interpretation:
- merge handling has improved, but conflict damping is still too weak in short high-curvature necks
- the solver may still over-respect local alternatives after the main outgoing lane is already obvious

#### 3. Rare but visible detour branches

Observed pattern:
- some screenshots still include a few long outlier trajectories that wander high or wide around the main corridor
- these outliers are sparse, but visually expensive because they read as solver instability

Interpretation:
- route diversity is still slightly under-filtered in specific geometries
- a small number of agents may be escaping local traps using overly permissive exploration instead of a cleaner constrained recovery

#### 4. Target-side overshoot / late alignment

Observed pattern:
- near final turns into the target, some agents arrive correctly at the approach corridor but still wobble or overshoot before absorption

Interpretation:
- final approach logic is improved versus older batches, but not yet consistently decisive
- exit / attractor absorption likely still needs a tighter "already aligned, stop exploring" regime

### What looks improved relative to older notes

- long-range routing is smoother and more legible
- obstacle hugging is generally less dominant than before
- several corridor scenes show healthy route-family structure instead of universal single-file collapse
- many runs now fail locally instead of globally, which is real progress

### Most likely next tuning direction

The screenshots suggest the next pass should stay narrow and local:

1. strengthen constrained-zone commitment
- especially at apexes, short necks, and target-side turns

2. penalize local recirculation more explicitly
- reject candidates that keep the agent inside the same constrained pocket without meaningful downstream progress

3. tighten "clear winner" collapse in local candidate selection
- when one corridor is already obviously dominant, reduce residual exploration faster

4. keep global routing mostly intact
- current screenshots do not justify reopening the whole routing layer

### Practical decision

Current judgment for this batch:
- realism: improved versus older global-failure states
- route readability: good in many scenes
- local constrained stability: still insufficient
- rollback status: not a rollback candidate
- recommended action: refine local apex / pinch / target commitment logic, not broad solver redesign

## 2026-04-20 empirical comparison note

After comparing the current screenshot batches against real tracked pedestrian datasets from:
- train stations
- public squares
- ecological corridor/group datasets
- dense event crowds

the most important interpretation became sharper:

- the current solver is already much closer to plausible empirical low-to-moderate density route families than earlier versions
- however, the remaining apex / pinch scribble should not be excused as “human variability”
- real tracked walkers usually show smoother commitment, sidestepping, queuing, or compression rather than repeated tight local recirculation

Practical implication:
- future local solver tuning should explicitly treat knotting / orbiting as a non-empirical defect
- preserving structured route spread is still desirable
- preserving tiny local loops is not

## 2026-04-20 validation after constrained-zone anti-recirculation pass

### Source

- User supplied a new screenshot batch specifically after the anti-recirculation / winner-collapse solver pass.
- This batch is interpreted as an A/B-style comparison against the immediately previous local-stability state.

### Overall verdict

The pass produced mixed results and should not be treated as a clean win.

What improved:
- in several scenes the outgoing corridor is taken more decisively
- some previously noisy local knots collapsed into cleaner single-route commitment
- a few diagonals and long corridors now look almost production-clean

What regressed:
- some scenes became too brittle and mode-collapsing
- several cases now show pathological trapping or over-hard commitment inside the wrong local pocket
- one or two scenes exhibit severe route loss / extreme outlier escape instead of merely reduced scribble

Interpretation:
- the anti-recirculation hypothesis was directionally correct
- but the implementation appears too aggressive in a subset of geometries
- we reduced local exploration, but in some scenes we also removed too much ability to recover from a bad local commitment

### Improvements observed

1. Cleaner corridor commitment in some scenes
- several examples show earlier collapse into the correct outgoing stream
- long tails after the main bend are cleaner than in previous batches

2. Reduced fine-grain dithering in successful geometries
- where the correct local winner was obvious, the solver now commits faster
- this is especially visible in some top-entry and long diagonal scenes

### Regressions observed

#### 1. Wrong-pocket hard commitment

Observed pattern:
- some agents now commit strongly inside a local corner or shoulder region instead of escaping it smoothly
- this appears as dense compression against a local inner edge or a vertical wall-adjacent strip

Interpretation:
- the winner-collapse logic is sometimes locking onto a locally best but globally brittle candidate
- the recirculation penalty reduced exploration, but did not sufficiently distinguish “escape forward” from “compress deeper into the same trap”

#### 2. Recovery failure after local mistake

Observed pattern:
- a few scenes show dramatic outlier detours or effectively failed local resolution after an early wrong move

Interpretation:
- the new pass likely suppressed useful fallback exploration too much once a constrained local winner was chosen
- the solver still needs a controlled way to recover from a bad local commitment

#### 3. Mode collapse in some geometries

Observed pattern:
- some route families now collapse too aggressively into a very narrow band or a single unstable cluster

Interpretation:
- local blending temperature is being reduced too strongly in at least some bottleneck / target conditions
- this is helping in clean cases and hurting in ambiguous ones

### Practical reading of the batch

The batch suggests the project should continue in the same local-solver lane, but with a more selective correction:

- keep the idea of penalizing low-progress recirculation
- soften the winner-collapse so it activates only when the winner is both locally dominant and forward-progress-safe
- add a distinction between:
  - “safe decisive commitment”
  - “fragile local minimum near a wall/pocket”

### Updated decision

Current judgment for this pass:
- anti-scribble direction: correct
- implementation strength: too aggressive
- rollback status: do not rollback the whole idea, but refine the local commitment gating
- recommended next action:
  - keep anti-recirculation pressure
  - weaken unconditional winner collapse
  - require stronger forward-progress / escape evidence before collapsing exploration
