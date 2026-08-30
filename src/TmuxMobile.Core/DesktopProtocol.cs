namespace TmuxMobile.Core;

public static class DesktopProtocol
{
    public const int CurrentVersion = 1;
    public const int MinimumSupportedClientVersion = 1;

    public static IReadOnlyList<string> RequiredFeatures { get; } = Array.AsReadOnly([
        "session-tabs-v1",
        "terminal-websocket-v1",
        "tmux-topology-v1"
    ]);
}

public sealed record DesktopCapabilities(
    int ProtocolVersion,
    int MinimumClientProtocolVersion,
    IReadOnlyList<string> Features);
