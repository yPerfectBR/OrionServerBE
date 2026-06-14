using Orion.Config;

namespace Orion.Compression;

/// <summary>
/// Bedrock game packet batch framing (0xFE header + optional zlib raw deflate).
/// Mirrors Basalt Protocol Io framing with the optimized deflate backend.
/// </summary>
public static class PacketFraming
{
    public const byte FrameMagic = 0xFE;

    public static int Compress(
        ReadOnlySpan<byte> input,
        Span<byte> output,
        CompressionMethod compression)
    {
        if (compression == CompressionMethod.Snappy)
        {
            throw new NotSupportedException("Snappy compression is not supported.");
        }

        int headerSize = compression == CompressionMethod.NotPresent ? 0 : 1;
        if (output.Length < input.Length + headerSize)
        {
            throw new ArgumentException("Output buffer is too small.", nameof(output));
        }

        int outputOffset = 0;
        if (compression != CompressionMethod.NotPresent)
        {
            output[0] = (byte)compression;
            outputOffset = 1;
        }

        if (compression == CompressionMethod.Zlib)
        {
            int bytesWritten = BedrockDeflateCodec.Compress(input, output[outputOffset..]);
            return bytesWritten + outputOffset;
        }

        input.CopyTo(output[outputOffset..]);
        return input.Length + outputOffset;
    }

    public static int Frame(
        ReadOnlySpan<byte> input,
        Span<byte> output,
        CompressionMethod compression,
        int compressionThreshold)
    {
        CompressionMethod method = compression;
        if (method != CompressionMethod.None
            && method != CompressionMethod.NotPresent
            && input.Length < compressionThreshold)
        {
            method = CompressionMethod.None;
        }

        if (output.Length == 0)
        {
            throw new ArgumentException("Output buffer is too small.", nameof(output));
        }

        output[0] = FrameMagic;
        int bytesWritten = Compress(input, output[1..], method);
        return bytesWritten + 1;
    }

    public static int Frame(ReadOnlySpan<byte> input, Span<byte> output)
    {
        Orion.Config.NetworkConfig network = OrionInfo.Network;
        CompressionMethod method = (CompressionMethod)network.CompressionMethod;
        return Frame(input, output, method, network.CompressionThreshold);
    }

    public static int Unframe(ReadOnlySpan<byte> input, Span<byte> output, out CompressionMethod compression)
    {
        if (input.Length == 0 || input[0] != FrameMagic)
        {
            compression = CompressionMethod.NotPresent;
            return 0;
        }

        ReadOnlySpan<byte> payload = input[1..];
        if (payload.Length == 0)
        {
            compression = CompressionMethod.NotPresent;
            return 0;
        }

        CompressionMethod header = (CompressionMethod)payload[0];
        switch (header)
        {
            case CompressionMethod.Zlib:
                compression = CompressionMethod.Zlib;
                return BedrockDeflateCodec.Decompress(payload[1..], output);

            case CompressionMethod.None:
                compression = CompressionMethod.None;
                payload[1..].CopyTo(output);
                return payload.Length - 1;

            case CompressionMethod.Snappy:
                throw new NotSupportedException("Snappy decompression is not supported.");

            default:
                compression = CompressionMethod.NotPresent;
                payload.CopyTo(output);
                return payload.Length;
        }
    }
}
