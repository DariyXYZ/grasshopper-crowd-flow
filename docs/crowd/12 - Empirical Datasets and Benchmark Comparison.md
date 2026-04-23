---
title: Empirical Datasets and Benchmark Comparison
tags:
  - project/ghcrowdflow
  - research
  - benchmark
  - datasets
  - validation
---

# Empirical Datasets and Benchmark Comparison

## Purpose

This note consolidates two lines of evidence for GhCrowdFlow:
- comparison with major crowd / pedestrian simulation engines and model families
- comparison with real tracked pedestrian trajectory datasets

It exists so future solver work can be guided by durable evidence instead of repeating broad research from scratch.

## High-level conclusion

Current GhCrowdFlow is directionally on the right track.

The most defensible strategic direction remains:
- `JuPedSim AVM/CSM` for the local locomotion mindset
- `Vadere OSM` for utility-based local decision logic and obstacle-respectful routing
- `Pathfinder` for production-grade architecture of routing + candidate steering + route reassessment
- `LEGION / MassMotion / AnyLogic` as product benchmarks for workflow, reporting, and scenario usefulness

No strong evidence was found that we should:
- revert to a pure Social Force architecture
- rewrite immediately into a full OSM solver

## Engine / model comparison summary

### JuPedSim AVM / CSM

Why it matters:
- best open reference for stable collision-free and anticipatory motion
- clearer first-class parameters for spacing, anticipation, reaction, and wall interaction

What it suggests for GhCrowdFlow:
- keep the hybrid direction
- formalize more of the current heuristic local layer into clearer behavioral parameters

### Vadere / OSM

Why it matters:
- strong reference for utility-based local movement
- emphasizes geodesic target distance plus penalties for obstacles and pedestrians

What it suggests for GhCrowdFlow:
- adopt clearer utility semantics in candidate scoring
- use as inspiration, not as a full immediate rewrite target
- remember that strict OSM-style local optimization is computationally expensive

### Pathfinder

Why it matters:
- clearest production reference for:
  - hierarchical routing
  - local route reassessment
  - travel-time / queue-time style target choice
  - inverse steering over candidate directions
  - explicit behaviors like seek, avoid walls, avoid occupants, lanes, pass, and cornering

What it suggests for GhCrowdFlow:
- our architecture is already close in spirit
- main remaining gaps are:
  - smoother route representation
  - more explicit cost decomposition
  - clearer local state / commitment logic

### Viswalk

Why it matters:
- mature commercial Social Force lineage
- confirms force-based models remain legitimate in practice

What it suggests for GhCrowdFlow:
- pure force models are still credible, but they do not appear to be the best end-state for our plugin goals

### AnyLogic

Why it matters:
- strong scenario logic, queue/service composition, experimentation workflow, and density analysis

What it suggests for GhCrowdFlow:
- useful product workflow benchmark
- less important than JuPedSim / Vadere / Pathfinder as a locomotion blueprint

### LEGION / MassMotion

Why they matter:
- strong benchmarks for real project usefulness:
  - reporting
  - hotspot analysis
  - operational planning
  - stakeholder-ready outputs

What they suggest for GhCrowdFlow:
- reporting and scenario outputs matter almost as much as prettier trajectories

## Real tracked pedestrian datasets reviewed

### Train station dataset (Scientific Data, 2024)

Source:
- https://www.nature.com/articles/s41597-024-04071-9

Why it matters:
- around 24.8 million anonymized trajectories from real train stations
- good reference for ordinary corridor walking, waiting, ticketing, and terminal-scale movement

Main relevance:
- useful for speed distributions, passage behavior, and corridor-scale route smoothness

### European squares dataset (Scientific Data, 2026)

Source:
- https://www.nature.com/articles/s41597-026-06686-6

Why it matters:
- 39 squares, 193 hours, about 348k cleaned trajectories
- strong reference for open public-space route families and route spread

Main relevance:
- useful for low-to-moderate density movement in open or semi-open spaces

### Lyon Festival of Lights dense crowd dataset (Scientific Data, 2025)

Source:
- https://www.nature.com/articles/s41597-025-04732-3

Why it matters:
- real dense crowd field data
- important reminder that dense-crowd mechanics differ from low-density avoidance scenes

