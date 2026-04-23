---
title: Metrics and Reporting Specification
tags:
  - project/ghcrowdflow
  - metrics
  - reporting
  - specification
  - architecture
---

# Metrics and Reporting Specification

## Purpose

This note defines a professional baseline metric set for GhCrowdFlow so the plugin can later export architect-facing reports that are clear, defensible, and consistent.

The goal is not to claim formal code-compliance certification by default.
The goal is to provide:
- technically legible metrics
- stable units and naming
- metrics that match expectations from pedestrian simulation tools and architectural analysis workflows

## Reporting philosophy

For architect-facing reports, metrics should be grouped by decision relevance, not by internal implementation.

The most useful groups are:
- movement quality
- density and crowding
- throughput and capacity
- travel time and delays
- route choice and usage
- risk / conflict indicators

## Recommended metric hierarchy

### Tier 1 — Core report metrics

These should become the default visible outputs for most projects.

#### 1. Density

Label:
- `Density`

Unit:
- `agents/m2`

Meaning:
- local pedestrian density in space

Why it matters:
- the most standard and immediately interpretable pedestrian heatmap metric
- useful for crowding, comfort, and safety reading

Recommended outputs:
- mean density
- peak density
- density heatmap

#### 2. Speed

Label:
- `Speed`

Unit:
- `m/s`

Meaning:
- average local pedestrian walking speed

Why it matters:
- helps distinguish free flow from constrained movement
- pairs naturally with density

Recommended outputs:
- mean speed
- minimum speed in critical zones
- speed heatmap

#### 3. Occupancy

Label:
- `Occupancy`

Preferred unit:
- `agent-s/cell`

Optional normalized variant:
- `normalized occupancy`

Meaning:
- how much agent presence accumulates in a location over time

Why it matters:
- shows persistent usage and dwell burden
- different from density because a place can be briefly dense or frequently occupied

Recommended outputs:
- occupancy heatmap
- mean occupancy by zone
- peak occupancy by zone

#### 4. Throughput

Recommended label:
- `Throughput`

Preferred unit:
- `agents/s`

Meaning:
- number of agents passing through a cell, line, gate, stair, or zone per unit time

Important terminology rule:
- avoid calling this simply `Flow` unless the exact meaning is explicitly documented
- for area heatmaps, `Cell Throughput` is clearer than `Flow`

Why it matters:
- capacity and circulation performance
- useful for identifying dominant movement corridors and bottlenecks

Recommended outputs:
- cell throughput heatmap
- gate throughput
- stair/escalator throughput
- exit throughput

#### 5. Travel Time

Label:
- `Travel Time`

Unit:
- `s`

Meaning:
- elapsed time from source to exit or between selected checkpoints

Why it matters:
- one of the most intuitive decision metrics for architects and operators

Recommended outputs:
- mean travel time by source-exit pair
- min / max / percentile travel time
- travel time by selected route family

#### 6. Completion / Clearance Time

Label:
- `Clearance Time`

Unit:
- `s`

Meaning:
- time until all agents in a scenario, zone, or evacuation set complete their movement

Why it matters:
- essential for evacuation or staged egress scenarios

Recommended outputs:
- total scenario clearance time
- clearance time by zone
- last-agent completion time

### Tier 2 — Strong professional analysis metrics

These are highly valuable for serious reports and should be added next after the core set.

#### 7. Queue Length / Queue Time

Labels:
- `Queue Length`
- `Queue Time`

Units:
- `agents`
- `s`

Meaning:
- how many agents accumulate before a bottleneck
- how long they wait in queue-like constrained movement

Why it matters:
- very useful for station, lobby, security, gate, and retail circulation studies

Recommended outputs:
- mean queue time at gate/exit
- peak queue length
- queue accumulation timeline

#### 8. Delay

Label:
- `Delay`

Unit:
- `s`

Meaning:
- difference between actual travel time and unconstrained or reference travel time

Why it matters:
- translates crowd effects into a practical performance metric
- easier for stakeholders to understand than only density plots

Recommended outputs:
- mean delay by agent group
- delay by route
- delay heatmap or zone summary

#### 9. Exit Split / Route Split

Labels:
- `Exit Split`
- `Route Split`

Unit:
- `%` or fraction

Meaning:
- distribution of people across exits or route alternatives

Why it matters:
- shows whether the model is over-concentrating movement
- valuable for evaluating attractor placement, signage logic, or exit choice realism

Recommended outputs:
- percentage by exit
- percentage by route branch
- time-varying split

#### 10. Zone Load

Label:
- `Zone Load`

Units:
- `agents`
- `agents/m2`
- `agent-s`

Meaning:
- summary metrics for selected architectural zones rather than just heatmap cells

Why it matters:
- architects often need room-, concourse-, or corridor-level summaries for decisions

