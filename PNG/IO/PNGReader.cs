﻿using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Remix.Helpers;

namespace Remix.IO;

/// <summary>
/// Reads primitive entries and color values from a <see cref="PNG"/> image.
/// </summary>
internal sealed class PNGReader : IDisposable, IImageReader<PNG> {
    private readonly static u8[] MAGIC_NUMBERS = new u8[8] {
        0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a
    };
    private readonly static Dictionary<PNGColorMode, u8> CHANNELS = new Dictionary<PNGColorMode, u8>() {
        { PNGColorMode.GRAYSCALE, 1 },
        { PNGColorMode.GRAYSCALE_WITH_ALPHA, 2 },
        { PNGColorMode.TRUECOLOR, 3 },
        { PNGColorMode.TRUECOLOR_WITH_ALPHA, 4 },
        { PNGColorMode.INDEXED, 1 },
    };

    private readonly PNGFilter _decoder = null!;
    private BinaryReader _source = null!;

    /// <summary>
    /// Open a <see cref="PNG"/> image to read.
    /// </summary>
    /// <param name="path">Access path of the <see cref="PNG"/>.</param>
    public PNGReader(string path) {
        _source = new BinaryReader(input: new FileStream(path, mode: FileMode.Open, access: FileAccess.Read));
        _decoder = new PNGFilter(decode: true);
    }