Main relevance:
- useful boundary condition for future work
- not the primary benchmark for our current low-agent architectural scenes

### DIAMOR / ATC ecological group datasets

Sources:
- https://dil.atr.jp/ISL/sets/groups/
- https://dil.atr.jp/crest2010_HRI/ATC_dataset/

Why they matter:
- real ecological tracking with velocity, motion angle, facing angle, and group annotations
- strong evidence for group-aware movement and asymmetric avoidance

Supporting papers:
- https://pubmed.ncbi.nlm.nih.gov/24580285/
- https://pubmed.ncbi.nlm.nih.gov/26172757/
- https://www.sciencedirect.com/science/article/pii/S1369847825000373

Main relevance:
- real groups preserve social formations
- dyads and triads change spacing and avoidance behavior
- singles often contribute more to avoidance than socially interacting dyads

## Empirical comparison with current GhCrowdFlow

### What already looks directionally correct

- macro route families are often believable
- long-range corridor guidance is much more readable than earlier states
- obstacle-respecting path bands are stronger
- the solver no longer collapses every scene into a single perfect mathematical centerline

### What still looks non-empirical

#### Local scribble / orbiting

Observed in current validation scenes:
- tight local loops
- tiny re-entries into the same pocket
- repeated indecision near apexes and pinch points

Empirical reading:
- this does not read like ordinary tracked pedestrian behavior in low-to-moderate density field data
- it should be treated as solver indecision, not as desirable “human complexity”

#### Sticky merge pockets

Observed in current validation scenes:
- local conflict clouds survive too long after the outgoing corridor is already obvious

Empirical reading:
- real pedestrians do slow, compress, and queue
- but they usually commit more decisively once the local continuation is clear

#### Rare wide detour outliers

Observed in current validation scenes:
- occasional long paths peel too far away from dominant route families

Empirical reading:
- real route diversity exists, but it is structured
- these outliers still look more numerical than ecological

#### Missing group realism

Observed in current solver:
- agents mostly behave as independent walkers with soft coupling

Empirical reading:
- acceptable for a first public release
- still incomplete relative to DIAMOR / ATC style real-world group behavior

## Practical project verdict

### Strategic verdict

The research does not justify a major strategic reversal.

Keep:
- hybrid architecture
- congestion-aware exit utility
- candidate-based local steering
- obstacle-respectful routing improvements
- reporting/heatmap/product-output work

Avoid:
- pure Social Force rollback
- full OSM rewrite as the immediate next step

### Realism verdict

Current GhCrowdFlow is already closer to real low-to-moderate density route structure than before.

But the next realism target should be explicit:
- preserve structured route spread
- preserve smooth corridor commitment
- remove local recirculation, tiny orbiting, and apex scribble

### Calibration verdict

Future tuning should rely on multiple empirical criteria, not visual impression alone.

Recommended calibration dimensions:
- path smoothness / curvature
- wall clearance
- lateral spread before merges
- bottleneck dwell behavior
- speed distributions
- exit split ratios
- conflict intensity near pinch points

## Source list

- JuPedSim pedestrian models:
  - https://www.jupedsim.org/stable/pedestrian_models/index.html
- Vadere:
  - https://www.vadere.org/
- OSM:
  - https://pedestriandynamics.org/models/optimal_steps_model/
- Pathfinder:
  - https://www.thunderheadeng.com/docs/2026-1/pathfinder/appendices/technical-reference/pathfinding/
  - https://www.thunderheadeng.com/docs/2026-1/pathfinder/appendices/technical-reference/steering/
- PTV Viswalk / Vissim:
  - https://cgi.ptvgroup.com/vision-help/VISSIM_2026_EN-DE/en-us/Content/8_VISWALK/Fugae_SimulationvonFu.htm
- AnyLogic Pedestrian Library:
  - https://anylogic.help/9/libraries/pedestrian/index.html
- Oasys MassMotion:
  - https://www.oasys-software.com/solutions/pedestrian-simulation/
- Bentley LEGION:
  - https://www.bentley.com/software/legion/
- Train station dataset:
  - https://www.nature.com/articles/s41597-024-04071-9
