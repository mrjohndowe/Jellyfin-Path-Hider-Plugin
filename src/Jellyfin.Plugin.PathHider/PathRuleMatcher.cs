using System.Text;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.PathHider;

/// <summary>
/// Compiles and evaluates Path Hider rules.
/// </summary>
internal sealed class PathRuleMatcher
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);
    private readonly IReadOnlyList<CompiledRule> _rules;

    private PathRuleMatcher(IReadOnlyList<CompiledRule> rules)
    {
        _rules = rules;
    }

    /// <summary>
    /// Gets an empty matcher.
    /// </summary>
    public static PathRuleMatcher Empty { get; } = new(Array.Empty<CompiledRule>());

    /// <summary>
    /// Gets the number of compiled rules.
    /// </summary>
    public int RuleCount => _rules.Count;

    /// <summary>
    /// Compiles newline-delimited rules.
    /// </summary>
    public static PathRuleMatcher Compile(string? rulesText, bool caseSensitive)
    {
        if (string.IsNullOrWhiteSpace(rulesText))
        {
            return Empty;
        }

        var rules = new List<CompiledRule>();
        foreach (var rawLine in rulesText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var scope = RuleScope.Any;
            if (line.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                scope = RuleScope.File;
                line = line["file:".Length..].Trim();
            }
            else if (line.StartsWith("folder:", StringComparison.OrdinalIgnoreCase))
            {
                scope = RuleScope.Folder;
                line = line["folder:".Length..].Trim();
            }
            else if (line.StartsWith("any:", StringComparison.OrdinalIgnoreCase))
            {
                line = line["any:".Length..].Trim();
            }

            line = Unquote(line);
            if (line.Length == 0)
            {
                continue;
            }

            rules.Add(CompiledRule.Create(rawLine.Trim(), line, scope, caseSensitive));
        }

        return rules.Count == 0 ? Empty : new PathRuleMatcher(rules);
    }

    /// <summary>
    /// Returns whether a filesystem entry matches any configured rule.
    /// </summary>
    public bool IsMatch(string? fullPath, string? name, bool isDirectory, out string? matchedRule)
    {
        var normalizedPath = NormalizePath(fullPath ?? string.Empty);
        var normalizedName = NormalizePath(
            string.IsNullOrWhiteSpace(name)
                ? GetName(normalizedPath)
                : name);

        foreach (var rule in _rules)
        {
            if (rule.IsMatch(normalizedPath, normalizedName, isDirectory))
            {
                matchedRule = rule.OriginalText;
                return true;
            }
        }

        matchedRule = null;
        return false;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"')
                || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1].Trim();
        }

        return value;
    }

    private static string NormalizePath(string value)
    {
        var normalized = value.Trim().Replace('\\', '/');

        while (normalized.Contains("//", StringComparison.Ordinal))
        {
            // Preserve a UNC prefix while collapsing duplicate separators elsewhere.
            if (normalized.StartsWith("//", StringComparison.Ordinal))
            {
                normalized = "//" + normalized[2..].Replace("//", "/", StringComparison.Ordinal);
                break;
            }

            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
        }

        if (normalized.Length > 1 && normalized.EndsWith("/", StringComparison.Ordinal))
        {
            normalized = normalized.TrimEnd('/');
        }

        return normalized;
    }

    private static string GetName(string normalizedPath)
    {
        var separatorIndex = normalizedPath.LastIndexOf('/');
        return separatorIndex >= 0 ? normalizedPath[(separatorIndex + 1)..] : normalizedPath;
    }

    private enum RuleScope
    {
        Any,
        File,
        Folder
    }

    private sealed class CompiledRule
    {
        private readonly RuleScope _scope;
        private readonly bool _usesFullPath;
        private readonly string? _exactPattern;
        private readonly Regex? _regex;
        private readonly StringComparison _comparison;

        private CompiledRule(
            string originalText,
            RuleScope scope,
            bool usesFullPath,
            string? exactPattern,
            Regex? regex,
            StringComparison comparison)
        {
            OriginalText = originalText;
            _scope = scope;
            _usesFullPath = usesFullPath;
            _exactPattern = exactPattern;
            _regex = regex;
            _comparison = comparison;
        }

        public string OriginalText { get; }

        public static CompiledRule Create(
            string originalText,
            string pattern,
            RuleScope scope,
            bool caseSensitive)
        {
            var normalizedPattern = NormalizePath(
                Environment.ExpandEnvironmentVariables(pattern));

            var usesFullPath = normalizedPattern.Contains("/", StringComparison.Ordinal);
            var hasWildcard = normalizedPattern.IndexOfAny(new[] { '*', '?' }) >= 0;
            var comparison = caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            if (!hasWildcard)
            {
                return new CompiledRule(
                    originalText,
                    scope,
                    usesFullPath,
                    normalizedPattern,
                    regex: null,
                    comparison);
            }

            var options = RegexOptions.CultureInvariant | RegexOptions.Compiled;
            if (!caseSensitive)
            {
                options |= RegexOptions.IgnoreCase;
            }

            var regex = new Regex(
                GlobToRegex(normalizedPattern),
                options,
                RegexTimeout);

            return new CompiledRule(
                originalText,
                scope,
                usesFullPath,
                exactPattern: null,
                regex,
                comparison);
        }

        public bool IsMatch(string path, string name, bool isDirectory)
        {
            if ((_scope == RuleScope.File && isDirectory)
                || (_scope == RuleScope.Folder && !isDirectory))
            {
                return false;
            }

            var candidate = _usesFullPath ? path : name;
            if (_exactPattern is not null)
            {
                return string.Equals(candidate, _exactPattern, _comparison);
            }

            try
            {
                return _regex!.IsMatch(candidate);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private static string GlobToRegex(string glob)
        {
            var builder = new StringBuilder("^");

            for (var index = 0; index < glob.Length; index++)
            {
                var character = glob[index];
                switch (character)
                {
                    case '*':
                        if (index + 1 < glob.Length && glob[index + 1] == '*')
                        {
                            while (index + 1 < glob.Length && glob[index + 1] == '*')
                            {
                                index++;
                            }

                            builder.Append(".*");
                        }
                        else
                        {
                            builder.Append("[^/]*");
                        }

                        break;

                    case '?':
                        builder.Append("[^/]");
                        break;

                    default:
                        builder.Append(Regex.Escape(character.ToString()));
                        break;
                }
            }

            builder.Append('$');
            return builder.ToString();
        }
    }
}
