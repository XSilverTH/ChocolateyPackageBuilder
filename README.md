# Chocolatey Package Builder

Chocolatey Package Builder is an early-stage .NET project for generating Chocolatey packages from Windows installer files. It currently includes a shared packaging core, a Spectre.Console-based CLI, an Avalonia GUI, and a launcher executable that chooses between them.

This project is very early in development. Expect rough edges, missing workflows, and changing command/UI behavior. More features and polish are coming soon.

## Current capabilities

- Detects common installer types:
  - MSI
  - Inno Setup
  - NSIS
- Generates `tools/chocolateyInstall.ps1` with installer-specific silent arguments.
- Builds `.nupkg` packages directly when the installer type is known.
- Creates scaffolded package templates when the installer type is unknown or when manual review is preferred.
- Packs a reviewed scaffold directory into a `.nupkg`.
- Provides one launcher executable: starts the GUI with no arguments, or routes arguments to the CLI.

## Requirements

- .NET 10 SDK
- Chocolatey knowledge for reviewing generated package scripts before distribution

## Build

From the repository root:

```bash
dotnet build ChocolateyPackageBuilder.slnx
```

## Usage

The `ChocolateyPackageBuilder` launcher is the main entry point. It starts the GUI when run without arguments and runs the CLI when arguments are provided.

Start the GUI:

```bash
dotnet run --project ChocolateyPackageBuilder
```

Run a CLI command through the launcher:

```bash
dotnet run --project ChocolateyPackageBuilder -- build path/to/installer.exe \
  --name example-package \
  --version 1.0.0 \
  --maintainer "Your Name" \
  --description "Chocolatey package for Example." \
  --output ./artifacts
```

If you run the launcher with `build` but omit the installer path, the CLI starts an interactive prompt.

### Installer type

By default, `build` uses automatic detection:

```bash
dotnet run --project ChocolateyPackageBuilder -- build path/to/installer.exe --type auto
```

Supported values:

- `auto`
- `msi`
- `inno`
- `nsis`
- `scaffold`

Use `scaffold` when you want a template directory instead of a direct package build:

```bash
dotnet run --project ChocolateyPackageBuilder -- build path/to/installer.exe --type scaffold
```

Review the generated `tools/chocolateyInstall.ps1`, then pack the scaffold:

```bash
dotnet run --project ChocolateyPackageBuilder -- pack path/to/package-template
```

## GUI

The GUI currently focuses on selecting an installer, detecting its type, previewing the generated Chocolatey install script, and building a package or scaffold. Launch it by running `ChocolateyPackageBuilder` without arguments.

## Project layout

```text
ChocolateyPackageBuilder/       Launcher executable; GUI with no args, CLI with args
ChocolateyPackageBuilder.Core/  Shared package generation and installer detection logic
ChocolateyPackageBuilder.Cli/   Command-line interface
ChocolateyPackageBuilder.Gui/   Avalonia desktop interface
```

## Development status

This repository is under active, early development. Generated scripts should be reviewed before use. Expect additional installer support, packaging options, and UI improvements in future updates.

## License

This project is licensed under the terms in [LICENSE](LICENSE).
