# GhCrowdFlow

Grasshopper plugin for crowd movement simulation and architectural heatmap analysis in Rhino 8.

`GhCrowdFlow` is a focused public extraction of the crowd-simulation work from a larger internal toolset. It is designed for early-stage architectural studies where you need:

- believable pedestrian trajectories on a 2D floor
- obstacle and wall avoidance
- congestion-aware exit choice
- heatmap mesh outputs for circulation analysis
- a Grasshopper-native workflow without external simulation software

## Features

- Crowd floor, obstacle, source, exit, and agent-profile components
- Simulation that runs until agents reach exits instead of stopping at one shared duration
- Per-agent behavioral variation for speed, noise, commitment, congestion sensitivity, wall avoidance, and turn anticipation
- Heatmap mesh generation for occupancy and movement intensity analysis
- Minimal modern icon set for all crowd nodes

## Repository Layout

- `src/Crowd/` simulation engine and heatmap services
- `src/GrasshopperComponents/` Grasshopper `.gha` plugin with crowd components
- `examples/` place for sample Grasshopper definitions and showcase files

## Build

Requirements:

- Rhino 8
- .NET SDK 8
- Windows

Build from the repository root:

```powershell
dotnet build .\GhCrowdFlow.sln -c Debug
```

The Grasshopper plugin assembly is produced from:

- `src/GrasshopperComponents/GrasshopperComponents.csproj`

## Current Scope

This public repository currently focuses on crowd simulation only. Broader internal tool categories such as solar, masterplanning, and other IND studio components are intentionally excluded from this repo.

## Development Notes

- The plugin targets `net48`, `net7.0`, and `net8.0`
- Developer-only visibility can be enabled with `GHCROWDFLOW_DEV=1`
- A local developer flag file can also be placed at `%AppData%\\GhCrowdFlow\\dev.flag`

## Status

This repository is being prepared as a standalone public release. The simulation and Grasshopper components are functional, and the next steps are example definitions, packaging polish, and public release workflow.
