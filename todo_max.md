# BMTP3 Todo — Analyse af mangler i BackupEngine

> **⚠️ HISTORICAL — Core2 only. Core4 supersedes this entirely.**
>
> This file documents issues from Core2 development. All items are either completed
> (Core2) or not applicable to Core4. Core4 has its own engine with separate tracking.
>
> **For active Core4 work, see:**
> - `plan.md` — task list and priorities
> - `mangler.md` — all open issues, bugs, code smells, and feature gaps

## ✅ COMPLETED FIXES (Core2 — historical)

### 1. Path Validation (HIGH PRIORITY) - ✅ COMPLETED
- Tilføjet `ValidateFileName()` der kaster `ArgumentException` hvis filnavn indeholder ugyldige tegn eller overskrider 255 tegn
- Tilføjet `ValidatePath()` der kaster `ArgumentException` hvis sti indeholder ugyldige tegn eller `..` path traversal

### 2. Staging File Cleanup (CRITICAL) - ✅ COMPLETED
- `LocalFileTransfer` sletter IKKE længere staging-filen automatisk
- `TransferItemStep` sletter staging EFTER successful verification
- Tilføjet `TryCleanupStaging()` metode

### 3. MTP Timeout (CRITICAL) - ✅ COMPLETED
- Tilføjet `MtpOperationTimeoutMs` (default 60s) i `BackupEngineOptions`
- `MtpGatekeeper` bruger nu timeout via `CancellationTokenSource.CancelAfter()`
- Timeout konfigureres via DI fra options

### 4. MTP Session Cleanup (HIGH) - ✅ COMPLETED
- Tilføjet eksplicit `mtpSession.Dispose()` i catch-blok ved pipeline crash
- Tilføjet `mtpSession.Dispose()` efter normal completion

### 5. Unbounded Memory - CollisionResolver (HIGH) - ✅ COMPLETED
- Tilføjet periodic cleanup af `_reservedRenamePaths`
- Tilføjet max limits: MaxRenameLocks=1000, MaxReservedPaths=10000
- `TryCleanupOldEntries()` kaldes hver 100. operation

### 6. Race Condition - Rename Lock (MEDIUM) - ✅ COMPLETED
- Ændret fra `GetOrAdd` til `TryGetValue` + `GetOrAdd` pattern
- Dispose extra semaphore hvis to oprettes for samme mappe

### 7. Verification Hash Selection (MEDIUM) - ✅ COMPLETED
- Tilføjet `SelectStrongestHash()` metode der vælger stærkeste algoritme
- Priority: BLAKE3_512 > BLAKE3_256 > SHA3 > SHA2 > MD5

### 8. Verification Retry Logic (MEDIUM) - ✅ COMPLETED
- Tilføjet `permanentFailure` short-circuit for hash verification
- Hash mismatch er permanent - retry giver ingen mening

---

## 🔜 REMAINING ISSUES (Core2 — not applicable to Core4)

### 9. Duplicate CompareBinaryAsync (LOW PRIORITY)
- To implementeringer: `CollisionResolver.cs` og `TransferItemStep.cs`
- Den i TransferItemStep har retry, den anden har ikke
- **Core4:** Single `ContentCompareAsync` in `RenameCollisionResolver` — no duplication

### 10. Worker Termination on Cancellation (LOW PRIORITY)
- Når `OperationCanceledException` kastes i worker, terminerer hele worker
- Kan efterlade items uprocesserede
- **Core4:** Single-threaded sequential engine — no worker pool concern
