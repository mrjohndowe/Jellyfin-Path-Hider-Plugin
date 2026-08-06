using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PathHider;

/// <summary>
/// Plugin configuration persisted by Jellyfin.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether scan filtering is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the newline-delimited hide rules.
    /// </summary>
    public string Rules { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether comparisons are case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether matching paths should be logged.
    /// </summary>
    public bool LogMatches { get; set; }
}
