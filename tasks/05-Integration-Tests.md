# Task: Integration Tests

> Tilføj integration tests for MTP traversal pipeline og BackupEngine end-to-end.

---

## Goal

Sikr at hele pipeline fra source discovery → traversal → download → hash → sidecar → verify virker sammen.

---

## Test 1: MTP Traversal Pipeline

**Scope:** gatekeeper → connector → traversal → content

```csharp
[Fact]
[SupportedOSPlatform("windows7.0")]
public async Task MtpTraversalPipeline_ConnectsAndTraverses()
{
    // Arrange
    // - Setup DI container med real services (eller stubs)
    // - Find MTP device via IMediaDeviceSourceDiscovery
    
    // Act
    // - Connect via ISourceConnector
    // - Create traversal via ISourceTraversalFactory
    // - Traverse and collect items
    
    // Assert
    // - Items found
    // - Each item has valid Id, FileName, RelativePath
}
```

**Udfordring:** Kræver real MTP device tilsluttet. Alternativ: brug mock/stub for `IMediaDevice`.

---

## Test 2: BackupEngine End-to-End

**Scope:** filesystem → download → hash → sidecar → verify

```csharp
[Fact]
public async Task BackupEngine_RunsEndToEnd_OnFileSystem()
{
    // Arrange
    // - Opret temp directory med test filer
    // - Opret BackupPlan (SourceType: FileSystem, Destination: temp dir)
    // - Setup DI med real services
    
    // Act
    // - Kør backupEngine.RunAsync(plan, ct)
    
    // Assert
    // - BackupResult.State == Completed
    // - Filer findes i destination
    // - Sidecar filer findes
    // - Session state gemt
    // - Catalog fil (hvis implementeret) findes
}
```

---

## Files to Create

- `BMTP3.Core4.Tests/Integration/MtpTraversalPipelineTests.cs`
- `BMTP3.Core4.Tests/Integration/BackupEngineEndToEndTests.cs`

---

## Dependencies

- Afhænger af stabil Core4 (de fleste tasks bør være done først)
- Kan starte med filesystem e2e test (kræver ikke MTP device)
