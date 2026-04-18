---
title: Errors and Debugging
tags:
  - project/ghcrowdflow
  - type/debug
  - type/errors
---

# Errors and Debugging

## 1. Old build loaded instead of new one

Symptom:

- тесты в Rhino показывали старое поведение, хотя код уже менялся

Root cause:

- в Grasshopper libraries оставались старые dll/gha
- Rhino мог держать lock на runtime files
- не всегда загружалась та сборка, которую только что собрали

## 2. Method not found for `CreateAgentProfile(...)`

Symptom:

- `Method not found: Crowd.Models.CrowdAgentProfile Crowd.Services.CrowdModelService.CreateAgentProfile(...)`

Root cause:

- несовместимость старого `.gha` и нового `Crowd.dll`

Resolution:

- добавлен backward-compatible overload в `CrowdModelService`

## 3. “Build succeeded” but plugin not updated

Symptom:

- VS / dotnet показывал успешную сборку
- Rhino продолжал показывать старое поведение

Root cause:

- файлы в `%APPDATA%\Grasshopper\Libraries\INDTools` не обновлялись из-за lock

## 4. Access denied during deploy

Typical message:

- `MSB3021`
- `MSB3026`
- `MSB3027`

Meaning:

- Rhino 8 holds `Crowd.dll`, `Crowd.pdb`, `INDGrasshopperComponents.gha`, etc.

## 5. Temp-file access denied in `artifacts\obj`

Symptom:

- restore / build occasionally fails with temp-file access denied inside repo artifacts

Likely causes:

- stale build artifacts
- rename conflicts after assembly name changes
- parallel restore/build collisions

## 6. net48 compatibility issue with `PriorityQueue`

Symptom:

- `CS0246: PriorityQueue<,> could not be found`

Root cause:

- `PriorityQueue` is not available in `net48`

Resolution:

- replaced with custom min-heap structure inside `CrowdPathFieldBuilder`

## Debugging rules of thumb

- first verify which file Rhino is actually loading
- do not trust screenshots until build/deploy path is confirmed
- close Rhino before concluding anything about runtime changes
- separate solver bugs from deployment bugs

## Related notes

- [[03 - Build and Deploy]]
- [[07 - Iteration Log]]

