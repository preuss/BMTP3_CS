# Core2 Learning Summary

**Purpose:** Summarize what Core2 taught us and which of those lessons Core4 should actually turn into design decisions.

**Last Updated:** 2026-05-12

---

## 1. Overall conclusion

Core2 proved that performance and feature richness matter, but it also proved that a complex pipeline architecture can cost too much in stability, debugging effort, and maintainability.

The main lesson is:

**Complex parallel orchestration is not a good foundation for Core4.**

---

## 2. What Core2 got right

### 2.1 The ambition was valid

Core2 tried to solve real problems:

- better throughput
- better separation of concerns
- support for optional feature layers
- DI-friendly composition

Those goals remain valid.

### 2.2 It revealed where performance can help

Core2 showed that filesystem backups may benefit from controlled concurrency.

That is relevant for Core4 - but **only later** and **only for filesystem**.

---

## 3. What Core2 got wrong

### 3.1 The pipeline became too complex

Channels, worker pools, and implicit stage coupling made the flow harder to understand and harder to prove correct.

**Core4 consequence:** start with sequential orchestration, not channel topology.

### 3.2 Concurrency introduced race conditions

When shared state, progress, and device resources are updated from multiple places, failures become hard to understand and reproduce.

**Core4 consequence:** introduce parallelism only after the baseline is already correct and simple.

### 3.3 MTP was treated too much like a normal parallel workload

That created instability, timeouts, and disconnect risk.

**Core4 consequence:** MTP is always sequential in Core4.

### 3.4 Error policies were too implicit

When pipeline stages just forward items, it becomes unclear which failures should stop, which should continue, and who owns the decision.

**Core4 consequence:** failure policy should be explicit and easy to follow in the engine flow.

### 3.5 Debugging became too expensive

If a developer must understand channels, stage protocols, and worker lifecycles just to debug a single problem, the architecture is already too expensive.

**Core4 consequence:** data flow should stay visible and linear for as long as possible.

---

## 4. What Core4 should inherit from Core2

Core4 should inherit these ideas - but in simpler form:

- separation of concerns
- optional feature layers
- dependency injection
- the insight that filesystem may deserve later performance improvements

Core4 should **not** inherit the pipeline architecture itself.

---

## 5. What Core4 should not copy

Core4 should not:

- rebuild channel-pipeline architecture as the baseline
- let MTP participate in the parallel engine
- make progress depend on complex shared-state synchronization
- hide failure policy in stage transitions
- treat pipeline reuse as a goal in itself

The correct lesson from Core2 is **not** "build a better pipeline".  
The correct lesson is "do not make pipeline complexity the baseline".

---

## 6. The normalized Core4 conclusion

Core2 therefore points toward this Core4 direction:

- sequential engine as the foundation
- streaming scanner contract
- MTP = sequential
- filesystem = sequential first, limited parallelism later
- optional feature layers on top of the stable baseline

---

## 7. The parallelism lesson from Core2

If one precise rule should be carried forward from Core2, it is this:

**Parallelism should only be introduced where the gain is real and the complexity does not destroy stability.**

In Core4 that means:

- not in Tier 1
- not for MTP
- later for filesystem only
- without redefining the public contracts

---

## 8. Final takeaway

Core2 taught us:

- that performance and features are relevant goals
- that pipeline complexity is a poor foundation
- that MTP cannot be treated like filesystem
- that Core4 must grow in layers instead of starting at maximum sophistication

That is the right way to learn from Core2.
