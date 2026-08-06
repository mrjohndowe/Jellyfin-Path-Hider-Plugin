
namespace Jellyfin.Plugin.PathHider.Tests;

public sealed class PathRuleMatcherTests
{
    [Fact]
    public void ExactFolderPath_MatchesFolderOnly()
    {
        var matcher = PathRuleMatcher.Compile("folder:/media/private", caseSensitive: true);

        Assert.True(matcher.IsMatch("/media/private", "private", isDirectory: true, out _));
        Assert.False(matcher.IsMatch("/media/private", "private", isDirectory: false, out _));
        Assert.False(matcher.IsMatch("/media/private/movie.mkv", "movie.mkv", isDirectory: false, out _));
    }

    [Fact]
    public void FolderName_MatchesAtAnyDepth()
    {
        var matcher = PathRuleMatcher.Compile("folder:Extras", caseSensitive: false);

        Assert.True(matcher.IsMatch("/media/Movie/EXTRAS", "EXTRAS", isDirectory: true, out _));
    }

    [Fact]
    public void DoubleStar_CrossesDirectories()
    {
        var matcher = PathRuleMatcher.Compile("file:**/*-workprint.mkv", caseSensitive: false);

        Assert.True(matcher.IsMatch(
            "/media/movies/title/cuts/title-workprint.mkv",
            "title-workprint.mkv",
            isDirectory: false,
            out _));
    }

    [Fact]
    public void SingleStar_DoesNotCrossDirectories()
    {
        var matcher = PathRuleMatcher.Compile("/media/*/secret.mkv", caseSensitive: true);

        Assert.True(matcher.IsMatch(
            "/media/title/secret.mkv",
            "secret.mkv",
            isDirectory: false,
            out _));

        Assert.False(matcher.IsMatch(
            "/media/title/cuts/secret.mkv",
            "secret.mkv",
            isDirectory: false,
            out _));
    }

    [Fact]
    public void QuestionMark_MatchesOneCharacter()
    {
        var matcher = PathRuleMatcher.Compile("file:sample-?.mkv", caseSensitive: true);

        Assert.True(matcher.IsMatch("/media/sample-a.mkv", "sample-a.mkv", false, out _));
        Assert.False(matcher.IsMatch("/media/sample-ab.mkv", "sample-ab.mkv", false, out _));
    }

    [Fact]
    public void CommentsAndBlankLines_AreIgnored()
    {
        var matcher = PathRuleMatcher.Compile(
            "# comment\n\nfile:hidden.mkv",
            caseSensitive: false);

        Assert.Equal(1, matcher.RuleCount);
        Assert.True(matcher.IsMatch("/media/hidden.mkv", "hidden.mkv", false, out _));
    }

    [Fact]
    public void Backslashes_AreNormalized()
    {
        var matcher = PathRuleMatcher.Compile(
            @"folder:C:\Media\Private",
            caseSensitive: false);

        Assert.True(matcher.IsMatch(
            @"C:\Media\Private",
            "Private",
            isDirectory: true,
            out _));
    }

    [Fact]
    public void QuotedPathWithSpaces_IsSupported()
    {
        var matcher = PathRuleMatcher.Compile(
            "folder:\"/media/Private Videos\"",
            caseSensitive: true);

        Assert.True(matcher.IsMatch(
            "/media/Private Videos",
            "Private Videos",
            isDirectory: true,
            out _));
    }
}
