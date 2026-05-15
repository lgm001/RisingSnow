# RisingSnow

WPF demo of a **rising snow** particle field: flakes spawn in a bottom band, accelerate upward with gentle sinusoidal drift, and are drawn in `OnRender` (same architectural pattern as `ParticlesCanvas` in the **Particles.WPF** companion project: `FrameworkElement` + dispatcher timer + `DrawingContext`).

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) with **.NET 10** (`net10.0-windows`).

## Build and run

```powershell
dotnet build RisingSnow.slnx -c Release
dotnet run --project RisingSnow/RisingSnow.csproj
```

## `SnowCanvas` and dashboard-style binding

[`SnowCanvas`](RisingSnow/SnowCanvas.cs) exposes **dependency properties** so you can drive the effect from a view model or external metric later:

| Property | Role |
|----------|------|
| `IsRunning` | When `false`, simulation pauses (existing flakes freeze). |
| `SpawnRate` | Target flakes per second (before `Intensity`). |
| `Intensity` | **0–1**; scales effective spawn rate (`SpawnRate * Intensity`) and rise speed (`SpeedMultiplier * max(Intensity, 0))`. Use a single bound metric for “how much snow”. |
| `SpeedMultiplier` | Scales initial upward velocity and acceleration for new flakes. |
| `DriftScale` | Scales horizontal wander amplitude and frequency. |
| `MaxParticles` | Upper bound on concurrent flakes. |
| `MaxLifetimeSeconds` | Removes flakes that live too long. |
| `FlakeColor` / `BackgroundColor` | Visual tuning. |
| `SpawnBandMin` / `SpawnBandMax` | Fraction of height from the bottom defining the spawn band (default 10–15%). |

Example binding (when you add a `DataContext`):

```xml
<local:SnowCanvas SpawnRate="60"
                  Intensity="{Binding DashboardLoad01}" />
```

## Changelog

See [CHANGELOG.md](CHANGELOG.md).
