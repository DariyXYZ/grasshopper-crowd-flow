---
title: Build and Deploy
tags:
  - project/ghcrowdflow
  - type/build
  - type/deploy
---

# Build and Deploy

## Goal

Нужное поведение для пользователя:

- нажать build
- получить новую сборку в Grasshopper libraries
- открыть Rhino / Grasshopper
- увидеть именно новый solver, а не старый dll/gha

## Current paths

- repo:
  - `C:\VS Code\GhCrowdFlow-release`
- grasshopper libraries:
  - `%APPDATA%\Grasshopper\Libraries\INDTools`
- old working IND source base:
  - `C:\VS Code\workfiles\INDToolsUpdate`

## Important facts

> [!warning]
> Если Rhino открыт, он может держать lock на `Crowd.dll`, `Crowd.pdb`, `INDGrasshopperComponents.gha` и связанных файлах. Тогда build может формально пройти частично, но реальные runtime-файлы не будут обновлены.

## Already implemented in this branch

- auto-deploy root через `Directory.Build.props`
- deploy target для `Crowd.dll`
- `.gha` artifact generation в Grasshopper project
- ориентация на `INDGrasshopperComponents.gha`

## Confirmed problems

- Rhino file locks during build
- stale artifacts from old `GhCrowdFlow.*` naming can interfere after assembly renaming
- transient temp-file access issues inside `artifacts\obj`
- build verification without Rhino should prefer `--no-restore` where possible after initial restore

## Safe build protocol

1. Полностью закрыть Rhino и Grasshopper.
2. Убедиться, что старые процессы не держат `Crowd.dll` / `.gha`.
3. Собрать решение.
4. Проверить timestamps в `%APPDATA%\Grasshopper\Libraries\INDTools\...`.
5. Только потом открыть Rhino и тестировать.

## Known build statuses

- `net48` critical, because Grasshopper compatibility matters most there
- `net7.0` and `net8.0` useful for packaging/runtime validation

## Build gotchas

- Access denied в `INDTools\...` почти всегда означает lock от Rhino.
- Access denied в `artifacts\obj` или `artifacts\bin` может означать:
  - stale build artifacts
  - conflict between old and renamed assembly outputs
  - temp-file restore collision

## Recommended future improvement

- завести отдельный deterministic build script for:
  - clean repo-local artifacts
  - build net48 first
  - deploy only when Rhino is confirmed closed
  - log exact deployed files and timestamps

## Связанные заметки

- [[01 - Current Status]]
- [[06 - Errors and Debugging]]
- [[07 - Iteration Log]]

## 2026-04-18 practical update

- `build.ps1` is now the preferred deterministic entry point for local verification.
- Default usage should verify `net48` without deploy:
  - `.\build.ps1`
- Explicit deploy should be opt-in and only run with Rhino closed:
  - `.\build.ps1 -DeployToGrasshopper`
- Full multi-target verification:
  - `.\build.ps1 -AllFrameworks`
- Direct solution-level builds can still hit transient `artifacts\obj\*.tmp` access conflicts, so they should not be treated as the canonical verification path for this branch.
