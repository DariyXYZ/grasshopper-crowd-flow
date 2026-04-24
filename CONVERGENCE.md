## 2026-04-24 - Uses IND-style assembly and component naming

**What diverges:** The standalone plugin still uses IND-aligned assembly and UI naming in several places, including `INDGrasshopperComponents`, `INDTools` Grasshopper categories, and related deploy-property names.

**Why:** The current standalone stream is intentionally kept close to the IND-integrated shape so that later integration costs stay low and the deployed plugin remains compatible with the existing test/deploy expectations.

**What merge would require:** Mainly a product decision rather than a code rewrite. If the standalone repo later needs fully public branding, this would require a coordinated rename across csproj metadata, component categories, build scripts, docs, and deployment expectations.

**Status:** open

## 2026-04-24 - Uses Word COM for report export

**What diverges:** `CrowdReportExportService` still automates Microsoft Word via COM using `Type.GetTypeFromProgID` and late-bound `InvokeMember`.

**Why:** The current report workflow depends on an existing DOCX template and a practical Windows-first export path already validated on target machines.

**What merge would require:** Replacing the COM export path with an OpenXML-native or other platform-safe document pipeline. That would remove the Windows-only interop dependency and bring the implementation closer to the IND architectural preference.

**Status:** open

## 2026-04-24 - Private project-history documentation remains in the repo

**What diverges:** `docs/crowd/` still contains historical integration and internal-path notes that reference INDTools, local machine paths, and offline library locations.

**Why:** These notes are currently useful as active engineering context during standalone stabilization and future reintegration planning.

**What merge would require:** Splitting private engineering notes from public-facing repo documentation, then scrubbing internal paths and IND-specific operational details before treating the standalone repo as a clean public package.

**Status:** open
