using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TmuxMobile.Core;
using TmuxMobile.Infrastructure;

namespace TmuxMobile.Infrastructure.Tests;

public sealed class AuditLoggerTests
{
    [Fact]
    public async Task WritesContentFreeRecordWithOwnerOnlyPermissions()
    {
        var root = Path.Combine(Path.GetTempPath(), $"tmux-mobile-audit-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "audit.jsonl");
        var logger = CreateLogger(path);

        Assert.True(await logger.WriteAsync("pane.text", "owner", "p_safe", true,
            CancellationToken.None));

        var content = await File.ReadAllTextAsync(path);
        Assert.Contains("\"action\":\"pane.text\"", content, StringComparison.Ordinal);
        Assert.Contains("\"target\":\"p_safe\"", content, StringComparison.Ordinal);
        Assert.DoesNotContain("terminal input", content, StringComparison.Ordinal);
        if (OperatingSystem.IsLinux())
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite,
                File.GetUnixFileMode(path));
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
                File.GetUnixFileMode(root));
        }
    }

    [Fact]
    public async Task InsecureExistingDirectoryFailsWithoutThrowing()
    {
        var root = Path.Combine(Path.GetTempPath(), $"tmux-mobile-audit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        if (OperatingSystem.IsLinux())
            File.SetUnixFileMode(root, UnixFileMode.UserRead | UnixFileMode.UserWrite |
                UnixFileMode.UserExecute | UnixFileMode.GroupRead);
        var logger = CreateLogger(Path.Combine(root, "audit.jsonl"));

        var result = await logger.WriteAsync("session.rename", "owner", "s_safe", true,
            CancellationToken.None);

        if (OperatingSystem.IsLinux()) Assert.False(result);
        else Assert.True(result);
    }

    private static JsonLineAuditLogger CreateLogger(string path) => new(
        Options.Create(new AuditOptions { Destination = path }),
        TimeProvider.System,
        new EnvironmentStub(),
        NullLogger<JsonLineAuditLogger>.Instance);

    private sealed class EnvironmentStub : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
