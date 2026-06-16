# Chocolatey Package Builder

> **⚠️ Note:** This project is in its very very early stages and a lot more is coming soon!

ChocolateyPackageBuilder is a .NET 10 command-line tool designed to streamline the creation of Chocolatey packages. It analyzes installers, determines their type (just MSI, Inno Setup and NSIS for now), generates the appropriate PowerShell installation scripts, and compiles them directly into `.nupkg` files or scaffolds a project template for further customization.

## Features

* **Installer Detection:** Automatically detects InnoSetup, NSIS, and MSI installers via binary and signature analysis.
* **Script Generation:** Generates the `chocolateyInstall.ps1` script with the correct silent arguments based on the installer type.
* **Auto-Packaging:** Builds `.nupkg` files directly using `NuGet.Packaging` without requiring the Chocolatey CLI.
* **Scaffolding:** Generates a template directory with a `.nuspec` and installation script for manual tweaking when the installer type is unknown or needs custom logic.
* **Packager:** Easily pack your modified scaffolded directories into a final `.nupkg`.
* **AOT Ready:** Configured for Native AOT publishing (`PublishAot=true`) for fast startup times.

## Getting Started

### Prerequisites

* [.NET 10.0 SDK](https://dotnet.microsoft.com/)

### Building the Tool

Clone the repository and build using the .NET CLI:

```bash
dotnet build
```

## Usage

The tool features a CLI interface.

### 1. Build a Package

Generate a Chocolatey package directly from an installer file.

```bash
cpb build <installerPath> [options]
```

**Options:**

* `-n, --name <NAME>`: The name of the package (Defaults to the filename).
* `-v, --version <VERSION>`: The version of the package (Default: `1.0.0`).
* `-m, --maintainer <MAINTAINER>`: The maintainer of the package.
* `-d, --description <DESCRIPTION>`: The description of the package.
* `-o, --output <OUTPUT>`: The output directory for the generated `.nupkg` or scaffold (Defaults to the current directory).

*Example:*

```bash
cpb build ./setup.exe -n "MySoftware" -v "2.0.0" -m "Dev"
```

### 2. Pack a Scaffolded Template

If the installer type was unknown, or you opted to scaffold the template for manual editing, you can pack the directory into a `.nupkg` once you're done editing the `.nuspec` and `.ps1` script.

```bash
cpb pack <directoryPath>
```

*Example:*

```bash
cpb pack ./MySoftware-template
```
