# Grasshopper Test Files

This folder contains Rhino and Grasshopper files used to validate the standalone `GhCrowdFlow` plugin.

## Current Structure

```text
tests/
  grasshopper/
    crowd/
      flow-demo/
```

## `crowd/flow-demo`

The current public regression scene lives in `crowd/flow-demo` and includes:

- `Flow.3dm`
- `Flow.gh`
- `CrowdReport_Template.docx`

Use this folder for smoke tests after build and deploy:

1. Build from the repository root with `build.ps1`.
2. Deploy with `build.ps1 -DeployToGrasshopper` while Rhino is closed.
3. Open `Flow.3dm` in Rhino 8.
4. Open `Flow.gh` in Grasshopper.
5. Validate simulation, heatmap, legend, image export, and report export behavior.
