---
title: Research Intake - Pedestrian Model Landscape and Direction
tags:
  - project/ghcrowdflow
  - research
  - benchmark
  - solver
  - strategy
---

# Research Intake - Pedestrian Model Landscape and Direction

## Source note

Recovered prior Codex research synthesis provided by the user and preserved as durable project knowledge on 2026-04-18.

## Core conclusion

There is no single universally most accurate pedestrian model for every scenario.
Validation and review literature point in the same direction:
- different models match different regimes well
- a model can match flow while still missing trajectories, density/velocity time series, or spatial-temporal patterns
- high-density crowd behavior usually requires a multi-layer approach rather than one universal formula

Practical takeaway for this project:
- if we want the strongest transparent and implementable locomotion core, the best orientation is `JuPedSim AVM + CSM`
- if we want a strong routing and steering architecture reference, `Pathfinder` is especially useful
- if we want a strong research-grade global navigation plus human spacing reference, `Vadere OSM` is especially useful
- if we want strong commercial product benchmarks, `LEGION` and `MassMotion` are valuable, but they are less useful as algorithm blueprints because much of the internal math is closed

## Source references cited in the original synthesis

General validation and review:
- validation procedure, 2017: https://www.sciencedirect.com/science/article/abs/pii/S1569190X17300849
- dense crowd review, 2024: https://www.sciencedirect.com/science/article/pii/S1569190X24000698

Social Force / commercial tools:
- PTV Viswalk: https://www.ptvgroup.com/en/products/pedestrian-simulation-software-ptv-viswalk
- PTV help: https://cgi.ptvgroup.com/vision-help/VISSIM_2026_EN-DE/en-us/Content/8_VISWALK/Fugae_SimulationvonFu.htm
- AnyLogic Help: https://anylogic.help/9/libraries/pedestrian/index.html
- AnyLogic Pedestrian Library: https://www.anylogic.com/resources/libraries/pedestrian-library/

GCF:
- GCF paper: https://journals.aps.org/pre/abstract/10.1103/PhysRevE.82.046111
- JuPedSim GCF docs: https://www.jupedsim.org/v1.2.1/api/generalized_centrifugal_force/index.html

CSM:
- JuPedSim pedestrian models: https://www.jupedsim.org/stable/pedestrian_models/index.html
- CSM docs/source: https://www.jupedsim.org/stable/_modules/jupedsim/models/collision_free_speed.html
- generalized collision-free model abstract: https://www.sciencedirect.com/science/article/abs/pii/S037843711931444X

AVM:
- AVM abstract: https://www.sciencedirect.com/science/article/pii/S0968090X21004502
- JuPedSim AVM docs/source: https://www.jupedsim.org/stable/_modules/jupedsim/models/anticipation_velocity_model.html

OSM / Vadere:
- Vadere framework: https://www.vadere.org/
- Vadere cite/framework page: https://www.vadere.org/how-to-cite-vadere/
- OSM docs: https://pedestriandynamics.org/models/optimal_steps_model/

Pathfinder:
- Pathfinding: https://www.thunderheadeng.com/docs/2026-1/pathfinder/appendices/technical-reference/pathfinding/
- Steering Mode: https://www.thunderheadeng.com/docs/2026-1/pathfinder/appendices/technical-reference/steering/

LEGION / MassMotion:
- LEGION page: https://www.bentley.com/software/legion/
- LEGION data sheet: https://www.bentley.com/wp-content/uploads/PDS-LEGION-Modeling-Simulation-LTR-EN-LR.pdf
- MassMotion: https://www.oasys-software.com/products/pedestrian-simulation/massmotion/
- MassMotion validation paper: https://www.sciencedirect.com/science/article/pii/S2352146514000520

Empirical trajectory and calibration references mentioned in the synthesis:
- train station dataset, 2024-11-20: https://www.nature.com/articles/s41597-024-04071-9
- ATC / DIAMOR group data: https://dil.atr.jp/ISL/sets/groups/
- ATC dataset: https://dil.atr.jp/crest2010_HRI/ATC_dataset/
- European Squares dataset, 2026-02-10: https://www.nature.com/articles/s41597-026-06686-6
- Lyon dense crowd dataset, 2025-04-30: https://www.nature.com/articles/s41597-025-04732-3

## Engineering comparison preserved from the original synthesis

