---
title: Open Questions and Next Steps
tags:
  - project/ghcrowdflow
  - type/next-steps
  - status/active
---

# Open Questions and Next Steps

## Open questions

### Source of truth

- Should active development continue in `GhCrowdFlow-release`?
- Or should `workfiles/INDToolsUpdate` become the main development base again?

### Behavior quality

- Are current trajectories sufficiently diverse after latest changes?
- Is exit-choice randomness now visibly working in real scenes?
- Are we still overconstraining people into one dominant lane?

### Performance

- After field builder and spatial index improvements, what is the true remaining hotspot?
- Is heatmap generation a meaningful part of the slowdown on user scenes?
- Do we need profiling per stage: field build / simulate / heatmap / deploy?

### Build reliability

- Should there be a dedicated clean-and-deploy script for Rhino-off workflow?
- Should obsolete `GhCrowdFlow.*` artifacts be removed or fully renamed in project outputs?

## Recommended next steps

1. Decide the real development base.
2. Close Rhino and perform one clean build/deploy cycle.
3. Re-run the user’s canonical test scenes.
4. Record:
   - runtime
   - screenshots
   - whether exits diversify
   - whether trajectories look alive but stable
5. Add lightweight profiling to solver stages if runtime is still poor.
6. Tune only after build/deploy certainty is restored.

## Suggested future notes

- `09 - Test Scenes`
- `10 - Profiling Notes`
- `11 - Parameter Presets`
- `12 - Release Checklist`

## Decision heuristic

> [!tip]
> Если есть сомнение, сначала чинить reproducibility, потом скорость, потом поведение. Иначе легко лечить не ту проблему.

## Links

- [[01 - Current Status]]
- [[03 - Build and Deploy]]
- [[04 - Behavior Model and Solver]]
- [[05 - Research Benchmarks]]
- [[07 - Iteration Log]]

