---
title: Architecture
tags:
  - project/ghcrowdflow
  - type/architecture
---

# Architecture

## High-level structure

Проект состоит из двух основных слоев:

- `src/Crowd`
  - доменная модель
  - path field / grid utilities
  - simulation service
  - heatmap service
- `src/GrasshopperComponents`
  - компоненты Grasshopper
  - адаптер между GH UI и `Crowd` domain layer
  - сборка `.gha`

## Core domain objects

- `CrowdFloor`
- `CrowdObstacle`
- `CrowdSource`
- `CrowdExit`
- `CrowdAgentProfile`
- `CrowdModel`
- `CrowdAgentState`
- `CrowdSimulationResult`

## Solver pipeline

Общий проход выглядит так:

1. Создается `CrowdGrid` из пола и препятствий.
2. Для каждого выхода строится distance / path field.
3. По источникам спавнятся агенты.
4. На каждом simulation step агенты:
   - при необходимости переоценивают exit
   - получают desired velocity
   - проходят локальное avoidance / smoothing / recovery
   - обновляют позицию и trajectory
   - поглощаются выходом при достижении условий финиша
5. По frames и trajectories строится heatmap / downstream visualization.

## Important technical notes

- Глобальная навигация не должна сводиться к “один shortest path на всех”.
- Локальная динамика должна включать:
  - density response
  - collision avoidance
  - wall avoidance
  - route smoothing
  - stochastic individuality
  - exit choice under congestion
- Grasshopper layer не должен ломать backward compatibility по сигнатурам и именам сборок.

## Architectural pain points

- Публичный standalone-repo потерял связь с IND ecosystem.
- Build/deploy and runtime loading тесно завязаны на Rhino lock behavior.
- Поведенческие константы сейчас слишком чувствительны: даже “безопасный” твик может резко убить живость траекторий.

## Related code

- `src/Crowd/Services/CrowdSimulationService.cs`
- `src/Crowd/Utilities/CrowdGrid.cs`
- `src/Crowd/Utilities/CrowdPathFieldBuilder.cs`
- `src/GrasshopperComponents/GrasshopperComponents.csproj`

## Связанные заметки

- [[03 - Build and Deploy]]
- [[04 - Behavior Model and Solver]]
- [[06 - Errors and Debugging]]

