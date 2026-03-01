# Arkitektur for Refaktorering af Backup-logik

Dette dokument beskriver den nye arkitektur for backup-funktionaliteten i BMTP3. Målet er at skifte fra en monolitisk batch-model til en fleksibel, kommando-drevet model, der håndterer én backup-kilde ad gangen.

## Overordnede Principper

- **Separation of Concerns:** Hver klasse har ét veldefineret ansvar.
- **Dependency Injection (DI):** Afhængigheder injiceres via constructors for at fremme løs kobling.
- **Compile-time Type Safety:** Arkitekturen udnytter C#'s generiske typer til at forhindre fejl under kompilering frem for under kørsel.

## Centrale Komponenter

Arkitekturen består af fire centrale komponenter, der arbejder sammen.

### 1. `BackupController`
- **Ansvar:** Fungerer som den centrale "router" eller orkestrerings-klasse for al backup-logik. Dette er det primære indgangspunkt fra konsol-applikationen til Core-biblioteket.
- **Logik:**
    1. Modtager en generisk `ISourceConfig` (konfiguration for den kilde, der skal backes up).
    2. Inspicerer den konkrete type af konfigurationen (f.eks. `DeviceSourceConfig` eller `DriveSourceConfig`).
    3. Anmoder DI-containeren om den korrekte, specialiserede `IBackupHandler<T>`.
    4. Kalder `PerformBackup` på den fundne handler med den typesikre konfiguration.

### 2. `IBackupHandler<T>` (Generisk Interface)
- **Ansvar:** Definerer kontrakten for en handler, der kan udføre en backup for én specifik type kilde. Dette er nøglen til at opnå typesikkerhed.
- **Definition:**
  ```csharp
  public interface IBackupHandler<T> where T : ISourceConfig
  {
      Task PerformBackup(T sourceConfig, CancellationToken cancellationToken);
  }
  ```

### 3. `DeviceBackupHandler` og `DriveBackupHandler` (Implementeringer)
- **Ansvar:** Indeholder den konkrete forretningslogik for at udføre en backup.
- **`DeviceBackupHandler`:**
    - Implementerer `IBackupHandler<DeviceSourceConfig>`.
    - Indeholder logik udelukkende for backup af et enkelt device.
- **`DriveBackupHandler`:**
    - Implementerer `IBackupHandler<DriveSourceConfig>`.
    - Indeholder logik udelukkende for backup af et enkelt drev.

### 4. `BackupConsoleCommand` (i `BMTP3.Consoles`)
- **Ansvar:** Håndterer interaktion med brugeren via kommandolinjen.
- **Logik:**
    1. Fortolker brugerens input for at identificere den *enkelte* kilde, der skal backes up (f.eks. via `--device "MyPhone"`).
    2. Opretter det korrekte `ISourceConfig`-objekt.
    3. Injekterer og kalder `BackupController.ExecuteBackup()` med dette objekt.

## Dataflow

En typisk backup-operation vil følge dette flow:

`Bruger input` -> `[BackupConsoleCommand]` -> `[BackupController]` -> `(DI-opslag)` -> `[Device/DriveBackupHandler]` -> `Udfører backup`

## Dependency Injection Opsætning

I `Startup/Configurations/ApplicationServiceSetup.cs` skal følgende services registreres:

```csharp
// Controlleren registreres som transient.
services.AddTransient<BackupController>();

// Handlerne registreres mod deres specifikke generiske interface.
services.AddTransient<IBackupHandler<DeviceSourceConfig>, DeviceBackupHandler>();
services.AddTransient<IBackupHandler<DriveSourceConfig>, DriveBackupHandler>();
```

## Forældede Komponenter

Den gamle `BackupMaster`-klasse, især dens `StartBackup`-metode, der itererer over flere kilder, skal betragtes som forældet og bør på sigt udfases. Den nye `BackupController` overtager ansvaret for at initiere backup-logikken.
