using TmuxMobile.Core;

namespace TmuxMobile.Server;

public sealed record LoginRequest(string ApiKey);
public sealed record CreateSessionRequest(string Name);
public sealed record CreateSessionResponse(string Id, string Name);
public sealed record RenameRequest(string Name);
public sealed record KeysRequest(IReadOnlyList<TmuxKey> Keys);
public sealed record TextRequest(string Text);
public sealed record CaptureResponse(string Text, int RequestedLines);
