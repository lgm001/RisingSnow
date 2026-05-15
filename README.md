# RisingSnow

WPF **rising snow** background: a small **`RisingSnow.Wpf`** class library you can reference from other desktop apps, plus this **`RisingSnow`** sample executable.

**Repository:** [github.com/lgm001/RisingSnow](https://github.com/lgm001/RisingSnow)

Licensed under the [MIT License](LICENSE).

## Repository layout

| Path | Purpose |
|------|---------|
| [`RisingSnow.Wpf/`](RisingSnow.Wpf/) | Reusable library: `SnowCanvas` (`FrameworkElement`) and `RisingSnowBackground` (`UserControl`) for full-bleed backgrounds. |
| [`RisingSnow/`](RisingSnow/) | Demo host (`MainWindow`) referencing the library. |
| [`.ai/`](.ai/) | Archived Cursor plan notes (not required at runtime). |

**Version:** `1.0.0` (assembly / package metadata on both projects).

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) with **.NET 10** (`net10.0-windows`).

## Editor workspace

Open [`RisingSnow.code-workspace`](RisingSnow.code-workspace) in **Visual Studio Code** or **Cursor** to load the repo root (solution `RisingSnow.slnx`, projects under `RisingSnow/` and `RisingSnow.Wpf/`).

## Build and run the demo

```powershell
dotnet build RisingSnow.slnx -c Release
dotnet run --project RisingSnow/RisingSnow.csproj
```

## Use as a background in another WPF app

1. Add a **project reference** to `RisingSnow.Wpf/RisingSnow.Wpf.csproj` (or pack the library and use `PackageReference` if you publish a NuGet feed).

2. In XAML, declare the XML namespace and place the control **behind** your content (lower `Panel.ZIndex` or declare it first in the visual tree):

```xml
<Window ...
        xmlns:rs="http://schemas.risingsnow.dev/wpf">
  <Grid>
    <rs:RisingSnowBackground Panel.ZIndex="0"
                             HorizontalAlignment="Stretch"
                             VerticalAlignment="Stretch" />
    <Grid Panel.ZIndex="1" Background="Transparent">
      <!-- Your UI -->
    </Grid>
  </Grid>
</Window>
```

3. Drive metrics from code or a view model via the hosted canvas:

```csharp
SnowBackground.Snow.SetBinding(
    SnowCanvas.SpawnRateProperty,
    new Binding("DashboardFlakesPerSec") { Source = viewModel });
```

Or set `SnowBackground.Snow.Intensity`, `SpawnRate`, `SpeedMultiplier`, etc. directly.

### `SnowCanvas` dependency properties

| Property | Role |
|----------|------|
| `IsRunning` | When `false`, simulation pauses (flakes freeze). |
| `SpawnRate` | Target flakes per second (before `Intensity`). |
| `Intensity` | **0–1**; scales effective spawn rate and rise speed. |
| `SpeedMultiplier` | Scales initial upward velocity and acceleration for new flakes. |
| `DriftScale` | Scales horizontal wander amplitude and frequency. |
| `MaxParticles` | Upper bound on concurrent flakes. |
| `MaxLifetimeSeconds` | Removes flakes that live too long. |
| `FlakeColor` / `BackgroundColor` | Visual tuning (set background transparent if the parent should show through). |
| `SpawnBandMin` / `SpawnBandMax` | Fraction of height from the bottom for spawning (defaults **0–1%**). |

## Changelog

See [CHANGELOG.md](CHANGELOG.md).
