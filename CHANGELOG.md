# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-05-15

### Added

- **`RisingSnow.Wpf`** class library (`net10.0-windows`): `SnowCanvas`, `RisingSnowBackground` `UserControl`, XML namespace `http://schemas.risingsnow.dev/wpf` (`rs` prefix) for reuse as a full-bleed background layer.
- **MIT** [`LICENSE`](LICENSE) at repository root.
- **`.ai/risingsnow-wpf-particles-plan.md`**: archived Cursor implementation plan.

### Changed

- **`RisingSnow` demo app** references `RisingSnow.Wpf` and hosts `rs:RisingSnowBackground` instead of in-app `SnowCanvas`.
- Solution [`RisingSnow.slnx`](RisingSnow.slnx) lists the library project first, then the executable.
- Root **README** documents layout, licensing, and how to consume the control from other WPF projects.

### Build

- `dotnet build RisingSnow.slnx -c Release` succeeded for the split solution.
- Initial GitHub publish: [https://github.com/lgm001/RisingSnow](https://github.com/lgm001/RisingSnow) (`413010f`).

## Earlier development (pre-1.0.0 tag)

The following shipped incrementally on `master` before this changelog section was consolidated for **1.0.0**:

### Changed

- `SnowCanvas`: default spawn band is the **bottom 1%** of height; flakes are **larger** with a **soft multi-layer glow**.

### Fixed

- `Intensity` dependency property: values outside 0–1 are **coerced** (non-finite fall back to 1).

### Added

- `SnowCanvas`: `FrameworkElement` rising-snow simulation (`DispatcherTimer` + wall-clock `dt`, spawn/cull, `OnRender` ellipses) modeled after the Particles.WPF canvas pattern.
- Dependency properties for runtime and dashboard-style binding.

### Build

- `dotnet build RisingSnow.slnx -c Release` after glow/spawn tweak: commit `f3bb75a`.
- Initial SnowCanvas and docs: commit `8e0ca8c`.
