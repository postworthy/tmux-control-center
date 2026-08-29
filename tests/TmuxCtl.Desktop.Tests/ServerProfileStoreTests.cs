using TmuxCtl.Desktop;
using Xunit;

namespace TmuxCtl.Desktop.Tests;

public sealed class ServerProfileStoreTests : IDisposable
{
    private readonly string _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
        $"tmuxctl-profile-tests-{Guid.NewGuid():N}");

    [Fact]
    public void SavesUpdatesLoadsAndDeletesOnlyProfileMetadata()
    {
        var path = System.IO.Path.Combine(_directory, "profiles.json");
        var store = new ServerProfileStore(path);

        var first = store.Save(null, "  Home server  ", "https://home.example/");
        Assert.Equal("Home server", first.Label);
        Assert.Equal("https://home.example", first.ServerUrl);

        var updated = store.Save(first.Id, "Home", "https://home.example:8443");
        Assert.Equal(first.Id, updated.Id);
        Assert.Single(store.Load());
        Assert.Equal(updated, store.Load().Single());

        var json = File.ReadAllText(path);
        Assert.DoesNotContain("apiKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("terminal", json, StringComparison.OrdinalIgnoreCase);

        Assert.True(store.Delete(first.Id));
        Assert.Empty(store.Load());
        Assert.False(store.Delete(first.Id));
    }

    [Fact]
    public void UsesOwnerOnlyPermissionsOnUnix()
    {
        if (OperatingSystem.IsWindows()) return;
        var path = System.IO.Path.Combine(_directory, "profiles.json");
        var store = new ServerProfileStore(path);
        store.Save(null, "Home", "https://home.example");

        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
            File.GetUnixFileMode(_directory));
    }

    [Theory]
    [InlineData("", "https://home.example")]
    [InlineData("Home", "http://home.example")]
    [InlineData("Home", "https://user:secret@home.example")]
    [InlineData("Home", "https://home.example/path")]
    public void RejectsInvalidProfiles(string label, string url)
    {
        var store = new ServerProfileStore(System.IO.Path.Combine(_directory, "profiles.json"));
        Assert.Throws<ArgumentException>(() => store.Save(null, label, url));
        Assert.False(File.Exists(store.Path));
    }

    [Fact]
    public void FailsClosedForCorruptState()
    {
        Directory.CreateDirectory(_directory);
        var path = System.IO.Path.Combine(_directory, "profiles.json");
        File.WriteAllText(path, "{not-json");
        var store = new ServerProfileStore(path);

        Assert.Throws<InvalidDataException>(() => store.Load());
        Assert.Equal("{not-json", File.ReadAllText(path));
    }

    [Fact]
    public void ChooserEscapesUntrustedProfileText()
    {
        var profile = new ServerProfile(Guid.NewGuid(), "</script><script>alert(1)</script>",
            "https://home.example");
        var html = ProfileChooserPage.Render([profile]);

        Assert.DoesNotContain("</script><script>alert(1)</script>", html, StringComparison.Ordinal);
        Assert.Contains("\\u003C/script\\u003E", html, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