Recommended outputs:
- mean active agents in zone
- peak agents in zone
- peak density in zone
- cumulative occupancy in zone

#### 11. Door / Stairs / Escalator Performance

Labels:
- `Door Throughput`
- `Stair Throughput`
- `Escalator Throughput`

Units:
- `agents/s`
- `agents/min`

Meaning:
- movement performance of discrete circulation elements

Why it matters:
- directly useful in circulation, transit, and egress studies

Recommended outputs:
- mean throughput
- peak throughput
- utilization over time

### Tier 3 — Diagnostic and advanced metrics

These are especially useful for model improvement, QA, and higher-end reports.

#### 12. Wall Clearance

Label:
- `Wall Clearance`

Unit:
- `m`

Meaning:
- distance from trajectory or occupied position to nearest boundary

Why it matters:
- excellent realism diagnostic
- useful for identifying obstacle hugging

Recommended outputs:
- mean wall clearance
- minimum wall clearance in critical zones
- percentile wall clearance

#### 13. Conflict Intensity

Label:
- `Conflict Intensity`

Unit:
- relative index or event count

Meaning:
- frequency or severity of local collision-avoidance events, directional contention, or near-conflict interactions

Why it matters:
- useful for debugging and for identifying uncomfortable or unstable circulation regions

Recommended outputs:
- conflict heatmap
- total conflict count by zone
- peak conflict intensity

#### 14. Route Curvature / Smoothness

Labels:
- `Route Curvature`
- `Route Smoothness`

Units:
- relative index
- `1/m` for curvature if formalized

Meaning:
- how much trajectories bend, wobble, or oscillate

Why it matters:
- powerful realism metric for solver validation
- useful for distinguishing clean routing from local indecision

Recommended outputs:
- mean curvature by route family
- oscillation / scribble score

#### 15. Level of Service

Label:
- `LOS`

Unit:
- category

Meaning:
- comfort or service class derived from density or density+speed thresholds

Why it matters:
- helps communicate performance to non-technical stakeholders

Important caution:
- LOS thresholds must be explicitly documented
- do not present LOS as authoritative unless the threshold basis is clearly stated

#### 16. Specific Flow

Label:
- `Specific Flow`

Unit:
- `agents/(m*s)`

Meaning:
- width-normalized flow rate across a section

Why it matters:
- useful when comparing doors, stairs, or corridors of different widths

Important distinction:
- this is different from cell throughput `agents/s`

## Recommended naming for GhCrowdFlow

To avoid ambiguity, use these preferred names:

- `Density, agents/m2`
- `Speed, m/s`
- `Occupancy, agent-s/cell`
- `Cell Throughput, agents/s`
- `Travel Time, s`
- `Clearance Time, s`
- `Delay, s`
- `Queue Time, s`
- `Queue Length, agents`
- `Exit Split, %`
- `Wall Clearance, m`
- `Conflict Intensity, relative`
- `Congestion, relative`

Avoid these ambiguous labels unless clarified:
- `Flow`
- `Usage`
- `Traffic`
- `Comfort`

## Minimum architect report package

For a strong first professional export, the baseline report should contain:

1. Scenario metadata
- project name
- date
- model version
- solver settings
- agent count
- timestep

2. Executive summary
- total agents
- total finished
- clearance time
- mean travel time
- peak density
- peak queue length
- main bottleneck locations

3. Heatmaps
- density
- speed
- occupancy
- cell throughput
- congestion

4. Element summaries
- exits
- doors
- stairs / ramps
- selected zones

5. Distribution metrics
- travel time percentiles
- exit split
- queue time percentiles

6. Key findings
- where crowding occurs
- where delays occur
- whether route distribution is balanced or over-concentrated
- whether any critical bottlenecks are evident

## Implementation priority for GhCrowdFlow

### Phase A — immediate

- `Density`
- `Speed`
- `Occupancy`
- `Cell Throughput`
- `Travel Time`
- `Clearance Time`
- `Exit Split`

### Phase B — next

- `Queue Length`
- `Queue Time`
- `Delay`
- `Zone Load`
- `Door / Stair Throughput`

### Phase C — advanced

- `Wall Clearance`
- `Conflict Intensity`
- `Route Curvature / Smoothness`
- `LOS`
- `Specific Flow`

## Notes on “official calculations”

For architect-facing reporting, these metrics can absolutely support formal project documentation.

But wording matters:
- say `simulation-based performance metrics`
- say `reported under the stated model assumptions`
- say `for design evaluation and comparison`

Avoid implying:
- statutory compliance certification by default
- universal regulatory acceptance without project-specific validation

The safest framing is:
- professional simulation outputs suitable for architectural analysis, option comparison, and reporting
- with explicit assumptions, units, and scenario definitions
