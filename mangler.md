# Core4 — Mangler / Issues

> Generated 29 May 2026. Updated after timestamp correction work.

---

## Fixed in this session

| Item | Status |
|------|--------|
| Timestamp correction (ResolveAndApplyEarliestAsync) | ✅ Applied to file + Item.Date* + Metadata |
| BackupPlanValidator `EnableTimestampCorrection` gate | ✅ Gate removed |
| `EarliestTimestampResolutionServiceAnother.cs` | ✅ Deleted (dead code) |
| `ConvertToDateTimeOffset` dead method | ✅ Removed |

---

## Remaining Issues

### Critical

- **C4: BackupPlanValidator blocks all plans**
  - Tier-gates prevent even minimal plans from reaching the engine.
  - `CollisionStrategy` — only `Error` allowed (line 75-76)
  - `ComparisonHashAlgorithmTypes` — feature-gated (line 114-115)
  - `VerificationHashAlgorithmTypes` — feature-gated (line 117-118)
  - `EnableMetadata` — feature-gated (line 120-121)
  - `SidecarFormat` — `None` and `Json` reject (lines 90-94)
  - `IncludePatterns`/`ExcludePatterns` — feature-gated (lines 99-103)
  - Several more...

- **I4: MoveableFileContent.MoveTo ignores overwrite parameter**
  - File: `Models/MoveableFileContent.cs:26`
  - `FileInfo.MoveTo(destinationPath, false)` — `overwrite` hardcoded to `false`
  - Any run with `CollisionStrategy.Overwrite` crashes with IOException.
  - The fix: change `false` → `overwrite`.

### Important

- **I2: Binary/hash comparison missing in collision handling**
  - No `ICollisionComparer` interface exists in Core4.
  - `CollisionHelpers.ResolveTargetPath` only checks `File.Exists()`.
  - Original Core has byte-by-byte and SIMD-accelerated comparison.
  - Cannot detect false duplicates (same content, different name).

- **I3: Post-write verification missing**
  - No `IPostWriteVerification` interface exists in Core4.
  - After `MoveTo`, no re-read or re-hash of the destination file.
  - Cannot detect silent corruption or failed writes.

- **I5: Sidecar does not match Original Core metadata richness**
  - Missing `[DeviceDetails]` / `[DriveDetails]` sections.
  - Missing `[PathMapping]` section (original → sanitized path).
  - Missing backup timestamp `BackupDateTime` (uses `StartTime`).

### Medium

- **Unused variable `earliest` in BackupEngine.cs:254**
  - `EarliestTimestampResolutionResult earliest = await ...`
  - Variable assigned but never read. Service now updates metadata directly.

- **DeleteEmptyDirectories post-run missing**
  - Original Core cleans up empty source directories after backup.
  - Not implemented in Core4.

- **Step numbering jump in BackupEngine (8 → 10)**
  - Comments skip step 9 (cosmetic).

### Low / Deferred

- **JSON sidecar** — `NotImplementedException` in `SidecarService` (line 14)
- **MTP/MediaDevice support** — `NotSupportedException` in `SourceTraversalFactory`
- **Runner subsystem** — `IBackupRunnerFactory`/`IBackupRunner` exist but not wired (intentional — parallelism deferred)
- **Source/output access probe** — missing, but fail-first is acceptable for now
- **Progress reporting** — per-item `BytesProcessed` only updated during download/hash, not final state
