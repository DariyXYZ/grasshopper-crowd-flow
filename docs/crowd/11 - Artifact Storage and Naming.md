---
title: Artifact Storage and Naming
tags:
  - project/ghcrowdflow
  - type/process
  - type/validation
  - artifacts
---

# Artifact Storage and Naming

## Purpose

This note defines the canonical storage pattern for validation screenshots and run reports.
The goal is to keep visual evidence compact, searchable, and easy to compare across iterations.

## Canonical folders

Project root:
- `C:\Users\dariy.n\Documents\Obsidian Vault\30 Projects\Grasshopper Plugins\GhCrowdFlow Crowd`

Artifacts root:
- `artifacts\`

Screenshot root:
- `artifacts\screenshots\`

Validation report root:
- `artifacts\reports\`

Templates root:
- `templates\`

## Folder structure

Use this pattern for screenshot batches:

```text
artifacts/
  screenshots/
    YYYY-MM-DD/
      run-01/
        scene-a-right-neck/
        scene-b-upper-split/
      run-02/
        scene-a-right-neck/
  reports/
    YYYY-MM-DD__run-01__validation-report.md
```

Why this shape works:
- date keeps chronology obvious
- `run-01`, `run-02` lets us compare multiple builds on the same day
- scene folders keep related screenshots together without extremely long filenames
- report note sits next to the artifact tree and summarizes runtime, settings, and conclusions

## Naming convention

### Run folder

Pattern:
- `run-01`
- `run-02`
- `run-03`

Meaning:
- one run folder = one build/configuration/test pass

### Scene folder

Pattern:
- `scene-a-right-neck`
- `scene-b-upper-split`
- `scene-c-l-corner`
- `scene-d-tight-gate`

Rules:
- lowercase kebab-case
- short but descriptive
- stable names across time so the same scene can be compared across reports

### Screenshot file

Pattern:
- `YYYY-MM-DD__run-01__scene-a-right-neck__img-01.png`
- `YYYY-MM-DD__run-01__scene-a-right-neck__img-02.png`

Rules:
- include date, run id, scene id, and image number
- keep metadata like runtime, agent count, timestep, and solver settings in the report note, not in the image filename
- use `.png` unless another format is clearly needed

### Validation report file

Pattern:
- `YYYY-MM-DD__run-01__validation-report.md`
- `YYYY-MM-DD__run-02__validation-report.md`

## Minimum metadata to record per run

Each report should store:
- build date
- run id
- branch or repo context
- short build description
- runtime range or exact runtime
- agent count
- timestep
- notable agent profile settings
- scene list
- links to screenshot folders or individual screenshots
- what improved
- what regressed
- decision: keep / refine / rollback

## Practical workflow

1. Build and deploy plugin.
2. Run a scene set in Rhino / Grasshopper.
3. Save screenshots into:
   - `artifacts\screenshots\YYYY-MM-DD\run-XX\scene-slug\`
4. Create a report in:
   - `artifacts\reports\YYYY-MM-DD__run-XX__validation-report.md`
5. Link the screenshot folders inside the report.
6. Add a short summary back into the main validation note if the run matters strategically.

## Notes

- Keep screenshot storage compact by putting repeated metadata into the report note rather than bloating filenames.
- Preserve both successful and failed runs. Failures are useful regression evidence.
- If a run becomes especially important, summarize it in `10 - Test Scenes and Validation Reports.md` as well.

## Links

- [[Project Home]]
- [[10 - Test Scenes and Validation Reports]]
- [[07 - Iteration Log]]
