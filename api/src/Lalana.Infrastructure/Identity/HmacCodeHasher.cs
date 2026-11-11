using System.Security.Cryptography;
using System.Text;
using Lalana.Application.Auth;

namespace Lalana.Infrastructure.Identity;

/// <summary>HMAC-SHA256 of codes and tickets with a server key, so a database copy alone is useless.</summary>
public sealed class HmacCodeHasher(byte[] key) : ICodeHasher
{
    public const int MinKeyLength = 32;

    private readonly byte[] key = key.Length >= MinKeyLength
        ? key
        : throw new ArgumentException($"The code hash key needs at least {MinKeyLength} bytes.", nameof(key));

    public string Hash(string code) => Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(code)));
}
