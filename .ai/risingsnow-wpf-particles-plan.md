# Rising snow control (Particles.WPF pattern)

_Archived copy of the implementation plan (Cursor). Repository layout has since been extended with `RisingSnow.Wpf` and `RisingSnowBackground`; see README and CHANGELOG._

## Reference pattern (already in your ecosystem)

[`ParticlesCanvas`](C:\repos\Particles.WPF\WPFParticlesControl\ParticlesCanvas.cs) is the template:

- **`FrameworkElement`** subclass (not thousands of `Ellipse` children).
- **`DispatcherTimer`** (`DispatcherPriority.Render`, ~16ms) in `Loaded` / stopped in `Unloaded`.
- **`OnRender`** draws background + primitives (`DrawEllipse`) with `SolidColorBrush` + per-dot alpha.
- **State** lives in a private `Particle` (or equivalent) list.
- **External control** via **`DependencyProperty`** so XAML / `Binding` / code-behind can drive behavior later.

[`MainWindow.xaml`](C:\repos\Particles.WPF\Particles.WPF\MainWindow.xaml) only hosts the control full-bleed with `ClipToBounds="True"`.

RisingSnow today is an empty shell: [`MainWindow.xaml`](c:\repos\RisingSnow\RisingSnow\MainWindow.xaml) + [`MainWindow.xaml.cs`](c:\repos\RisingSnow\RisingSnow\MainWindow.xaml.cs). No need to reference `Particles.WPF`; **reuse the architecture**, not the package.

## Implementation plan

### 1. Add `SnowCanvas` (new file in RisingSnow project)

Create something like [`RisingSnow/SnowCanvas.cs`](c:\repos\RisingSnow\RisingSnow\SnowCanvas.cs) (name flexible) as `public sealed class SnowCanvas : FrameworkElement`.

**Simulation (rising snow, dt-based):**

- Maintain `List<Snowflake>` with: `X`, `Y`, `Vy`, `Ay`, `SpawnX`, drift phase/frequency/amplitude, `BornUtc`, `Radius`, `Opacity`.
- Each tick: compute `dt` from **wall clock** between ticks (not assumed 1/60) so motion stays stable if the UI thread stalls.
- Integrate: `Vy += Ay * dt`; `Y -= Vy * dt` (screen coords, **up** is decreasing `Y`).
- Horizontal position: `X = SpawnX + sin(totalTime * freq + phase) * amp` (use a monotonic `totalTime` accumulator += `dt`).
- **Spawn:** accumulate `SpawnRate * dt`; each whole unit spawns one flake with randomized parameters within clamped ranges (bottom band 10–15% of `ActualHeight`, random `X` across `ActualWidth`).
- **Cull:** remove when `Y + Radius < 0` or age exceeds `MaxLifetimeSeconds`.
- **Cap:** enforce `MaxParticles` to avoid runaway `Children`/list growth (unlike linked particles, snow is append-heavy).

**Rendering:**

- `OnRender`: optional solid background brush DP, then draw each flake as `DrawEllipse` with `Color.FromArgb(a, r, g, b)` matching your “soft white / slight blue” spec.
- Call `InvalidateVisual()` at end of tick (same cadence as [`ParticlesCanvas.Tick`](C:\repos\Particles.WPF\WPFParticlesControl\ParticlesCanvas.cs)).

**Lifecycle:** mirror `Loaded` / `Unloaded` / `SizeChanged` from `ParticlesCanvas` (start/stop timer; on size change you may only need to clamp or clear if dimensions collapse—avoid full clear unless you want a hard reset).

### 2. DependencyProperties for “dashboard metric” readiness

Expose a **small, opinionated** set of DPs (all `FrameworkPropertyMetadata` with callbacks where needed):

| Property | Role |
|----------|------|
| `IsEnabled` or `IsRunning` | Pause spawning + ticking without unloading |
| `SpawnRate` (flakes/sec) | Primary lever for a future metric |
| `SpeedMultiplier` | Scales `v0` / `ay` together or separately (pick one clear model) |
| `DriftScale` | Multiplier on drift amplitude and/or frequency |
| `MaxParticles` | Hard cap |
| `MaxLifetimeSeconds` | Cull old flakes |
| `SnowTint` / `FlakeColor` + `BackgroundColor` | Visual tuning |

Optional convenience: `Intensity` (0–1) **implemented inside** the control as a normalized input that maps to `SpawnRate` and/or `SpeedMultiplier` so a single dashboard channel can drive “more snow / faster rise” without multiple bindings—document the mapping in XML comments.

Use `OnPropertyChanged` callbacks to `InvalidateVisual()` for visual-only DPs; for sim parameters, no full particle rebuild required unless you change semantics drastically.

### 3. Wire into the app shell

Update [`MainWindow.xaml`](c:\repos\RisingSnow\RisingSnow\MainWindow.xaml):

- `xmlns:local="clr-namespace:RisingSnow"`.
- Root `Grid` with `local:SnowCanvas` filling the client area (`HorizontalAlignment="Stretch"` `VerticalAlignment="Stretch"`), `ClipToBounds="True"`, sensible dark `Background` on the window or control DP.

[`MainWindow.xaml.cs`](c:\repos\RisingSnow\RisingSnow\MainWindow.xaml.cs) can stay minimal (optional: set a `Name` on `SnowCanvas` and assign a test `SpawnRate` in ctor for demo—only if you want immediate motion without a VM).

### 4. Future dashboard hook (design only in this pass)

Next step after the control exists: introduce a **`ViewModel`** with a metric property and bind `SpawnRate` / `Intensity` / `SpeedMultiplier` to it. No TCP/HTTP in this task unless you explicitly want it—**binding surface is the integration point**.

### 5. Docs and verification

- Update [`README.md`](c:\repos\RisingSnow\README.md) (if present) with how to tune DPs and where to bind metrics.
- Append [`CHANGELOG.md`](c:\repos\RisingSnow\CHANGELOG.md) under Unreleased for the new effect + binding-oriented DPs.
- Run `dotnet build RisingSnow.slnx -c Release` after edits.

## Deliberate deviations from `ParticlesCanvas`

- **Growing/shrinking list** instead of fixed-count `RebuildIfReady` (snow spawns continuously).
- **Delta-time integration** instead of `+= Vx` per tick without `dt` (better for bound metrics and UI thread jitter).
- **No line graph / O(n²) links**—only flakes, so render stays cheap at moderate counts.

## Out of scope (per your constraints)

- WebView2, WebGL, `Viewport3D`, third-party particle libraries.
