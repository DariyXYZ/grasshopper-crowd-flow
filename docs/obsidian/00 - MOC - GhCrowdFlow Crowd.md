---
title: GhCrowdFlow Crowd Knowledge Base
aliases:
  - Crowd MOC
  - GhCrowdFlow MOC
tags:
  - project/ghcrowdflow
  - type/moc
  - status/active
---

# GhCrowdFlow Crowd

Главная точка входа в базу знаний по crowd-плагину для Rhino/Grasshopper.

> [!info]
> Эта папка задумана как “память проекта” для новых веток и новых диалогов, чтобы работа продолжалась вперед без повторного раскопа контекста.

## Быстрый старт

- [[01 - Current Status]]
- [[02 - Architecture]]
- [[03 - Build and Deploy]]
- [[04 - Behavior Model and Solver]]
- [[05 - Research Benchmarks]]
- [[06 - Errors and Debugging]]
- [[07 - Iteration Log]]
- [[08 - Open Questions and Next Steps]]

## Что здесь хранить дальше

- подтвержденные находки по коду
- гипотезы, которые еще не проверены
- тестовые сцены и наблюдения по траекториям
- проблемы сборки, деплоя и блокировок Rhino
- сравнения с внешними движками и данными реальных пешеходов
- решения, которые уже пробовали и почему они сработали или не сработали

## Текущее состояние проекта

- Публичный репозиторий `GhCrowdFlow-release` оказался standalone-срезом, а не полной историей IND-ветки.
- Реальная старая IND-совместимая база найдена в `C:\VS Code\workfiles\INDToolsUpdate`.
- Основная проблема последних итераций: solver стал одновременно менее живым и более медленным.
- Уже подтверждено, что часть деградации была не в модели как таковой, а в том, что тестировались не те сборки или загружались старые файлы из Grasshopper libraries.

## Рекомендуемый порядок чтения

1. [[01 - Current Status]]
2. [[03 - Build and Deploy]]
3. [[04 - Behavior Model and Solver]]
4. [[05 - Research Benchmarks]]
5. [[08 - Open Questions and Next Steps]]

