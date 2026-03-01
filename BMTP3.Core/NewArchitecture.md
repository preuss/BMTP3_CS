# BMTP3.Core Refactoring: Architecture Proposal

This document outlines the plan for refactoring the `BMTP3.Core` project. It details the current legacy architecture, the goals for the new design, and a proposal for a new, modern pipeline-based architecture.

## 1. Analysis of the Current (Legacy) Architecture

The existing `BMTP3.Core` application is a functional command-line tool for performing backups. However, it mixes several responsibilities, making it complex and difficult to maintain or extend.

### Key Components:

*   **Entry Point (`Program.cs`):** Acts as the application starter. It initializes the dependency injection container, parses command-line arguments, and triggers the main backup process.
*   **Configuration (`ConfigurationHandler`, `BackupSettingsReader`):** Responsible for reading and parsing both command-line arguments and `.toml` configuration files. It combines these into a single `IBackupSettings` object that defines the entire backup session.
*   **Orchestration (`BackupMaster`):** The central "conductor". Its primary role is to discover all available backup sources (like local drives and portable MTP devices) and match them against the loaded configuration. It creates a list of `BackupJob` objects, representing a "many-to-many" backup plan.
*   **Execution (`BackupHandler`):** The main "workhorse". It receives a `BackupJob` and executes it. Its responsibilities are broad:
    *   Iterating through files on the source.
    *   Managing state via a JSON file (`BackupRecordDataStore`) to track already backed-up files.
    *   Copying files to the destination.
    *   Comparing files using a `FileComparer` to avoid duplicates.
    *   Creating `.ini` metadata "sidecar" files for each backed-up file using `SideCarDocument` and `IniSideCarWriter`.
*   **Core Problem:** The architecture violates the Single Responsibility Principle (SRP). `BackupHandler` does too much, and the core logic is tightly coupled to the concept of handling multiple sources and targets within a single run. This makes the code hard to test and reason about.

## 2. Goals for the New Architecture

The refactoring aims to adhere to modern software design principles (SOLID, KISS, DRY).

1.  **Decouple Core Logic from UI:** `BMTP3.Core` will be converted from an executable (`Exe`) to a class library (`Library`). It will contain no logic for parsing command-line arguments or reading configuration files.
2.  **Embrace Simplicity (KISS):** The core responsibility of the new library will be to execute a **single, well-defined backup operation**: from **one source** to **one target**.
3.  **Clear Separation of Concerns (SRP):** The responsibility for reading configuration and orchestrating multiple backup jobs (e.g., looping through a list of sources) will belong exclusively to the `BMTP3.Consoles` application. `BMTP3.Consoles` will call the `BMTP3.Core` library for each individual backup job.
4.  **Testability & Flexibility (DIP):** The new architecture must be built on abstractions (interfaces) to allow for easy unit testing and future extensions (e.g., adding a new cloud-based backup target).

## 3. Proposed "Enrichment Pipeline" Architecture

To solve the challenges of the old architecture and accommodate performance optimizations (especially for slow MTP devices), we propose a pipeline-based design.

The core concept is inspired by a factory assembly line. A single, central object, `BackupItem`, represents the file being processed. This "item" travels through a series of "stations" (pipeline stages). At each station, the *same* `BackupItem` object is "enriched" with additional data.

This approach avoids creating new object types at each stage and gives us fine-grained control over the concurrency of each step.

### The Central Data Model: `BackupItem`

This class acts as the "work-in-progress" object on the assembly line. It starts with basic information and is gradually filled out.

```csharp
// Location: NewBackupFlow/Models/BackupItem.cs
public class BackupItem
{
    // --- Stage 1: From Enumeration ---
    public IBackupSource Source { get; }
    public string SourceRelativePath { get; }
    public string Name { get; }
    public long Size { get; }
    public DateTimeOffset LastWriteTime { get; }

    // --- Stage 2: From Staging ---
    public string? LocalTempPath { get; set; }

    // --- Stage 3: From Analysis ---
    public string? Hash { get; set; }
    public string? HashAlgorithm { get; set; }

    // --- Stage 4: From Decision ---
    public BackupAction Action { get; set; } = BackupAction.Undecided;

    // --- Stage 5: From Commit ---
    public string? TargetRelativePath { get; set; }

    // Constructor for initial creation
    public BackupItem(IBackupSource source, string sourceRelativePath, string name, long size, DateTimeOffset lastWriteTime)
    {
        // ... initialization ...
    }
}
```

**Note on `null` values:** A current point of discussion is the use of nullable properties (`string?`). This reflects the "enrichment" process, where data is not available at the start. Alternative designs, such as using a state machine or separate state objects, could be considered to create a more type-safe model. This is a topic for further refinement.

### The Pipeline Stages (The "Stations")

Each stage is an independent service that performs one specific task. The entire process is orchestrated by a `BackupPipeline` class that moves items from one stage to the next, managing concurrency.

1.  **Stage 1: Enumeration**
    *   **Interface:** `IBackupSource`
    *   **Responsibility:** Discovers files on a given source (e.g., a local folder or an MTP device). For each file found, it creates a `BackupItem` object with the initial information and passes it into the pipeline.

2.  **Stage 2: Staging**
    *   **Interface:** `IItemStager`
    *   **Responsibility:** Ensures the file's content is available on a fast, local disk. For an MTP device, this involves downloading the file to a temporary folder. For a local file, it might just mean confirming its path. It populates the `BackupItem.LocalTempPath` property.
    *   **Concurrency:** This is a critical control point. For MTP sources, this stage **must** run single-threaded. For local disk sources, it can run with high parallelism.

3.  **Stage 3: Analysis**
    *   **Interface:** `IMetadataAnalyzer`
    *   **Responsibility:** Reads the local temporary file (from `LocalTempPath`) and performs CPU-intensive work, primarily calculating a cryptographic hash (e.g., MD5 or SHA256). It populates the `BackupItem.Hash` property.
    *   **Concurrency:** Highly parallelizable, as it only depends on the local disk.

4.  **Stage 4: Decision**
    *   **Interface:** `IDecisionEngine`
    *   **Responsibility:** Takes the enriched `BackupItem` and determines what to do with it. It queries a state store (`IBackupStateStore`) using the item's hash and relative path to see if an identical version already exists at the destination. It populates the `BackupItem.Action` property (e.g., with `Copy` or `Skip`).

5.  **Stage 5: Commit**
    *   **Interface:** `IBackupTarget`
    *   **Responsibility:** Executes the final action. If `BackupItem.Action` is `Copy`, this service:
        1.  Copies the file from `LocalTempPath` to the final destination.
        2.  Creates the associated `.ini` sidecar file with all the collected metadata.
        3.  Updates the `IBackupStateStore` to register that the file has been successfully backed up.
