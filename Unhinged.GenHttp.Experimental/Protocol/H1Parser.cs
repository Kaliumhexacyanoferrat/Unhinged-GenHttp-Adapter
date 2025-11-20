namespace Unhinged.GenHttp.Experimental.Protocol;

internal static class H1Parser
{
    // ===== Common tokens (kept as ReadOnlySpan<byte> for zero-allocation literals) =====

    private static ReadOnlySpan<byte> Crlf => "\r\n"u8;
    private static ReadOnlySpan<byte> CrlfCrlf => "\r\n\r\n"u8;

    // ASCII byte codes (documented for clarity)
    private const byte Space = 0x20;        // ' '
    private const byte Question = 0x3F;     // '?'
    private const byte QuerySeparator = 0x26; // '&'
    private const byte Equal = 0x3D;        // '='
    private const byte Colon = 0x3A;        // ':'
    private const byte SemiColon = 0x3B;    // ';'
}