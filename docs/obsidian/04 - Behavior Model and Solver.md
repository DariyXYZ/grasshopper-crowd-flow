---
title: Behavior Model and Solver
tags:
  - project/ghcrowdflow
  - type/solver
  - type/behavior
---

# Behavior Model and Solver

## Core problem

Последние итерации показали две крайности:

- слишком живо: вихри, локальные петли, нестабильность
- слишком просто: картонные shortest-path trajectories, почти без индивидуальности

Цель не в том, чтобы убрать сложность, а в том, чтобы удержать живость без паразитных завихрений.

## Desired qualities

- плавные линии
- естественная вариативность людей
- разные траектории при одинаковой сцене
- не все выбирают один и тот же выход
- устойчивость в узких местах
- приемлемое время расчета

## What mechanisms exist in solver

- path field based global guidance
- local candidate point selection
- density avoidance
- wall repulsion / wall following
- flow following
- stuck escape
- exit choice randomness
- congestion-aware exit utility
- turn anticipation
- start scatter / wander
- lane commitment / side bias

## Why quality degraded

Основные причины деградации были такие:

- defaut parameters стали слишком осторожными
- randomness и variation были слишком зажаты
- exit switching logic была слишком консервативной
- route following начал доминировать над индивидуальной микродинамикой
- часть скорости уходила на бесполезно дорогие вычисления, а не на поведение

## What has been improved

- кэш расстояний до стен
- spatial index по агентам
- ускорение path field builder через heap
- более живые profile defaults
- возврат более сильной stochastic individuality
- снижение route over-dominance

## Key tuning principles

> [!note]
> Реалистичность рождается не от одной константы, а от баланса трех уровней: глобальный маршрут, локальные взаимодействия, индивидуальная вариативность.

### Global layer

- route field
- corridor visibility
- exit utility

### Local layer

- collision avoidance
- density and spacing
- wall interaction

### Individual layer

- variation percent
- steering noise
- spawn jitter
- side bias
- route commitment
- exit randomness

## Anti-patterns to avoid

- “все идут по одному идеальному пути”
- “шум убрали почти в ноль”
- “switch exit почти невозможен”
- “каждый дорогой локальный расчет делается по всем агентам”
- “фикс маршрута маскирует старую сборку, а не решает проблему”

## Related notes

- [[05 - Research Benchmarks]]
- [[07 - Iteration Log]]
- [[08 - Open Questions and Next Steps]]

