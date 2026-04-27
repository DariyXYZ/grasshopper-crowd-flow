---
title: Current Status
tags:
  - project/ghcrowdflow
  - status/active
  - type/status
---

# Current Status

## Summary

Проект находится в переходной точке между публичным standalone-репо и старой рабочей IND-совместимой линией.

> [!warning]
> Нельзя считать `GhCrowdFlow-release` единственным источником правды по проекту. Исторически рабочая IND-база и сборочная логика сохранились в `C:\VS Code\workfiles\INDToolsUpdate`.

## Что уже подтверждено

- В `GhCrowdFlow-release` всего один публичный коммит `Prepare standalone public GhCrowdFlow repository`.
- Из-за этого в текущем репо:
  - пропала нормальная git-история итераций поведения
  - assembly и категория плагина были переименованы из `IND/INDTools` в `GhCrowdFlow`
  - возник разрыв между реальным IND-плагином и публичным репозиторием
- Старая IND-совместимая база найдена по пути:
  - `C:\VS Code\workfiles\INDToolsUpdate`

## Что уже сделано в текущей ветке

- возвращена ориентация на `INDTools` в категориях Grasshopper-компонентов
- `INDGrasshopperComponents.gha` снова используется как имя артефакта для деплоя
- добавлена обратная совместимость по `CreateAgentProfile(...)`
- починена часть build/deploy-конвейера
- ускорен `CrowdPathFieldBuilder` через min-heap вместо линейного поиска
- добавлен spatial index для соседних агентов
- добавлен кэш расстояний до границ в grid
- частично возвращена поведенческая вариативность через более живые default-параметры

## Что еще не завершено

- Нужна чистая стабилизация сборки net48 после переименования артефактов.
- Rhino блокирует перезапись `Crowd.dll` и `INDGrasshopperComponents.gha`, если открыт во время билда.
- Текущее качество траекторий еще не подтверждено на живой сцене после последних правок.
- Основной вопрос: продолжать ли разработку в `GhCrowdFlow-release` или переносить source of truth обратно в `INDToolsUpdate`.

## Практический вывод

На данный момент safest path такой:

1. закрыть Rhino перед сборкой
2. использовать эту базу знаний как контекст
3. сравнивать поведение и сборку с `INDToolsUpdate`
4. не делать выводов о модели, пока не подтверждено, что Rhino подхватил новую сборку

## Связанные заметки

- [[03 - Build and Deploy]]
- [[04 - Behavior Model and Solver]]
- [[06 - Errors and Debugging]]
- [[08 - Open Questions and Next Steps]]

## 2026-04-27 canonical repo and package staging cleanup

- Restored the practical source-of-truth rule:
  - active GitHub-bound local repo: `C:\VS Code\GhCrowdFlow-release`
  - offline package staging: `X:\CompDesign_Projects\Library\crowd_flow`
  - `INDToolsUpdate` is reference/context only for this standalone packaging pass
- Standalone plugin identity is now implemented in the canonical repo:
  - `CrowdFlow.dll`
  - `CrowdFlow.gha`
  - plugin metadata name `Crowd Flow`
  - deploy path `%APPDATA%\Grasshopper\Libraries\CrowdFlow\net48`
- Components still intentionally appear under `INDTools / Crowd` inside Grasshopper.
- `build.ps1 -DeployToGrasshopper` now creates the clean deploy folder before copying files.
- `X:\CompDesign_Projects\Library\crowd_flow\plugin_library` was refreshed from `GhCrowdFlow-release\artifacts\bin\GrasshopperComponents\Release\net48` and hash-matched against the repo artifacts.
