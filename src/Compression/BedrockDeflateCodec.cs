using ConMaster.Compression;
using System.IO.Compression;

namespace Orion.Compression;

/// <summary>
/// Raw RFC 1951 deflate for Bedrock batches. Uses a per-thread <see cref="DeflateCompressor"/>
/// (zero managed allocations on the hot path after warmup).
/// </summary>
public static class BedrockDeflateCodec
{
    private static readonly ThreadLocal<DeflateCompressor> Compressor = new(CreateCompressor, trackAllValues: false);

    public static int Compress(ReadOnlySpan<byte> input, Span<byte> output)
    {
        return Compressor.Value!.Compress(input, output);
    }

    public static int Decompress(ReadOnlySpan<byte> input, Span<byte> output)
    {
        return Compressor.Value!.Decompress(input, output);
    }

    public static bool TryCompress(ReadOnlySpan<byte> input, Span<byte> output, out int bytesWritten)
    {
        return Compressor.Value!.TryCompress(input, output, out bytesWritten);
    }

    public static bool TryDecompress(ReadOnlySpan<byte> input, Span<byte> output, out int bytesWritten)
    {
        return Compressor.Value!.TryDecompress(input, output, out bytesWritten);
    }

    public static int GetCompressBound(int sourceLength) => sourceLength + 64;

    private static DeflateCompressor CreateCompressor()
    {
        return new DeflateCompressor
        {
            CompressionLevel = CompressionLevel.Fastest,
            MemoryLevel = 8
        };
    }
}