### 1. Classic Social Force

Strengths:
- continuous movement
- intuitive physical interpretation
- natural obstacle-avoidance starting point

Weaknesses:
- without substantial extensions it often looks too smooth or insufficiently human in anticipatory avoidance, counterflow, and complex geometry
- useful as a baseline, but not the best final target for this plugin

### 2. Generalized Centrifugal Force Model

Strengths:
- stronger than basic Social Force in volume exclusion and corridor calibration
- historically important force-based improvement

Weaknesses:
- still less compelling than newer anticipation-based velocity formulations for the direction we want

### 3. Collision-Free Speed Model

Strengths:
- very strong stable base layer
- adapts speed to available headway and density
- easier to control and calibrate than force-only integration
- attractive for architectural simulation workflows where robustness matters

Weaknesses:
- stronger as a compact collision-free locomotion layer than as a full behavioral architecture by itself

### 4. Anticipation Velocity Model

Preserved assessment from the original synthesis:
- best open-model candidate for our direction
- explicitly models anticipation rather than just repulsion
- separates perception, prediction, and strategy selection
- captures lane formation in bidirectional flow
- validated against uni- and bi-directional fundamental diagrams
- exposes parameters that map well to a Grasshopper agent profile: `time_gap`, `desired_speed`, `radius`, `anticipation_time`, `reaction_time`, `wall_buffer_distance`

Project interpretation:
- AVM is the strongest conceptual reference for the local motion layer we want to evolve toward

### 5. Optimal Steps Model

Strengths:
- often more convincing than force models in narrow passages, obstacle negotiation, and purposeful local path choice
- utility-based step choice is behaviorally attractive for human movement

Weaknesses:
- heavier to implement cleanly in a lightweight solver
- useful as a design inspiration for utility-based local decision making, but not necessarily the first full rewrite target

### 6. Pathfinder steering architecture

Strengths:
- very strong production architecture reference
- useful decomposition: navmesh + A* + string pulling + seek curve + inverse steering with explicit behaviors for walls, occupants, lanes, passing, and cornering

Project interpretation:
- especially valuable not as a single scientific locomotion law, but as the architecture of the local decision layer

### 7. LEGION and MassMotion

Strengths:
- excellent product benchmarks for workflow, route choice, reporting, and operational usefulness
- strong practical references for least-effort, macro-navigation, and scenario-level behavior

Weaknesses:
- limited value as direct algorithm truth because the internal formulations are much less transparent than JuPedSim, Vadere, or Pathfinder documentation

## Preserved engineering ranking

For an open, implementable, well-justified direction for this codebase, the prior synthesis ranked priorities as:

1. `AVM (JuPedSim)`
2. `CSM (JuPedSim)`
3. `OSM (Vadere)`
4. `Pathfinder-style steering`
5. `Classic Social Force / GCF`
6. `LEGION / MassMotion`

## Preserved assessment of the current GhCrowdFlow codebase

The previous synthesis concluded that the current solver is already conceptually a hybrid system rather than a toy:
- `grid + distance field routing`
- `walkable grid + boundary repulsion`
- `continuous local steering` with anticipation-like elements, wall repulsion, time-to-collision, lane bias, turn smoothing, start scatter, and final approach
- `congestion-aware exit choice + commitment + reassessment`
- profile-level parameters for separation, density weighting, wall avoidance, turn anticipation, exit choice randomness, congestion sensitivity, and exit commitment

Project interpretation:
- the existing architecture is directionally correct
- the next step is evolutionary refinement, not throwing everything away and copying one external model wholesale

## Preserved gap analysis

The prior synthesis identified these main gaps between the current solver and stronger modern references:
- current path field was still closer to discrete grid routing than geodesic/eikonal-quality guidance
- local motion remained heuristic rather than explicitly formalized as a full AVM or CSM-style model
- important human-motion parameters should be treated as first-class profile parameters instead of remaining buried in constants
- exit choice should move closer to predicted travel-time utility with queue and inertia effects
- no calibration loop yet ties parameters to empirical trajectory datasets

## Preserved recommended direction for GhCrowdFlow v2

The prior synthesis recommended this overall direction:
- not a wholesale copy of `LEGION`
- not a return to pure `Social Force`
- not an immediate full rewrite into `OSM`

