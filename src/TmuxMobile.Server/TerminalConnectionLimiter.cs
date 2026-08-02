using System.Collections.Concurrent;

namespace TmuxMobile.Server;

public sealed class TerminalConnectionLimiter
{
    private readonly object _gate = new();
    private readonly Dictionary<string, int> _perUser = new(StringComparer.Ordinal);
    private int _total;

    public IDisposable? TryAcquire(string user, int globalLimit, int perUserLimit)
    {
        lock (_gate)
        {
            _perUser.TryGetValue(user, out var count);
            if (_total >= globalLimit || count >= perUserLimit) return null;
            _total++;
            _perUser[user] = count + 1;
            return new Lease(this, user);
        }
    }

    private void Release(string user)
    {
        lock (_gate)
        {
            _total--;
            var next = _perUser[user] - 1;
            if (next == 0) _perUser.Remove(user);
            else _perUser[user] = next;
        }
    }

    private sealed class Lease(TerminalConnectionLimiter owner, string user) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) owner.Release(user);
        }
    }
}
