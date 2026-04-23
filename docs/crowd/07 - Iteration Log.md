---
title: Iteration Log
tags:
  - project/ghcrowdflow
  - type/log
---

# Iteration Log

## Early observed direction

- Были более живые траектории и волны.
- Затем шла серия итераций по борьбе с завихрениями.
- После этого поведение стало слишком минималистичным и прямолинейным.

## Confirmed project-level discoveries

- Публичное repo оказалось standalone snapshot.
- Старый IND source base найден отдельно в `workfiles/INDToolsUpdate`.
- Часть пользовательских тестов шла не по свежему коду, а по старой загруженной сборке.

## Technical iterations already done

### Build / deployment

- добавлен deploy root в Grasshopper libraries
- добавен `.gha` artifact generation
- возвращено имя `INDGrasshopperComponents.gha`
- возвращены `INDTools` categories in GH components

### Compatibility

- добавлен backward-compatible overload для `CreateAgentProfile(...)`

### Performance

- boundary distance caching in grid
- spatial index for neighbor queries
- path field builder changed from linear open-list search to heap-based priority queue

### Behavior

- частично возвращены более живые defaults
- усилена heterogeneity
- ослаблена излишняя route rigidity
- оставлены современные элементы solver-а вместо полного отката к старому коду

## What still needs explicit validation

- скорость на реальной пользовательской сцене
- качество траекторий после закрытия Rhino и чистого deploy
- корректность exit choice variation
- сохранение smoothness without vortices

## How to log future iterations

Для каждой новой итерации фиксировать:

- какая гипотеза проверялась
- какие файлы менялись
- какой expected effect
- что увидели на тестовой сцене
- что это значит для следующей итерации

## Links

- [[04 - Behavior Model and Solver]]
- [[06 - Errors and Debugging]]
- [[08 - Open Questions and Next Steps]]