Recommended hybrid direction:
- `Vadere/OSM-inspired global field`
- `JuPedSim AVM-inspired local motion`
- `Pathfinder-style candidate steering and route utility`
- `LEGION-style congestion-aware exit choice and reporting`

Why this direction was preferred:
- realism
- explainability
- computational stability
- architectural clarity
- good fit for a Grasshopper plugin product

## Preserved implementation recommendations

The prior synthesis recommended the following concrete next steps:

1. Keep the existing high-level architecture:
- global path field
- local steering
- exit reassessment
- heatmap and outputs

2. Improve the routing layer:
- replace discrete distance field with weighted Dijkstra / Fast Marching or a more geodesic / eikonal-like field
- include obstacle proximity cost

3. Formalize the agent profile around AVM/CSM-like parameters:
- `TimeGap`
- `AnticipationTime`
- `ReactionTime`
- `WallBufferDistance`
- `NeighborRepulsionStrength`
- `NeighborRepulsionRange`
- `PreferredSideBias`
- `ComfortDistance`

4. Keep candidate-based steering, but move it toward explicit utility semantics:
- route-direction cost
- predicted local density cost
- time-to-collision cost
- wall-clearance cost
- turn-effort cost
- lane-coherence cost
- exit-utility bias

5. Improve exit choice into travel-time utility:
- predicted travel time
- predicted queue time
- switch penalty
- commitment penalty
- reevaluation with inertia and optionally softmax choice

6. Add an empirical calibration loop using real datasets:
- speed-density curves
- interpersonal spacing and group behavior
- long-duration indoor trajectories
- open-space path choices
- dense-flow phenomena

7. Add richer output metrics:
- wall clearance
- conflict intensity
- queueing map
- lane coherence
- travel time by exit

## Current project relevance

This note matters because it provides a preserved strategic answer to the question:
- what external model family should GhCrowdFlow move toward without losing its current architectural strengths?

Short answer:
- evolve the existing solver toward an `AVM-inspired hybrid` with stronger routing smoothness, clearer candidate-utility semantics, and more empirical calibration

## Links

- [[04 - Behavior Model and Solver]]
- [[05 - Research Benchmarks]]
- [[07 - Iteration Log]]
- [[08 - Open Questions and Next Steps]]

## 2026-04-20 targeted external comparison refresh

Context:
- Per user request, prior preserved research was re-checked against current primary and official sources rather than broad secondary summaries.
- Goal was not to restart model selection from zero, but to test whether the current project direction is still aligned with strong open and commercial references.

### Main refresh conclusion

The previously preserved strategic direction still looks correct:
- `JuPedSim AVM/CSM` remains the strongest open reference for the local locomotion layer
- `Vadere OSM` remains a strong reference for utility-based local step choice and obstacle-respectful routing fields
- `Pathfinder` remains one of the clearest production references for hierarchical route choice plus candidate-based steering
- `LEGION` and `MassMotion` remain strongest as product benchmarks for scenario workflow, reporting, and operational usefulness rather than transparent algorithm blueprints
- `Viswalk` still represents the mature commercial force-model lineage, but it does not by itself suggest that GhCrowdFlow should revert to a pure Social Force architecture

### Fresh source-aligned takeaways

#### JuPedSim

Current official JuPedSim documentation still presents:
- `Collision Free Speed Model` as a computationally efficient collision-free locomotion base
- `Anticipation Velocity Model` as an anticipatory extension built on top of CSM
- per-agent parameterization for AVM neighbor and wall parameters
- wall handling based on smooth directional adjustment and wall gliding rather than crude force bouncing

Practical relevance for GhCrowdFlow:
- this strongly supports keeping a stable collision-free / anticipatory local layer
- our current local steering is directionally compatible, but it is still more heuristic and less formally parameterized than the AVM family

#### Vadere / OSM

Current Vadere / PedestrianDynamics descriptions still frame OSM as:
- utility or potential minimization over candidate step positions
- target attraction combined with obstacle and pedestrian repulsion
- geodesic distance to target as part of the utility field

The same documentation also explicitly notes a key limitation:
- solving many local optimization problems is computationally expensive
- real-time simulation with thousands of agents is not yet the easy/default path for strict OSM-style stepping

Practical relevance for GhCrowdFlow:
- OSM still supports our desire for more intelligent local constrained decisions
- but it also reinforces that a full OSM rewrite would likely be too heavy for the lightweight responsive plugin direction
- this supports using OSM as design inspiration for candidate utility semantics, not as a wholesale replacement

