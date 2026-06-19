# Chocolatey Package Builder

> **⚠️ Note:** This project is in its very early stages and more installer workflows are planned.

ChocolateyPackageBuilder is a .NET 10 Avalonia application for creating Chocolatey packages. Run it with no arguments to open the GUI, or pass arguments to use the CLI. It detects MSI, Inno Setup, and NSIS installers, generates `chocolateyInstall.ps1`, and either builds a `.nupkg` directly or creates a scaffold for manual review.

## Features

* **Avalonia GUI:** A SukiUI-styled package builder for selecting an installer, editing metadata, previewing the generated install script, and building output.
* **CLI mode:** The same executable acts as a command-line app when arguments are provided.
* **Installer detection:** Detects Inno Setup, NSIS, and MSI installers via binary and signature analysis.
* **Script generation:** Generates Chocolatey install scripts with installer-specific silent arguments.
* **Auto-packaging:** Builds `.nupkg` files directly using `NuGet.Packaging` without requiring the Chocolatey CLI.
* **Scaffolding:** Creates a template directory with a `.nuspec`, installer, and script when the installer type is unknown or manual edits are requested.
* **Packager:** Packs scaffolded directories into final `.nupkg` files.

## Getting Started

### Prerequisites

* [.NET 10.0 SDK](https://dotnet.microsoft.com/)

### Build

```bash
dotnet build ChocolateyPackageBuilder.slnx
```

## Usage

### GUI

Run without arguments:

```bash
dotnet run --project ChocolateyPackageBuilder.App
```

The GUI opens the package-builder workflow. Pick an installer, confirm or override the installer type, edit package metadata, review the generated script, and build the package or scaffold.

### CLI

Run with arguments:

```bash
dotnet run --project ChocolateyPackageBuilder.App -- <command> [options]
```

#### Build a package or scaffold

```bash
dotnet run --project ChocolateyPackageBuilder.App -- build <installerPath> [options]
```

Options:

* `-n, --name <NAME>`: Package name. Defaults to a Chocolatey-safe slug from the installer filename.
* `-v, --version <VERSION>`: Package version. Defaults to `1.0.0`.
* `-m, --maintainer <MAINTAINER>`: Package maintainer. Defaults to the current OS user, or `Unknown`.
* `-d, --description <DESCRIPTION>`: Package description. Defaults to `Chocolatey package for <name>.`.
* `-o, --output <OUTPUT>`: Output directory. Defaults to the current directory.
* `-t, --type <TYPE>`: Installer type: `auto`, `msi`, `inno`, `nsis`, or `scaffold`. Defaults to `auto`.

`--type auto` detects the installer. If detection returns unknown, the command does not prompt; it creates a scaffold and warns that silent arguments need review. Use `--type scaffold` to force scaffold output.

Example:

```bash
dotnet run --project ChocolateyPackageBuilder.App -- build ./setup.exe -n my-software -v 2.0.0 -m Dev --type auto
```

#### Pack a scaffolded template

After editing a scaffolded `.nuspec` or `tools/chocolateyInstall.ps1`, pack the directory into a `.nupkg`:

```bash
dotnet run --project ChocolateyPackageBuilder.App -- pack <directoryPath>
```

Example:

```bash
dotnet run --project ChocolateyPackageBuilder.App -- pack ./my-software-template
```
