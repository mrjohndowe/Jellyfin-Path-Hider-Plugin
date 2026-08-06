using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PathHider;

/// <summary>
/// Prevents matching files and directories from entering Jellyfin's resolver pipeline.
/// </summary>
public sealed class ConfiguredPathIgnoreRule : IResolverIgnoreRule
{
    private readonly ILogger<ConfiguredPathIgnoreRule> _logger;
    private readonly object _cacheLock = new();

    private volatile MatcherCache _cache = new(null, false, PathRuleMatcher.Empty);

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfiguredPathIgnoreRule"/> class.
    /// </summary>
    public ConfiguredPathIgnoreRule(ILogger<ConfiguredPathIgnoreRule> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool ShouldIgnore(FileSystemMetadata fileInfo, BaseItem parent)
    {
        var configuration = Plugin.Instance?.Configuration;
        if (configuration is null || !configuration.Enabled || string.IsNullOrWhiteSpace(configuration.Rules))
        {
            return false;
        }

        var matcher = GetMatcher(configuration);
        if (!matcher.IsMatch(fileInfo.FullName, fileInfo.Name, fileInfo.IsDirectory, out var matchedRule))
        {
            return false;
        }

        if (configuration.LogMatches)
        {
            _logger.LogInformation(
                "Path Hider excluded {ItemType} '{Path}' using rule '{Rule}'",
                fileInfo.IsDirectory ? "folder" : "file",
                fileInfo.FullName,
                matchedRule);
        }

        return true;
    }

    private PathRuleMatcher GetMatcher(PluginConfiguration configuration)
    {
        var cache = _cache;
        if (cache.Matches(configuration))
        {
            return cache.Matcher;
        }

        lock (_cacheLock)
        {
            cache = _cache;
            if (!cache.Matches(configuration))
            {
                var matcher = PathRuleMatcher.Compile(
                    configuration.Rules,
                    configuration.CaseSensitive);

                cache = new MatcherCache(
                    configuration.Rules,
                    configuration.CaseSensitive,
                    matcher);

                _cache = cache;

                _logger.LogInformation(
                    "Path Hider loaded {RuleCount} enabled rule(s)",
                    matcher.RuleCount);
            }

            return cache.Matcher;
        }
    }

    private sealed record MatcherCache(
        string? Rules,
        bool CaseSensitive,
        PathRuleMatcher Matcher)
    {
        public bool Matches(PluginConfiguration configuration)
        {
            return string.Equals(Rules, configuration.Rules, StringComparison.Ordinal)
                && CaseSensitive == configuration.CaseSensitive;
        }
    }
}