#### Pathfinder

Current Pathfinder 2026.1 documentation still shows the clearest production decomposition:
- hierarchical path planning
- local door / target choice using travel-time, queue-time, and commitment-style costs
- `A*` on a triangulated navigation mesh
- path smoothing via string pulling
- spline-like seek curves for smoother following
- inverse steering over discrete candidate directions with weighted behaviors such as seek, avoid walls, avoid occupants, lanes, pass, and cornering
- backtrack prevention and periodic route re-evaluation

Practical relevance for GhCrowdFlow:
- this remains extremely close to the architectural lane we want
- our project already shares several ideas:
  - candidate steering
  - congestion-aware exit choice
  - commitment and reassessment
  - corner / wall / lane effects
- the biggest remaining Pathfinder-style gap is not philosophy but explicit structure:
  - clearer route utility terms
  - smoother path representation than a plain grid ridge
  - more explicit local state / cost composition

#### Viswalk

Current PTV Viswalk documentation still describes pedestrian motion as:
- based on the Social Force Model lineage
- validated against both macroscopic and microscopic phenomena
- structured across strategic, tactical, and operational levels

Practical relevance for GhCrowdFlow:
- this confirms that force-based models remain credible in commercial practice
- but it does not overturn the earlier conclusion that pure force models are not the cleanest end-state for our plugin goals
- especially for explainability, controllability, and local constrained geometry behavior, AVM/CSM + candidate utility still appears more attractive

#### AnyLogic

Current AnyLogic Pedestrian Library documentation emphasizes:
- continuous-space motion in a physical environment
- environment + behavior composition
- strong workflow around queues, services, attractors, markup, density maps, and scenario testing

Practical relevance for GhCrowdFlow:
- AnyLogic remains a valuable product benchmark for integration, experimentation, and scenario logic
- it is less useful than JuPedSim / Vadere / Pathfinder as a transparent locomotion reference

#### LEGION and MassMotion

Current official product pages still emphasize:
- design-stage and operations-stage scenario testing
- reporting, hotspot analysis, throughput / safety assessment, and stakeholder communication
- complex venue modeling and high practical utility

Practical relevance for GhCrowdFlow:
- both remain important product benchmarks
- they reinforce the need for strong outputs and reporting, not just prettier trajectories
- they do not provide enough transparent algorithm detail to replace open technical references for core locomotion design

### Updated comparison verdict for GhCrowdFlow

#### Where current GhCrowdFlow is aligned correctly

- hybrid architecture instead of one monolithic pedestrian law
- congestion-aware exit utility rather than pure nearest-exit routing
- candidate-based local steering rather than simple force summation only
- attention to wall interaction, lane behavior, turn anticipation, and local variability
- growing emphasis on heatmaps, legend clarity, and interpretable outputs

#### Where GhCrowdFlow still lags the stronger references

1. Routing smoothness and representation
- stronger engines use navmesh/geodesic/string-pulled or otherwise smoother route representations than a raw grid ridge

2. Explicit utility semantics
- our candidate scoring is still partly heuristic and less transparently decomposed than Pathfinder or OSM-style formulations

3. Formal local motion parameterization
- JuPedSim AVM/CSM expose clearer first-class parameters for anticipation, wall buffer, reaction, and spacing behavior than the current GhCrowdFlow profile surface

4. Validation and calibration loop
- literature and products still point to calibration and multi-metric validation as essential
- we still rely mostly on visual scene review rather than a stronger empirical calibration loop

5. Product workflow maturity
- MassMotion / LEGION / AnyLogic still outperform us in scenario workflow, reporting richness, and operational analysis packaging

### Updated strategic recommendation

No major strategic reversal is justified by the refreshed comparison.

Most defensible direction remains:
- keep the current hybrid architecture
- evolve routing toward smoother geodesic / navmesh-like or weighted-field guidance
- evolve local steering toward more explicit `AVM/CSM + Pathfinder-style utility` semantics
- continue LEGION / MassMotion-inspired reporting and scenario outputs
- avoid both extremes:
  - pure Social Force rollback
  - full OSM rewrite as the immediate next step

### Primary sources checked in this refresh

- JuPedSim pedestrian models:
  - https://www.jupedsim.org/stable/pedestrian_models/index.html
