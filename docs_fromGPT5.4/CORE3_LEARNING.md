# Core3 Learning Summary

**Purpose:** Summarize what Core3 got right, what it still got wrong, and what Core4 should directly inherit from it.

**Last Updated:** 2026-05-12

---

## 1. Overall conclusion

Core3 was much closer to the right foundation than Core2 because it returned to a sequential, readable, easier-to-debug backup flow.

The main lesson is:

**A simple sequential engine is a better foundation than a complex parallel pipeline.**

That does not mean Core3 is sufficient as the final design.  
It means Core3 is a better baseline that still needs important corrections.

---

## 2. What Core3 got right

### 2.1 Sequential orchestration

Core3's linear flow made the engine easier to understand:

- scan
- transfer
- optional feature stages
- sidecar/completion

That is valuable and should be preserved in Core4's foundation.

### 2.2 Simpler component boundaries

Compared to earlier generations, Core3 had a more understandable split between:

- scanning
- transfer
- metadata/hash/sidecar work
- DI/composition

That direction is worth keeping.

### 2.3 Fewer concurrency failures

Because Core3 was sequential, it avoided many of the deadlocks and race conditions that made Core2 fragile.

That is especially important for MTP-oriented design thinking.

---

## 3. What Core3 still got wrong

### 3.1 Relative paths were not protected strongly enough

That left room for directory-structure loss and filename collisions.

**Core4 consequence:** relative paths must be central to item and destination handling.

### 3.2 Transfer failures were too destructive

A single file failure could stop too much of the overall backup.

**Core4 consequence:** per-file failures should be handled explicitly and should not unnecessarily kill the whole run.

### 3.3 Sidecars happened too late

If sidecars are produced too late, later feature failures can leave successful transfers undocumented.

**Core4 consequence:** sidecars must be written immediately after successful transfer.

### 3.4 Dry-run semantics were too weak

Dry-run could still be too expensive or too semantically unclear.

**Core4 consequence:** dry-run should be intentionally cheaper and behaviorally explicit.

### 3.5 Progress understanding was not stable enough

There was still mismatch between what the system wanted to report and what it could safely and consistently report.

**Core4 consequence:** the public progress contract should stay factual and stable before richer UI metrics are added.

---

## 4. What Core4 should inherit from Core3

Core4 should directly inherit:

- sequential baseline thinking
- readable control flow
- simpler DI and component structure
- clear cancellation awareness
- easier debugging

Core4 should therefore look more like Core3 than Core2 at its foundation.

---

## 5. What Core4 must change relative to Core3

Core4 must not become just "Core3 with more features".

It must also correct the structural gaps:

- preserve relative paths correctly
- write `.sidecar.json` immediately after transfer
- make MTP an explicit sequential strategy
- keep feature flags explicit
- use `MetadataExtractor` first and `ExifTool` as fallback
- keep the public baseline on `BackupPlan`, `BackupResult`, `IBackupProgress`, and `IFileProgress`

---

## 6. The parallelism lesson from Core3

It is important to draw the correct conclusion.

Core3 does **not** prove that Core4 should now be parallelized in a general way.  
It proves that the sequential baseline is the right place to start.

That leads to the normalized Core4 decision:

- MTP = sequential
- filesystem = sequential first, limited parallelism later

---

## 7. Final takeaway

If there is one thing to carry from Core3 into Core4, it is this:

- keep the simplicity
- fix the concrete correctness issues
- move sidecars earlier
- keep MTP sequential
- add parallelism only after the baseline is trustworthy

That is the right way to learn from Core3.
