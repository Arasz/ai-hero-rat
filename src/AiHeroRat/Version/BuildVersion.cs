using CommunityToolkit.Diagnostics;

namespace AiHeroRat.Version;

/// <summary>The informational version of a build; <see cref="Bare"/> drops any <c>+sha</c> suffix.</summary>
internal sealed class BuildVersion
{
    public BuildVersion(string informational)
    {
        Guard.IsNotNullOrWhiteSpace(informational);
        var plusIndex = informational.IndexOf('+');
        Bare = plusIndex < 0 ? informational : informational[..plusIndex];
    }

    public string Bare { get; }
}
