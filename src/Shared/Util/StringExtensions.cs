using System;
using System.Text;

namespace Protocol.Shared.Util;

public static class StringExtensions
{
    /// <summary>
    /// Generates a deterministic, non-cryptographic 32-bit integer hash from a string.
    /// This value remains identical across application restarts and different machines.
    /// </summary>
    public static int Int32Hash(this string input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.Length == 0) return 0;

        // FNV-1a constants for 32-bit hashing
        const uint fnvPrime = 16777619;
        const uint fnvOffsetBasis = 2166136261;

        uint hash = fnvOffsetBasis;

        // Process characters as spans to prevent memory allocations
        ReadOnlySpan<char> span = input.AsSpan();

        for (int i = 0; i < span.Length; i++)
        {
            // XOR the lower 8 bits of the character
            hash ^= (byte)(span[i] & 0xFF);
            hash *= fnvPrime;

            // XOR the upper 8 bits of the character (handles Unicode/UTF-16 correctly)
            hash ^= (byte)((span[i] >> 8) & 0xFF);
            hash *= fnvPrime;
        }

        // Cast to signed 32-bit int. Unchecked context handles the bit transition naturally.
        return unchecked((int)hash);
    }
}