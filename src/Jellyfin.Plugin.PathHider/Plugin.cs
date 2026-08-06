using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.PathHider;

/// <summary>
/// Jellyfin Path Hider plugin entry point.
/// </summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Stable plugin identifier.
    /// </summary>
    public static readonly Guid PluginId = Guid.Parse("96388552-61d4-4f91-a0ab-c72f32a864b1");

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the loaded plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override Guid Id => PluginId;

    /// <inheritdoc />
    public override string Name => "Path Hider";

    /// <inheritdoc />
    public override string Description =>
        "Excludes configured files and folders from Jellyfin library scans.";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            // A route-safe identifier is more reliable than a display name containing spaces.
            Name = "PathHiderConfig",

            // Use an explicit resource name so MSBuild and Jellyfin agree on the path.
            EmbeddedResourcePath =
                "Jellyfin.Plugin.PathHider.Configuration.configPage.html",

            // Also expose the page in the Dashboard menu. Jellyfin may still show a
            // Settings button on the plugin details page, depending on web-client version.
            EnableInMainMenu = true
        };
    }
}
