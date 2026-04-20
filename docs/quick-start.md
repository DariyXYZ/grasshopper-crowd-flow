# Quick Start

This guide is for a first-time public user who wants to build `GhCrowdFlow`, deploy it into Grasshopper, and run the included demo files.

## Requirements

- Windows
- Rhino 8
- Grasshopper
- .NET SDK 8

## Repository Contents You Will Use First

- `examples/flow-demo/Flow.gh`
- `examples/flow-demo/Flow.3dm`
- `templates/reporting/CrowdReport_Template.docx`
- `build.ps1`

## 1. Build The Plugin

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

This performs a local build without deploying into Grasshopper.

## 2. Deploy Into Grasshopper

Close Rhino before deploy so `.dll` and `.gha` files are not locked.

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -DeployToGrasshopper
```

The deploy flow targets the `net48` build used by the local Grasshopper plugin path.

## 3. Open The Demo Files

1. Start Rhino 8.
2. Open `examples/flow-demo/Flow.3dm`.
3. Open Grasshopper.
4. Open `examples/flow-demo/Flow.gh`.

If Grasshopper asks to trust or load the local plugin, allow it and reopen the definition if needed.

## 4. First Demo Run

Suggested first pass:

1. Review the floor, obstacles, sources, and exits in the sample definition.
2. Run `Run Crowd Simulation`.
3. Inspect the trajectories.
4. Generate a heatmap with `Create Crowd Heatmap`.
5. Add `Create Crowd Heatmap Legend` if you want a separate readable legend in Rhino.

## 5. Reporting Workflow

The repo includes a report template for the export workflow:

- `templates/reporting/CrowdReport_Template.docx`

Suggested export flow:

1. Export a PNG with `Export Crowd Image`.
2. Feed that PNG together with the simulation result into `Export Crowd Report`.
3. Use the included template path above.
4. If Microsoft Word is available locally, the component can also attempt PDF export.

## Troubleshooting

- If deploy fails, close Rhino and Grasshopper and run the deploy command again.
- If the definition opens but components are missing, confirm the deploy step completed after the latest build.
- If report export fails, confirm Microsoft Word is installed and the template path points to the included DOCX file.
- If image export fails, retry from an active Rhino view and keep the Rhino document open.