- European squares dataset:
  - https://www.nature.com/articles/s41597-026-06686-6
- Lyon dense crowd dataset:
  - https://www.nature.com/articles/s41597-025-04732-3
- DIAMOR / ATC groups dataset:
  - https://dil.atr.jp/ISL/sets/groups/
- Group dynamics papers:
  - https://pubmed.ncbi.nlm.nih.gov/24580285/
  - https://pubmed.ncbi.nlm.nih.gov/26172757/
  - https://www.sciencedirect.com/science/article/pii/S1369847825000373

## 2026-04-20 heatmap conventions across engines

There is no single universal heatmap convention across all pedestrian engines.
However, common patterns are clear.

### Heatmap types most commonly provided

#### 1. Density map

This is the most universal heatmap type.
Typical unit:
- `ped/m2` or `agents/m2`

Typical calculation:
- count pedestrians around a point, cell, or local neighborhood
- divide by accessible area
- show either current, mean, maximum, or sliding-window density

Official examples:
- AnyLogic density map explicitly reports density in `units/m2`
- PTV Viswalk provides grid-based and area-based density evaluation in `ped/m2`
- Pathfinder internally estimates local occupant density for movement and speed-density logic

#### 2. Speed map

Also very common.
Typical unit:
- `m/s`

Typical calculation:
- average pedestrian speed for agents currently in a cell/area
- or average over a time interval / sliding window

Official examples:
- PTV Viswalk explicitly supports grid-based density and speed evaluation
- many products tie speed maps to density-based LOS or throughput analysis

#### 3. Occupancy / utilization / dwell map

Common in practice, though naming varies.
Typical units:
- person-seconds per cell
- percentage of time occupied
- mean count over time

Typical calculation:
- accumulate person presence over time in cells/areas
- optionally normalize by duration or area

Practical use:
- shows where people spend time, not just where they pass quickly

#### 4. LOS / comfort map

Very common in transport and terminal planning tools.
Typical unit:
- categorical level rather than a raw physical unit

Typical calculation:
- derived from density thresholds
- sometimes informed by speed-density relationships

Official examples:
- PTV Viswalk explicitly links grid-based density/speed evaluation to LOS schemes
- Pathfinder uses SFPE-style speed-density profiles for movement calibration

#### 5. Preferred path / path usage / trajectory density map

Very common in commercial reporting, even if not always named “heatmap”.
Typical unit:
- often unitless intensity or pass counts

Typical calculation:
- accumulate path traces, passages, or local route usage

Official examples:
- LEGION product materials explicitly mention maps for preferred paths and crowd densities
- MassMotion product materials emphasize crowding, usage patterns, and flow analysis

#### 6. Delay / queue / congestion map

Common in operations-oriented products, though formulae differ.
Typical unit:
- seconds of delay
- relative congestion
- queue time / dwell burden

Typical calculation:
- compare actual motion to desired motion
- or accumulate waiting / queue presence in cells or areas

Official examples:
- PTV Viswalk area measurements expose time delay and time gain metrics
- MassMotion product materials emphasize travel times, flow rates, and crowding analysis

### Important convention about “flow”

Across the industry, `flow` is less standardized than `density`.

Two common meanings exist:

1. Cross-section throughput
- unit: `ped/s`
- calculation: pedestrians crossing a gate, line, door, stair, or escalator per second

2. Specific flow
- unit: `ped/(m*s)`
- calculation: density times speed, or throughput normalized by width

Practical takeaway for GhCrowdFlow:
- calling a full-area heatmap simply `Flow` can be ambiguous
- if the value is per-cell traversals over time, a clearer label may be:
  - `Cell Throughput, agents/s`
- if a future map is based on density times speed or width-normalized section flow, it should be named differently

### Product-fit recommendation for GhCrowdFlow UI

For a clear and industry-legible set of heatmaps, the most useful baseline set is:
- `Density, agents/m2`
- `Speed, m/s`
- `Occupancy, agent-s/cell` or `Mean Occupancy`
- `Cell Throughput, agents/s`
- `Congestion, relative`

Longer-term additions if the product matures:
- `LOS`
- `Delay`
- `Preferred Paths`
- `Conflict Intensity`
