# Chocolatey Package Builder

Chocolatey Package Builder is an early-stage .NET project for generating Chocolatey packages from Windows installer files and saved custom installer projects. It currently includes a shared packaging core, a Spectre.Console-based CLI, an Avalonia GUI, and a launcher executable that chooses between them.

This project is very early in development. Expect rough edges and changing command/UI behavior. Generated scripts should be reviewed before distribution.

## Current capabilities

- Detects common installer types:
  - MSI
  - Inno Setup
  - NSIS
- Generates `tools/chocolateyInstall.ps1` with installer-specific silent arguments.
- Builds `.nupkg` packages directly when the installer type is known.
- Creates scaffolded package templates when the installer type is unknown or when manual review is preferred.
- Packs a reviewed scaffold directory into a `.nupkg`.
- Saves `.cpbproj` custom installer projects containing package metadata, bundled files, and ordered install actions.
- Builds custom project packages with generated scripts for copying files and starting files with arguments.
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
dotnet run --project ChocolateyPackageBuilder -- pack path/to/package-template --output ./artifacts
```

### Custom installer projects

The GUI's **Custom project** tab is the primary workflow for project-based installers. Create or open a `.cpbproj`, add files to the project, stack actions such as copying a bundled file or running a file with arguments, then save and build the project package.

Saved projects are portable: GUI-added files are copied under the project `files/` directory, while literal action paths such as `%ProgramFiles%\Vendor\app.exe` are evaluated on the installing computer.

Pack a saved project from the CLI:

```bash
dotnet run --project ChocolateyPackageBuilder -- pack path/to/project.cpbproj --output ./artifacts
```

## GUI

The GUI opens on the custom project editor for action-based installers. The **Quick installer** tab keeps the single-installer workflow for selecting an installer, detecting its type, previewing the generated Chocolatey install script, and building a package or scaffold.

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
