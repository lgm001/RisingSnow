# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `SnowCanvas`: `FrameworkElement` rising-snow simulation (`DispatcherTimer` + wall-clock `dt`, spawn/cull, `OnRender` ellipses) modeled after the Particles.WPF canvas pattern.
- Dependency properties for runtime and future dashboard binding: `IsRunning`, `SpawnRate`, `Intensity`, `SpeedMultiplier`, `DriftScale`, `MaxParticles`, `MaxLifetimeSeconds`, `FlakeColor`, `BackgroundColor`, `SpawnBandMin`, `SpawnBandMax`.
- `MainWindow` hosts `SnowCanvas` full-bleed with defaults.

### Build

- Agent `dotnet build RisingSnow.slnx -c Release` succeeded.
- Commit: `8e0ca8c` (initial SnowCanvas and docs).
