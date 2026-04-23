---
title: Research Benchmarks
tags:
  - project/ghcrowdflow
  - type/research
  - type/benchmark
---

# Research Benchmarks

## Purpose

Эта заметка фиксирует внешние ориентиры, на которые стоит смотреть, чтобы не сваливаться ни в “игрушечный shortest path”, ни в неуправляемый хаос.

## 1. JuPedSim

URL:

- [JuPedSim Routing Docs](https://www.jupedsim.org/stable/concepts/routing.html)

Key takeaways:

- разделяет route planning и wayfinding
- допускает decision logic по stages / transitions
- указывает, что routing не сводится к одному target point
- учитывает queue-like behavior и multi-stage journeys

Что полезно для нас:

- не вести всех к одной точке одинаково
- поддерживать более богатую exit/route decision logic
- думать о сцене как о route network, а не только как о distance field

## 2. ORCA / RVO2

URL:

- [ORCA official](https://gamma-web.iacs.umd.edu/ORCA/)

Key takeaways:

- smooth collision-free local avoidance
- эффективные локальные взаимодействия
- естественное место для parallel computation

Что полезно для нас:

- локальный avoidance должен быть математически легким и масштабируемым
- распараллеливание имеет смысл там, где агентные расчеты независимы по snapshot

## 3. Continuum Crowds

URL:

- [Continuum Crowds reference](https://www.researchgate.net/publication/220183618_Continuum_crowds)

Key takeaways:

- global navigation and local flow should not be disconnected
- dynamic potential field can produce smooth large-scale motion

Что полезно для нас:

- path field должен быть не только shortest path, но и поддержкой для smooth flow structure

## 4. Real pedestrian trajectory references

URLs:

- [Jülich bottleneck trajectories](https://www.fz-juelich.de/en/jsc/downloads/trajectories-bottleneck-pedestrian-dyn)
- [Jülich legacy dataset page](https://data-legacy.fz-juelich.de/dataset.xhtml?persistentId=doi%3A10.26165%2FJUELICH-DATA%2FYIJOQW)
- [2026 public squares trajectory dataset](https://www.nature.com/articles/s41597-026-06686-6)

What these datasets remind us:

- реальные люди дают family of trajectories, а не одну линию
- bottleneck and corridor behaviors differ
- geometry type matters
- one universal parameter setting is rarely enough for all facilities

## Applied conclusion for this project

Наша модель должна стремиться к гибридному поведению:

- глобальное поле маршрута
- локальное smooth avoidance
- вариативность агентов
- congestion-aware exit choice
- facility-sensitive behavior

## Linked notes

- [[04 - Behavior Model and Solver]]
- [[08 - Open Questions and Next Steps]]