- Vadere framework:
  - https://www.vadere.org/
- Optimal Steps Model:
  - https://pedestriandynamics.org/models/optimal_steps_model/
- Pathfinder technical references:
  - https://www.thunderheadeng.com/docs/2026-1/pathfinder/appendices/technical-reference/pathfinding/
  - https://www.thunderheadeng.com/docs/2026-1/pathfinder/appendices/technical-reference/steering/
- PTV Viswalk / Vissim pedestrian simulation:
  - https://cgi.ptvgroup.com/vision-help/VISSIM_2026_EN-DE/en-us/Content/8_VISWALK/Fugae_SimulationvonFu.htm
- AnyLogic Pedestrian Library:
  - https://anylogic.help/9/libraries/pedestrian/index.html
- Oasys MassMotion:
  - https://www.oasys-software.com/solutions/pedestrian-simulation/
- Bentley LEGION:
  - https://www.bentley.com/software/legion/
- Validation / review references:
  - https://www.sciencedirect.com/science/article/abs/pii/S1569190X17300849
  - https://www.sciencedirect.com/science/article/pii/S1569190X24000698

## 2026-04-20 empirical trajectory datasets refresh

Context:
- User requested a return to real tracked pedestrian movement examples and a comparison against current GhCrowdFlow outputs.
- This pass focused on field and ecological datasets rather than only synthetic benchmarks or engine documentation.

### Main empirical sources reviewed

#### 1. Train-station mobility dataset (Scientific Data, 2024)

Source:
- https://www.nature.com/articles/s41597-024-04071-9

What it contains:
- around 24.8 million anonymized pedestrian trajectories from two Czech train stations
- 24/7 field recording from 2022-09-05 to 2023-08-04
- real-world coordinates, speeds, passage-time comparisons, and large-scale station hall movement

Why it matters:
- strong field reference for ordinary station walking, waiting, ticketing, and corridor passage
- useful for speed distributions, waiting patterns, and corridor-scale route smoothness

Important note from the paper:
- the data is large and operationally realistic, but automatic tracking still suffers from occlusions, missed detections, and ID errors around clutter and boundaries
- this matters when using the dataset for calibration: even real-world data needs careful filtering, not blind fitting

#### 2. European public squares dataset (Scientific Data, 2026)

Source:
- https://www.nature.com/articles/s41597-026-06686-6

What it contains:
- 39 European squares
- 193 hours of video footage
- about 348k cleaned pedestrian trajectories
- weekday/weekend and seasonal variation
- webcam-based tracking with georeferencing and technical validation

Why it matters:
- strong field reference for open public-space movement
- useful for dominant route families, path spread in open areas, attraction to urban features, and low-to-moderate density route diversity

Important note from the paper:
- moving pedestrians are tracked much more reliably than seated or standing ones
- complete trajectories are far more robust for moving walkers than for stationary users of the space

#### 3. Lyon Festival of Lights dense crowd dataset (Scientific Data, 2025)

Source:
- https://www.nature.com/articles/s41597-025-04732-3

What it contains:
- multiscale field dataset from a dense real event crowd
- broad overview flow records plus around 7000 microscopic trajectories
- densities up to 4 ped/m2
- additional contact/push statistics and qualitative rare-event observations

Why it matters:
- strong field reference for dense merging, opposing flows, and real high-pressure crowd motion
- useful for deciding what low-density GhCrowdFlow scenes should and should not attempt to reproduce

Important note from the paper:
- dense crowd datasets differ mechanically from classical ETH/UCY-style low-density avoidance scenes
- the paper explicitly contrasts dense real-event regimes with low-density surveillance datasets around 0.1-0.5 ped/m2

#### 4. DIAMOR / ATC ecological tracking datasets

Sources:
- https://dil.atr.jp/ISL/sets/groups/
- https://dil.atr.jp/crest2010_HRI/ATC_dataset/

What they contain:
- real pedestrian tracking in Osaka corridors / public indoor spaces
- per-person position, velocity, angle of motion, and facing angle
- manually annotated groups and socially interacting partners

Why they matter:
- strong ecological reference for social groups, dyads, and group-aware collision avoidance
- especially valuable because they move beyond “independent particles”

