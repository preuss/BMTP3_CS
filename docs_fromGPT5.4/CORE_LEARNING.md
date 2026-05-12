# Core Learning Summary

**Purpose:** Describe what the original Core taught us and how those lessons translate into concrete Core4 decisions.

**Last Updated:** 2026-05-12

---

## 1. Overall conclusion

The original Core proved that the backup domain is valuable and practical, but it also showed that too much responsibility concentrated in a few large classes becomes hard to maintain and hard to evolve safely.

The key lesson is:

**Feature richness is not enough. The structure must also be sustainable.**

---

## 2. What the original Core got right

### 2.1 It addressed real backup needs

The original Core dealt with real problems:

- real sources
- real files
- metadata
- sidecars
- temp-file handling and cleanup

That matters. Core4 must not become so "clean" that it forgets the practical domain needs.

### 2.2 It revealed the right domain concerns

Legacy code made it clear that these areas matter:

- scanning and source identity
- metadata quality
- sidecar consistency
- cleanup on failure
- explicit state

Core4 should take all of those seriously.

---

## 3. What the original Core got wrong

### 3.1 Too much responsibility in too few classes

Large orchestration classes mixed:

- source selection
- scanning
- transfer
- metadata
- comparison
- sidecars
- error handling

**Core4 consequence:** split responsibilities into small services with clear contracts.

### 3.2 Fail-fast without a good model for partial success

When one file or one step failed, it was difficult to handle partial progress cleanly.

**Core4 consequence:** Core4 should distinguish clearly between per-file failures and job-level failures.

### 3.3 Sidecar and metadata were too tightly coupled to the main flow

When supporting behavior is hard-wired into the orchestration, replacement and testing become difficult.

**Core4 consequence:** sidecar and metadata must sit behind their own interfaces and models.

### 3.4 State was too implicit

When state is represented through nullable fields and mixed logic, the system becomes harder to reason about and debug.

**Core4 consequence:** Core4 should use explicit session state and explicit item status.

### 3.5 Metadata handling was too fragile

Reading metadata on too many file types and without a clear fallback strategy created unnecessary failures.

**Core4 consequence:** metadata should be read through a controlled, normalized strategy.

---

## 4. What Core4 should inherit from legacy

Core4 should inherit:

- respect for the practical backup domain
- cleanup discipline
- the value of sidecars
- the need for metadata and timestamps
- the need for clear user-relevant outcomes

Core4 should **not** inherit the monolithic structure.

---

## 5. What Core4 should do differently

Based on the legacy lessons, Core4 should:

- split responsibilities across scanner, transfer, sidecar, metadata, validation, and engine
- keep contracts small and explicit
- use explicit `BackupSessionState`
- make sidecar generation a separate service
- use a clear metadata strategy
- handle cleanup and partial failures more deliberately

This is also why Core4 now builds on a smaller, more stable public contract.

---

## 6. The metadata and sidecar lesson

Legacy code especially shows two things:

1. metadata is important, but fragile
2. sidecars are valuable, but they must be able to evolve without being hard-wired into the main orchestration

The normalized Core4 consequence is therefore:

- read metadata through `MetadataExtractor` first
- use `ExifTool` as fallback
- let filesystem attributes provide minimum fallback data
- write sidecars as `.sidecar.json`
- write the sidecar immediately after successful transfer

---

## 7. Final takeaway

If there is one thing to carry from the original Core into Core4, it is this:

- keep the practical domain understanding
- drop the monolithic structure
- make state, metadata, and sidecar explicit
- make failure handling and cleanup more controlled

That is the right way to learn from legacy Core.
