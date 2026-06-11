# Software Design Principper & Tanker

Et overblik over de vigtigste kategorier af software design-principper.

---

## Design Principper

**SOLID**
- **S** — Single Responsibility: én klasse, ét ansvar
- **O** — Open/Closed: åben for udvidelse, lukket for modificering
- **L** — Liskov Substitution: subtyper skal kunne erstatte deres basistype
- **I** — Interface Segregation: mange små interfaces frem for ét stort
- **D** — Dependency Inversion: afhæng af abstraktioner, ikke konkretioner

**GRASP** (General Responsibility Assignment Software Patterns)
- Creator, Controller, Low Coupling, High Cohesion, Polymorphism, Pure Fabrication, Indirection, Protected Variations

**DRY** — Don't Repeat Yourself
**KISS** — Keep It Simple, Stupid
**YAGNI** — You Aren't Gonna Need It
**Separation of Concerns**
**Law of Demeter** — tal kun med dine nærmeste naboer
**Composition over Inheritance**
**Favor Explicitness over Implicitness**

---

## Architectural Patterns

**Clean Architecture** (Onion / Ports & Adapters / Hexagonal)
**Domain-Driven Design (DDD)** — Aggregates, Bounded Contexts, Ubiquitous Language
**Event-Driven Architecture**
**CQRS** — Command Query Responsibility Segregation
**Event Sourcing**
**Microservices vs. Monolith**

---

## Fejlhåndtering & Robusthed

**Fail-Fast** — kast exception tidligt, lad ikke fejl propagere
**Defensive Programming** — valider alt input
**Offensive Programming** — modsætning til defensive; stol på kalderen, fejl bør aldrig ske
**Circuit Breaker** — stop at kalde et fejlende subsystem
**Bulkhead** — isoler fejl så de ikke spreder sig
**Dead Letter Queue** — fejlede beskeder parkeres til manuel inspektion

---

## Kode-kvalitet & Stil

**Command-Query Separation (CQS)** — en metode enten muterer state *eller* returnerer data, aldrig begge
**Tell, Don't Ask** — bed objektet om at gøre noget, spørg det ikke om data og beslut selv
**Explicit over Implicit**
**Make Invalid States Unrepresentable** — brug typer så compileren afviser ugyldige tilstande
**Principle of Least Astonishment** — opfør dig som kalderen forventer

---

## Testing

**Test Pyramid** — mange unit tests, færre integration, få E2E
**Given/When/Then** (Arrange/Act/Assert)
**Test doubles** — Mock, Stub, Fake, Spy, Dummy
**Property-Based Testing** — generer tilfældige inputs, verificer invarianter

---

## Concurrency & Performance

**Immutability** — undgå shared mutable state
**Actor Model** — isolerede enheder kommunikerer via beskeder
**Backpressure** — slow consumers signalerer til producers

---

## I dette projekt (Core4)

Principper der aktivt anvendes:

| Princip | Hvor |
|---|---|
| Fail-Fast | Validator gates kaster exception ved første fejl |
| CQS | Validators returnerer intet, kaster bare |
| Make Invalid States Unrepresentable | Enums frem for strings (`CollisionStrategy`, `RenameStrategy`, osv.) |
| Explicit over Implicit | Direkte type-navne frem for aliases, direkte assign frem for Map-metoder |
| Separation of Concerns | Core4 kender ikke til CLI; Consoles kender ikke til engine-internals |
| YAGNI | `MaxDegreeOfParallelism` fjernet — ikke implementeret, bruges aldrig |
| Single Responsibility | `BackupPlanValidator`, `RenameCollisionResolver`, `MtpUriParser` — ét ansvar hver |
