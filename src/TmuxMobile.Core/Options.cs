using System.ComponentModel.DataAnnotations;

namespace TmuxMobile.Core;

public sealed class TmuxOptions
{
    public const string Section = "Tmux";
    [Required] public string ExecutablePath { get; init; } = "/usr/bin/tmux";
    public string? SocketName { get; init; }
    [Range(1, 60)] public int PollingIntervalSeconds { get; init; } = 3;
    [Range(2, 300)] public int PreviewRefreshIntervalSeconds { get; init; } = 10;
    [Range(10, 5000)] public int MaxCaptureLines { get; init; } = 500;
    [Range(1024, 1_048_576)] public int MaxCaptureBytes { get; init; } = 131_072;
    [Range(1, 60)] public int ProcessTimeoutSeconds { get; init; } = 5;
    [Range(10, 1000)] public int CardPreviewLines { get; init; } = 80;
    [Required] public string Prefix { get; init; } = "C-b";
}

public sealed class AuthOptions
{
    public const string Section = "Authentication";
    public const string TailnetTestAcknowledgement = "TAILNET_TEST_ONLY";
    [Required] public string Mode { get; init; } = "ApiKey";
    public string? ApiKey { get; init; }
    public bool AllowDevelopmentBypass { get; init; }
    public bool UnsafeAllowProductionBypass { get; init; }
    public bool UnsafeAllowInsecureHttp { get; init; }
    public bool UnsafeAllowWeakApiKeyForTest { get; init; }
    public string? UnsafeTestProfileAcknowledgement { get; init; }
}

public sealed class SecurityOptions
{
    public const string Section = "Security";
    public string[] AllowedOrigins { get; init; } = [];
    public bool ExternalHttpsTermination { get; init; }
    [Range(1024, 1_048_576)] public int MaxRequestBodyBytes { get; init; } = 65_536;
    [Range(256, 65_536)] public int MaxWebSocketMessageBytes { get; init; } = 16_384;
    [Range(1, 100)] public int MaxTerminalConnections { get; init; } = 4;
    [Range(1, 10)] public int MaxTerminalConnectionsPerUser { get; init; } = 2;
    [Range(1, 1440)] public int TerminalIdleTimeoutMinutes { get; init; } = 30;
    [Range(1, 1000)] public int MaxTerminalInputMessagesPerSecond { get; init; } = 64;
    [Range(1024, 1_048_576)] public int MaxTerminalInputBytesPerSecond { get; init; } = 262_144;
}

public sealed class StatusOptions
{
    public const string Section = "Status";
    [Range(1, 1440)] public int IdleAfterMinutes { get; init; } = 10;
    public string[] WaitingPatterns { get; init; } = ["press enter", "continue?", "waiting for"];
    public string[] CompletedPatterns { get; init; } = ["completed", "finished", "done"];
    public string[] FailurePatterns { get; init; } = ["error:", "failed", "fatal:"];
    public string[] ShellCommands { get; init; } = ["bash", "zsh", "fish", "sh"];
}

public sealed class AuditOptions
{
    public const string Section = "Audit";
    [Required] public string Destination { get; init; } = "logs/audit.jsonl";
}

public sealed class DataProtectionSettings
{
    public const string Section = "DataProtection";
    [Required] public string KeysDirectory { get; init; } = "data-protection";
}

public sealed class ForwardedHeaderSettings
{
    public const string Section = "ForwardedHeaders";
    public bool Enabled { get; init; }
    public string[] KnownProxies { get; init; } = ["127.0.0.1", "::1"];
    public string[] KnownProxyHosts { get; init; } = [];
}
