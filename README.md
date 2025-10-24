# BMTP3

BMTP3 stands for **Backup Media Transfer Protocol**.

This is the third major implementation of the backup program:
- The first version was written in legacy C#.
- The second version was implemented in Java with DLL support for MTP.
- BMTP3 is now re-implemented using modern C# (.NET 8).

## Overview

BMTP3 is a reliable and extensible backup solution for Windows, designed to securely back up data from a wide range of devices.
Supported devices include smartphones, tablets, cameras, and external drives.  

Device communication is handled via popular protocols:
- **MTP (Media Transfer Protocol)**
- **PTP (Picture Transfer Protocol)**
- **MSC (Mass Storage Class)**

BMTP3 provides a modern command-line interface (CLI) with advanced options, progress reporting, and verification features. 
The architecture is modular, making it easy to extend and customize for different backup scenarios.

## Key Features

- Backup files and folders from multiple device types
- Verify backup integrity and data consistency
- Modular architecture for easy extension and customization
- Progress bars and status reporting in the console
- Support for .NET 8 and C# 12 features
- Dependency injection for testability and scalability
- Configuration via TOML files for flexible setup
- Extensible backup jobs for portable devices and drives

## Who Should Use BMTP3?

BMTP3 is ideal for:
- Individuals seeking a reliable backup solution for their mobile devices
- Power users and IT professionals needing reliable device backups
- Developers looking for a customizable backup framework
- Anyone who wants to automate and verify backups from mobile devices and external media

## Quick Start

1. **Install .NET 8**  
   Download and install the latest .NET 8 SDK from [dotnet.microsoft.com](https://dotnet.microsoft.com/download).

2. **Clone the repository**  
   `git clone https://github.com/preuss/BMTP3_CS.git`

3. **Build the project**  
   `dotnet build`

4. **Run the application**  
   See documentation for usage details.

## License

This project is licensed under the terms of the [GNU Affero General Public License v3.0](https://www.gnu.org/licenses/agpl-3.0.html).  
See [LICENSE.md](LICENSE.md) for full license details.

## Contributor License Agreement (CLA)

By contributing to this project, you agree that your contributions are licensed under the AGPL-3.0 license, and you grant the project owner the right to relicense your contributions under other terms in the future.  
See [CLA.md](CLA.md) for details.

## Download

Future: [NuGet Package](https://www.nuget.org/packages/MediaDevices/)

## Documentation

See `README.md` and project source for usage and API details.  
Update documentation as new features are added.

## Donate

You are welcome to support this project. 

[![Donate](https://raw.githubusercontent.com/Bassman2/MediaDevices/master/.github/images/donate.gif)](https://www.paypal.me/GBassman)
