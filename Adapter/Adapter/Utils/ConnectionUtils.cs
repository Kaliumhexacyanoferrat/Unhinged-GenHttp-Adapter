using System.Runtime.CompilerServices;
using Unhinged;

namespace Adapter.Utils;

internal static class ConnectionUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteHeaderUnmanaged(this FixedBufferWriter writer, KeyValuePair<string, string> header)
    {
        writer.WriteUnmanaged(header.Key);
        writer.WriteUnmanaged(": "u8);
        writer.WriteUnmanaged(header.Value);
        writer.WriteUnmanaged("\r\n"u8);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteHeaderUnmanaged(this FixedBufferWriter writer, string key, string value)
    {
        writer.WriteUnmanaged(key);
        writer.WriteUnmanaged(": "u8);
        writer.WriteUnmanaged(value);
        writer.WriteUnmanaged("\r\n"u8);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteHeaderUnmanaged(this FixedBufferWriter writer, ReadOnlySpan<byte> key, string value)
    {
        writer.WriteUnmanaged(key);
        writer.WriteUnmanaged(": "u8);
        writer.WriteUnmanaged(value);
        writer.WriteUnmanaged("\r\n"u8);
    }
}