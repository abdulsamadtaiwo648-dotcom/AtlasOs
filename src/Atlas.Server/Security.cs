using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Atlas.Server;

/// <summary>Decides which network addresses may talk to Atlas.</summary>
public static class NetworkPolicy
{
    public static bool IsAllowed(IPAddress? address, bool allowPublicInternet)
    {
        if (allowPublicInternet)
            return true;

        if (address == null)
            return false;

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] b = address.GetAddressBytes();
            return b[0] == 10                                   // 10.0.0.0/8      home / office networks
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)    // 172.16.0.0/12
                || (b[0] == 192 && b[1] == 168)                 // 192.168.0.0/16  typical home Wi-Fi
                || (b[0] == 169 && b[1] == 254)                 // link-local
                || (b[0] == 100 && b[1] >= 64 && b[1] <= 127);  // 100.64.0.0/10   Tailscale
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            byte[] b = address.GetAddressBytes();
            return (b[0] & 0xFE) == 0xFC                        // fc00::/7  unique-local
                || (b[0] == 0xFE && (b[1] & 0xC0) == 0x80);     // fe80::/10 link-local
        }

        return false;
    }
}

/// <summary>Compares secrets without leaking, through timing, how much of a guess was right.</summary>
public static class TokenCheck
{
    public static bool Matches(string? supplied, string expected)
    {
        if (string.IsNullOrEmpty(supplied) || string.IsNullOrEmpty(expected))
            return false;

        // Hash both sides so the comparison is always over equal-length data.
        byte[] a = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        byte[] b = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}

/// <summary>Slows down password guessing: too many wrong tries from one address locks it out for a while.</summary>
public sealed class AuthThrottle
{
    private readonly int _maxFailures;
    private readonly TimeSpan _window;
    private readonly TimeSpan _lockout;
    private readonly Func<DateTimeOffset> _now;
    private readonly Dictionary<string, (int Failures, DateTimeOffset First, DateTimeOffset? LockedUntil)> _state = new();
    private readonly object _lock = new();

    public AuthThrottle(
        int maxFailures = 8,
        TimeSpan? window = null,
        TimeSpan? lockout = null,
        Func<DateTimeOffset>? now = null)
    {
        _maxFailures = maxFailures;
        _window = window ?? TimeSpan.FromMinutes(10);
        _lockout = lockout ?? TimeSpan.FromMinutes(10);
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    public bool IsLockedOut(string client)
    {
        lock (_lock)
        {
            if (!_state.TryGetValue(client, out var s) || s.LockedUntil == null)
                return false;

            if (s.LockedUntil > _now())
                return true;

            _state.Remove(client); // lockout over
            return false;
        }
    }

    public void RecordFailure(string client)
    {
        lock (_lock)
        {
            DateTimeOffset now = _now();
            _state.TryGetValue(client, out var s);

            if (s.Failures == 0 || now - s.First > _window)
                s = (0, now, null);

            s.Failures++;

            if (s.Failures >= _maxFailures)
                s.LockedUntil = now + _lockout;

            _state[client] = s;
        }
    }

    public void RecordSuccess(string client)
    {
        lock (_lock)
            _state.Remove(client);
    }
}
