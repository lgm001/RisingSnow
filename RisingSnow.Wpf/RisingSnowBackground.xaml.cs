using System.Windows.Controls;

namespace RisingSnow.Wpf;

/// <summary>
/// Drop-in rising-snow layer for use behind other UI (full-bleed <see cref="SnowCanvas"/>).
/// Bind or set properties on <see cref="Snow"/> from code (e.g. dashboard metrics).
/// </summary>
public partial class RisingSnowBackground : UserControl
{
    public RisingSnowBackground() => InitializeComponent();

    /// <summary>The hosted simulation surface.</summary>
    public SnowCanvas Snow => SnowHost;
}