Related findings from linked papers:
- dyads and larger groups change avoidance behavior
- socially interacting groups are less responsive in collision avoidance
- singles often shoulder more of the avoidance burden
- three-person groups often form stable V-shaped formations
- group width shrinks with increasing density, but the social configuration does not disappear instantly

Supporting sources:
- https://pubmed.ncbi.nlm.nih.gov/24580285/
- https://pubmed.ncbi.nlm.nih.gov/26172757/
- https://www.sciencedirect.com/science/article/pii/S1369847825000373

### Empirical patterns that are most relevant for GhCrowdFlow

#### Real tracked pedestrians usually show smooth commitment, not tiny local recirculation

Across station, square, and ecological corridor datasets, ordinary walking trajectories generally:
- bend smoothly around obstacles and other pedestrians
- fan out and re-merge
- adapt locally to other people
- occasionally hesitate or queue

But they do not typically show repeated tight micro-loops or orbiting in a small patch of space during ordinary uncongested navigation.

Implication for GhCrowdFlow:
- the remaining local scribble seen in our screenshots is not a desirable form of realism
- it should be treated as solver indecision, not human-like variation

#### Real route diversity exists, but it is structured

In open and semi-open field datasets:
- pedestrians do not all follow one identical centerline
- however, they also do not scatter arbitrarily
- route families are corridor-like and shaped by geometry, visibility, and social context

Implication for GhCrowdFlow:
- our recent progress toward dominant path families plus secondary alternatives is aligned with reality
- but rare wide detour outliers in current scenes still look less empirical and more numerical

#### Dense crowds are a different regime

The Lyon paper makes clear:
- low-density avoidance and dense crowd motion should not be treated as one phenomenon
- at higher density, collective compression, contacts, and flow constraints matter much more

Implication for GhCrowdFlow:
- current low-agent Rhino scenes should be compared mainly to low-to-moderate density field movement, not to extreme dense-crowd footage
- this supports keeping the current solver target focused on smooth architectural circulation before trying to emulate crowd-pressure phenomena

#### Social groups materially change trajectories

The DIAMOR/ATC ecosystem shows:
- groups preserve social formations
- dyads and triads alter avoidance and spacing
- collision avoidance is not symmetric when one side is a socially interacting group

Implication for GhCrowdFlow:
- our current solver still behaves more like independent walkers with soft local coupling
- this is acceptable for a first public release, but it means empirical realism is incomplete wherever group behavior matters

#### Empirical calibration should use multiple metrics

The validation literature and dataset papers reinforce:
- one metric is insufficient
- flow, density, speed, spacing, path shape, and group behavior may disagree

Implication for GhCrowdFlow:
- screenshot realism alone is helpful but insufficient
- future calibration should explicitly track:
  - path smoothness / curvature
  - wall clearance
  - bottleneck dwell behavior
  - lateral spread before merges
  - speed distributions
  - exit split ratios

### Practical comparison with current GhCrowdFlow results

#### Where current results are empirically plausible

- corridor-scale route families are now often believable
- obstacle-respecting macro-guidance is visibly better than before
- not everyone collapses immediately onto one perfect mathematical line
- long-range trajectories in several scenes now resemble ordinary low-to-moderate density circulation more closely

#### Where current results still diverge from real tracked behavior

1. Local knotting is still too synthetic
- real tracked walkers may slow, queue, sidestep, or compress
- they much less often perform repeated tiny orbiting or scribble-like re-entry in the same pocket

2. Pinch-point commitment is still too weak in some scenes
- field data suggests people usually commit into an available outgoing corridor once it is evident
- our remaining sticky merge clouds are still too indecisive

3. Social realism is still under-modeled
- real datasets show group formation, facing-angle effects, and asymmetric avoidance burden
- our current solver does not yet reproduce that layer

4. Low-density open scenes are still a better target than dense-event realism
- our current validation scenes are more comparable to ordinary station / square movement than to Lyon-scale dense pressure
- this means we should judge them mainly on smoothness, plausible spread, and stable commitment

### Updated empirical verdict

Empirical tracking data does not invalidate the current project direction.
Instead, it sharpens the next realism criteria:

- keep the recent gains in corridor readability
- remove local recirculation and scribble, because those do not read as ordinary empirical walking
- preserve structured route families rather than universal centerline collapse
- avoid overfitting low-density scenes to dense-crowd mechanics
- treat group behavior as a later but important realism layer, not as a solved problem
