using Microsoft.Extensions.Logging.Abstractions;
using TmuxMobile.Core;
using TmuxMobile.Infrastructure;

namespace TmuxMobile.Infrastructure.Tests;

public sealed class ProcessRunnerTests
{
    [Fact]
    public async Task KeepsArgumentsSeparateAndCapturesStreams()
    {
        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance);
        var result = await runner.RunAsync(new("/usr/bin/printf", ["%s", "hello world"],
            TimeSpan.FromSeconds(2), 1024, "test.printf"), CancellationToken.None);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("hello world", result.StandardOutput);
    }

    [Fact]
    public async Task BoundsRetainedOutput()
    {
        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance);
        var result = await runner.RunAsync(new("/usr/bin/printf", ["1234567890"],
            TimeSpan.FromSeconds(2), 4, "test.bound"), CancellationToken.None);
        Assert.True(result.WasTruncated);
        Assert.Equal("1234", result.StandardOutput);
    }
}
