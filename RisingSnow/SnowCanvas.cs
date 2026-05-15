using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace RisingSnow;

/// <summary>
/// Rising snow: soft flakes spawn along the bottom band, accelerate upward, drift horizontally, and cull off the top.
/// Pattern matches Particles.WPF <c>ParticlesCanvas</c>: <see cref="FrameworkElement"/>, <see cref="DispatcherTimer"/>, <see cref="OnRender"/>.
/// Expose <see cref="DependencyProperty"/> values for future binding to dashboard metrics (e.g. <see cref="Intensity"/>, <see cref="SpawnRate"/>).
/// </summary>
public sealed class SnowCanvas : FrameworkElement
{
    private readonly List<Snowflake> _flakes = [];
    private readonly Random _rng = new();
    private DispatcherTimer? _timer;
    private bool _loaded;
    private DateTime _lastTickUtc;
    private double _totalTime;
    private double _spawnAccumulator;

    public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
        nameof(IsRunning),
        typeof(bool),
        typeof(SnowCanvas),
        new FrameworkPropertyMetadata(true));

    public static readonly DependencyProperty SpawnRateProperty = DependencyProperty.Register(
        nameof(SpawnRate),
        typeof(double),
        typeof(SnowCanvas),
        new FrameworkPropertyMetadata(40.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// Normalized 0–1 scalar applied to <see cref="SpawnRate"/> and <see cref="SpeedMultiplier"/> so a single metric can modulate the effect.
    /// Default 1 = use base <see cref="SpawnRate"/> and <see cref="SpeedMultiplier"/> as-is.
    /// </summary>
    public static readonly DependencyProperty IntensityProperty = DependencyProperty.Register(
        nameof(Intensity),
        typeof(double),
        typeof(SnowCanvas),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, null, CoerceIntensity));

    public static readonly DependencyProperty SpeedMultiplierProperty = DependencyProperty.Register(
        nameof(SpeedMultiplier),
        typeof(double),
        typeof(SnowCanvas),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DriftScaleProperty = DependencyProperty.Register(
        nameof(DriftScale),
        typeof(double),
        typeof(SnowCanvas),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MaxParticlesProperty = DependencyProperty.Register(
        nameof(MaxParticles),
        typeof(int),
        typeof(SnowCanvas),
        new FrameworkPropertyMetadata(400, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MaxLifetimeSecondsProperty = DependencyProperty.Register(
        nameof(MaxLifetimeSeconds),
        typeof(double),
        typeof(SnowCanvas),
        new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FlakeColorProperty = DependencyProperty.Register(
        nameof(FlakeColor),
        typeof(Color),
        typeof(SnowCanvas),
        new FrameworkPropertyMetadata(Color.FromRgb(0xf5, 0xfa, 0xff), OnVisualDpChanged));

    public static readonly DependencyProperty BackgroundColorProperty = DependencyProperty.Register(
        nameof(BackgroundColor),
        typeof(Color),
        typeof(SnowCanvas),
        new FrameworkPropertyMetadata(Color.FromRgb(0x0a, 0x0f, 0x18), OnVisualDpChanged));

    public static readonly DependencyProperty SpawnBandMinProperty = DependencyProperty.Register(
        nameof(SpawnBandMin),
        typeof(double),
        typeof(SnowCanvas),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender),
        ValidateBandFraction);

    public static readonly DependencyProperty SpawnBandMaxProperty = DependencyProperty.Register(
        nameof(SpawnBandMax),
        typeof(double),
        typeof(SnowCanvas),
        new FrameworkPropertyMetadata(0.01, FrameworkPropertyMetadataOptions.AffectsRender),
        ValidateBandFraction);

    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public double SpawnRate
    {
        get => (double)GetValue(SpawnRateProperty);
        set => SetValue(SpawnRateProperty, value);
    }

    /// <summary>0–1; multiplies effective spawn rate and speed. Bind a dashboard channel here for a single lever.</summary>
    public double Intensity
    {
        get => (double)GetValue(IntensityProperty);
        set => SetValue(IntensityProperty, value);
    }

    public double SpeedMultiplier
    {
        get => (double)GetValue(SpeedMultiplierProperty);
        set => SetValue(SpeedMultiplierProperty, value);
    }

    public double DriftScale
    {
        get => (double)GetValue(DriftScaleProperty);
        set => SetValue(DriftScaleProperty, value);
    }

    public int MaxParticles
    {
        get => (int)GetValue(MaxParticlesProperty);
        set => SetValue(MaxParticlesProperty, value);
    }

    public double MaxLifetimeSeconds
    {
        get => (double)GetValue(MaxLifetimeSecondsProperty);
        set => SetValue(MaxLifetimeSecondsProperty, value);
    }

    public Color FlakeColor
    {
        get => (Color)GetValue(FlakeColorProperty);
        set => SetValue(FlakeColorProperty, value);
    }

    public Color BackgroundColor
    {
        get => (Color)GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    /// <summary>Lower edge of spawn band as fraction of height from bottom (e.g. 0 = viewport bottom).</summary>
    public double SpawnBandMin
    {
        get => (double)GetValue(SpawnBandMinProperty);
        set => SetValue(SpawnBandMinProperty, value);
    }

    /// <summary>Upper edge of spawn band as fraction of height from bottom (e.g. 0.01 = top of a 1% band).</summary>
    public double SpawnBandMax
    {
        get => (double)GetValue(SpawnBandMaxProperty);
        set => SetValue(SpawnBandMaxProperty, value);
    }

    public SnowCanvas()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private static bool ValidateBandFraction(object value) =>
        value is double d && d >= 0 && d <= 1;

    private static object CoerceIntensity(DependencyObject d, object baseValue)
    {
        if (baseValue is not double v || double.IsNaN(v) || double.IsInfinity(v))
            return 1.0;
        return Math.Clamp(v, 0, 1);
    }

    private static void OnVisualDpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((SnowCanvas)d).InvalidateVisual();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _loaded = true;
        _lastTickUtc = DateTime.UtcNow;
        _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
        InvalidateVisual();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _loaded = false;
        if (_timer != null)
        {
            _timer.Stop();
            _timer = null;
        }
    }

    private void Tick()
    {
        if (!_loaded)
            return;

        double w = ActualWidth;
        double h = ActualHeight;
        if (w < 1 || h < 1)
            return;

        DateTime now = DateTime.UtcNow;
        double dt = (now - _lastTickUtc).TotalSeconds;
        _lastTickUtc = now;
        if (dt <= 0 || dt > 0.25)
            dt = Math.Min(dt > 0 ? dt : 1.0 / 60.0, 0.25);

        if (IsRunning)
        {
            _totalTime += dt;

            double intensity = Intensity;
            double effectiveSpawn = SpawnRate * intensity;
            double speedMul = SpeedMultiplier * Math.Max(0, intensity);

            _spawnAccumulator += effectiveSpawn * dt;
            int toSpawn = (int)Math.Floor(_spawnAccumulator);
            _spawnAccumulator -= toSpawn;

            int cap = Math.Max(0, MaxParticles);
            for (int i = 0; i < toSpawn && _flakes.Count < cap; i++)
                _flakes.Add(SpawnFlake(w, h, speedMul));

            double maxLife = Math.Max(0.1, MaxLifetimeSeconds);
            double driftScale = Math.Max(0, DriftScale);

            for (int i = _flakes.Count - 1; i >= 0; i--)
            {
                Snowflake s = _flakes[i];
                s.Vy += s.Ay * dt;
                s.Y -= s.Vy * dt;
                s.X = s.SpawnX + Math.Sin(_totalTime * s.DriftFreq * driftScale + s.Phase) * (s.DriftAmp * driftScale);

                double age = (now - s.BornUtc).TotalSeconds;
                if (s.Y + s.Radius < 0 || age > maxLife)
                    _flakes.RemoveAt(i);
            }
        }

        InvalidateVisual();
    }

    private Snowflake SpawnFlake(double w, double h, double speedMul)
    {
        double bandLo = Math.Clamp(Math.Min(SpawnBandMin, SpawnBandMax), 0, 1);
        double bandHi = Math.Clamp(Math.Max(SpawnBandMin, SpawnBandMax), 0, 1);
        double yMin = h * (1 - bandHi);
        double yMax = h * (1 - bandLo);
        if (yMax <= yMin)
            yMin = Math.Max(0, h * (1 - bandHi) - 1);

        double x0 = _rng.NextDouble() * w;
        double y0 = yMin + _rng.NextDouble() * (yMax - yMin);

        double vy0 = (18 + _rng.NextDouble() * 16) * speedMul;
        double ay = (10 + _rng.NextDouble() * 32) * speedMul;

        // Radius ~1.1–2.75 device-independent units (slightly larger than first revision).
        double r = (2.25 + _rng.NextDouble() * 3.25) / 2;
        double op = 0.4 + _rng.NextDouble() * 0.5;

        double driftAmp = 0.5 + _rng.NextDouble() * 1.5;
        double driftFreq = 0.2 + _rng.NextDouble() * 0.4;
        double phase = _rng.NextDouble() * Math.PI * 2;

        return new Snowflake(x0, y0, vy0, ay, x0, phase, driftAmp, driftFreq, r, op, DateTime.UtcNow);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = ActualWidth;
        double h = ActualHeight;
        if (w < 1 || h < 1)
            return;

        dc.DrawRectangle(new SolidColorBrush(BackgroundColor), null, new Rect(0, 0, w, h));

        Color baseColor = FlakeColor;
        foreach (Snowflake s in _flakes)
            DrawGlowingFlake(dc, new Point(s.X, s.Y), s.Radius, s.Opacity, baseColor);
    }

    /// <summary>Soft multi-layer disc (no bitmap effects) for a subtle glow around each flake.</summary>
    private static void DrawGlowingFlake(DrawingContext dc, Point center, double radius, double opacity, Color rgb)
    {
        static SolidColorBrush Brush(byte a, Color c) =>
            new(Color.FromArgb(a, c.R, c.G, c.B));

        double o = Math.Clamp(opacity, 0, 1);
        Point p = center;

        // Wide, faint halo
        byte a0 = (byte)Math.Clamp((int)Math.Round(255 * o * 0.10), 0, 255);
        dc.DrawEllipse(Brush(a0, rgb), null, p, radius * 1.65, radius * 1.65);

        byte a1 = (byte)Math.Clamp((int)Math.Round(255 * o * 0.18), 0, 255);
        dc.DrawEllipse(Brush(a1, rgb), null, p, radius * 1.35, radius * 1.35);

        byte a2 = (byte)Math.Clamp((int)Math.Round(255 * o * 0.42), 0, 255);
        dc.DrawEllipse(Brush(a2, rgb), null, p, radius * 1.12, radius * 1.12);

        byte a3 = (byte)Math.Clamp((int)Math.Round(255 * o), 0, 255);
        dc.DrawEllipse(Brush(a3, rgb), null, p, radius, radius);
    }

    private sealed class Snowflake(
        double x,
        double y,
        double vy,
        double ay,
        double spawnX,
        double phase,
        double driftAmp,
        double driftFreq,
        double radius,
        double opacity,
        DateTime bornUtc)
    {
        public double X = x;
        public double Y = y;
        public double Vy = vy;
        public double Ay = ay;
        public readonly double SpawnX = spawnX;
        public readonly double Phase = phase;
        public readonly double DriftAmp = driftAmp;
        public readonly double DriftFreq = driftFreq;
        public readonly double Radius = radius;
        public readonly double Opacity = opacity;
        public readonly DateTime BornUtc = bornUtc;
    }
}
