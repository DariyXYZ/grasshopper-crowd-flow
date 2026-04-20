# Reporting Workflow

`GhCrowdFlow` includes a simple architect-facing reporting path built around two Grasshopper components:

- `Export Crowd Image`
- `Export Crowd Report`

## Included Template

Use the bundled template:

- `templates/reporting/CrowdReport_Template.docx`

## Expected Workflow

1. Run the crowd simulation and keep the resulting `CrowdSimulationResult`.
2. Generate an optional `CrowdHeatmapResult`.
3. Use `Export Crowd Image` to save a report-ready PNG from the scene or heatmap geometry.
4. Use `Export Crowd Report` with:
   - the simulation result
   - the optional heatmap result
   - the exported PNG path
   - the included template path
   - a target output path for the report

## Notes

- DOCX export is the baseline workflow.
- PDF export relies on Microsoft Word COM on Windows.
- The template path is intentionally explicit so users can replace the DOCX layout with their own office standard if needed.
- Heatmap legend labels and value ranges are intended to flow from the heatmap result into the report text.
