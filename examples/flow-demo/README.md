# Flow Demo

This folder contains the first public test scene for `GhCrowdFlow`.

Files:

- `Flow.3dm` Rhino scene
- `Flow.gh` Grasshopper definition

Recommended order:

1. Build the plugin from the repo root.
2. Deploy with `build.ps1 -DeployToGrasshopper`.
3. Open `Flow.3dm` in Rhino 8.
4. Open `Flow.gh` in Grasshopper.
5. Run the simulation and inspect the trajectories, heatmap, legend, and report export flow.