    /// <summary>
    /// Read the <see cref="RGBA"/> buffers from the <see cref="PNG"/> image file to the <paramref name="target"/>.
    /// </summary>
    /// <param name="target">The <see cref="PNG"/> itself.</param>
    public Task ReadBuffer(PNG target) {
        (u64 FirstIDAT, u64 Length) = DetectIDATs();

        using UMem<u8> encoded = UMem<u8>.Create(allocationLength: Length);
        using UMem<u8> decoded = UMem<u8>.Create(
            allocationLength: target.Scale.X * target.Scale.Y * CHANNELS[target.ColorMode] + target.Scale.Y
        );

        CopyIDATsBuffer(FirstIDAT, encoded);

        using(UnmanagedMemoryStream ustream = encoded.AsStream(access: FileAccess.Read))
        using (ZLibStream zip = new ZLibStream(stream: ustream, mode: CompressionMode.Decompress)) {
            i32 element = -1;
            u64 index = 0;

            while ((element = zip.ReadByte()) != -1)
                decoded[index++] = (u8)element;
        }

        u8 channelCount = CHANNELS[target.ColorMode];
        u32 lineLength = target.Scale.X * channelCount + 1;

        (u32 X, u32 Y) coordinate = (0, 0);
        PNGFilterType currentFilter = (PNGFilterType)decoded[0];

        for (u64 i = 1; i < decoded.Length; i += channelCount) {
            if (i % lineLength == 0) {
                currentFilter = (PNGFilterType)decoded[i];
                coordinate.X = 0;

                ++coordinate.Y;
                ++i;
            }

            target[coordinate.X, coordinate.Y].CopyFrom(from: decoded.AsSpan(from: i, length: channelCount));

            switch (currentFilter)  {
                case PNGFilterType.Sub: {
                        if ((i32)coordinate.X > 0)
                            _decoder.PrimitiveFilter(channelCount, target[coordinate.X - 1, coordinate.Y], ref target[coordinate.X, coordinate.Y]);
                        else
                            _decoder.PrimitiveFilter(channelCount, 0x00000000, ref target[coordinate.X, coordinate.Y]);

                        break;
                    }
                case PNGFilterType.Up: {
                        if ((i32)coordinate.Y > 0)
                            _decoder.PrimitiveFilter(channelCount, target[coordinate.X, coordinate.Y - 1], ref target[coordinate.X, coordinate.Y]);
                        else
                            _decoder.PrimitiveFilter(channelCount, 0x00000000, ref target[coordinate.X, coordinate.Y]);

                        break;
                    }
                case PNGFilterType.Avg: {
                        RGBA up = 0x00000000;
                        RGBA sub = 0x00000000;

                        if ((i32)coordinate.X > 0)
                            sub = target[coordinate.X - 1, coordinate.Y];

                        if ((i32)coordinate.Y > 0)
                            up = target[coordinate.X, coordinate.Y - 1];

                        _decoder.AvgFilter(channelCount, up, sub, ref target[coordinate.X, coordinate.Y]);
                        break;
                    }
                case PNGFilterType.Paeth: {
                        RGBA up = 0x00000000;
                        RGBA sub = 0x00000000;
                        RGBA sub_up = 0x00000000;

                        if ((i32)coordinate.X > 0)
                            sub = target[coordinate.X - 1, coordinate.Y];

                        if ((i32)coordinate.Y > 0)
                            up = target[coordinate.X, coordinate.Y - 1];

                        if ((i32)coordinate.X > 0 && (i32)coordinate.Y > 0)
                            sub_up = target[coordinate.X - 1, coordinate.Y - 1];

                        _decoder.PaethFilter(channelCount, up, sub, sub_up, ref target[coordinate.X, coordinate.Y]);
                        break;
                    }
            }

            ++coordinate.X;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates the file is a <see cref="PNG"/> image.
    /// </summary>
    /// <returns>Return <see langword="true"/> if the file is a <see cref="PNG"/> image. Otherwise return <see langword="false"/>.</returns>
    public bool IsPNG() {
        Span<u8> buffer = stackalloc u8[8];
        _source.Read(buffer: buffer);

        return buffer.SequenceEqual(other: MAGIC_NUMBERS);
    }

    /// <summary>
    /// Read a specific entry from the <see cref="PNG"/> header chunk.
    /// </summary>
    /// <typeparam name="T">Type of the entry.</typeparam>
    /// <param name="name">Name of the entry.</param>
    /// <returns>Return the entry value as <typeparamref name="T"/>.</returns>
    public T ReadHeaderEntry<T>(PNGHeaderEntry name) {
        Span<u8> stackBuff = stackalloc u8[4];
        _source.BaseStream.Seek(offset: 16, origin: SeekOrigin.Begin);

        switch (name) {
            case PNGHeaderEntry.Scale_X:
                _source.Read(stackBuff);

                stackBuff.Reverse();
                return Unsafe.BitCast<u32, T>(BitConverter.ToUInt32(stackBuff));

            case PNGHeaderEntry.Scale_Y:
                _source.BaseStream.Seek(offset: 4, origin: SeekOrigin.Current);
                _source.Read(stackBuff);

                stackBuff.Reverse();
                return Unsafe.BitCast<u32, T>(BitConverter.ToUInt32(stackBuff));

            case PNGHeaderEntry.Depth:
                _source.BaseStream.Seek(offset: 8, origin: SeekOrigin.Current);

                u8 depth = _source.ReadByte();
                return Unsafe.BitCast<u8, T>(depth);

            case PNGHeaderEntry.ColorMode:
                _source.BaseStream.Seek(offset: 9, origin: SeekOrigin.Current);

                PNGColorMode colorMode = (PNGColorMode)_source.ReadByte();
                return Unsafe.BitCast<PNGColorMode, T>(colorMode);
        }

        return default!;
    }

    public void Dispose() => _source.Dispose();

    #region PRIVATE METHODS

    private (u64 FirstIDATChunk, u64 Length) DetectIDATs() {
        _source.BaseStream.Seek(offset: 33, origin: SeekOrigin.Begin);

        Span<u8> name = stackalloc u8[4];
        Span<u8> length = stackalloc u8[4];

        u64 first = 0;
        u64 allLength = 0;

        while (_source.BaseStream.Length > _source.BaseStream.Position) {
            _source.Read(length);
            _source.Read(name);

            length.Reverse();
            u32 len = BitConverter.ToUInt32(value: length);

            if ("IDAT".SequenceEqual(Encoding.Latin1.GetString(bytes: name))) {
                if (first == 0)
                    first = (u64)_source.BaseStream.Position - 8;

                allLength += len;
            }

            _source.BaseStream.Seek((i64)len + 4, SeekOrigin.Current);
        }

        return (first, allLength);
    }

    private void CopyIDATsBuffer(u64 start, UMem<u8> destination) {
        ReadOnlySpan<u8> IDAT = stackalloc u8[4] { 0x49, 0x44, 0x41, 0x54 };

        Span<u8> name = stackalloc u8[4];
        Span<u8> length = stackalloc u8[4];

        u64 bufferPosition = 0;
        _source.BaseStream.Seek(offset: (i64)start, origin: SeekOrigin.Begin);

        do {
            _source.Read(length);
            _source.Read(name);

            length.Reverse();
            i32 idatLen = BitConverter.ToInt32(length);

            _source.Read(buffer: destination.AsSpan(bufferPosition, idatLen));
            _source.BaseStream.Seek(offset: 4, origin: SeekOrigin.Current);

            bufferPosition += (u64)idatLen;

        } while (name.SequenceEqual(other: IDAT));
    }

    #endregion
}

internal enum PNGHeaderEntry : u8 {
    Scale_X,
    Scale_Y,
    ColorMode,
    Depth
}

internal enum PNGFilterType : u8 {
    None,
    Sub,
    Up,
    Avg,
    Paeth
}
