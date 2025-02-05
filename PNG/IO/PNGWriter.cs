﻿using System.IO.Compression;
using System.Runtime.CompilerServices;
using Remix.Helpers;

namespace Remix.IO;

internal class PNGWriter: IDisposable, IImageWriter<PNG> {
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

    private PNGFilter _encoder = null!;
    private BinaryWriter _writer = null!;

    private PNGHeader _header = null!;

    public PNGWriter(string path) {
        this._writer = new BinaryWriter(output: new FileStream(path, mode: FileMode.Create, access: FileAccess.Write));
        this._encoder = new PNGFilter(decode: false);

        this._header = new PNGHeader();
        this._writer.Write(buffer: MAGIC_NUMBERS);
    }

    public void WriteHeaderEntry<T>(PNGHeaderEntry entry, T value) {
        switch(entry) {
            case PNGHeaderEntry.Scale_X:
                _header.Scale = (Unsafe.As<T, u32>(ref value), _header.Scale.Y);
                break;

            case PNGHeaderEntry.Scale_Y:
                _header.Scale = (_header.Scale.X, Unsafe.As<T, u32>(ref value));
                break;

            case PNGHeaderEntry.Depth:
                _header.Depth = Unsafe.As<T, u8>(ref value);
                break;

            case PNGHeaderEntry.ColorMode:
                _header.ColorMode = Unsafe.As<T, PNGColorMode>(ref value);
                break;
        }
    }

    public Task WriteBuffer(PNG from) {
        using UMem<u8> filtered = CreateFilteredBuffer(from);
        using UMem<u8> zipped = UMem<u8>.Create(allocationLength: (u64)(filtered.Length * 1.5f));

        i64 written = (i64)zipped.Length;

        using(UnmanagedMemoryStream ustream = zipped.AsStream(access: FileAccess.Write))
        using(ZLibStream zlib = new ZLibStream(stream: ustream, compressionLevel: CompressionLevel.SmallestSize)) {

            zlib.Write(buffer: filtered.AsSpan(from: 0, length: (i32)filtered.Length));
            zlib.Flush();

            /* Compressed data + 6 bytes of zlib header & CRC bytes (2 & 4 bytes) */
            written = ustream.Position + 6;
        }


        _header.CopyTo(destination: _writer);
        _header.Dispose();

        using PNGChunk IDAT_Container = new PNGChunk(name: "IDAT", buffer: zipped.AsSpan(0, (i32)written));
        IDAT_Container.CopyTo(destination: _writer);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Write a <see cref="PNGChunk"/> to the <see cref="PNG"/> file.
    /// </summary>
    /// <param name="chunk">Generic or specified <see cref="PNG"/> chunk.</param>
    public void WriteChunk(PNGChunk chunk) => chunk.CopyTo(destination: _writer);

    public void Dispose() {
        using PNGChunk IEND_Container = new PNGChunk(name: "IEND", buffer: UMem<byte>.Invalid);
        IEND_Container.CopyTo(destination: _writer);

        _writer.Dispose();
    }

    private UMem<u8> CreateFilteredBuffer(PNG from) {
        UMem<u8> buffer = UMem<u8>.Create(allocationLength: from.Scale.X * from.Scale.Y * CHANNELS[from.ColorMode] + from.Scale.Y);

        for(i32 y = 0; y < from.Scale.Y; ++y) {
            u64 nextFilterByte = (from.Scale.X * CHANNELS[from.ColorMode] + 1) * (u32)y;
            buffer[nextFilterByte] = (u8)PNGFilterType.Up;

            for(i32 x = 0; x < from.Scale.X; ++x) {
                RGBA current = from[(u32)x, (u32)y];

                switch((PNGFilterType)buffer[nextFilterByte]) {
                    case PNGFilterType.Sub: {
                            if(x > 0) {
                                RGBA before = from[(u32)x - 1, (u32)y];
                                _encoder.PrimitiveFilter(channelCount: CHANNELS[from.ColorMode], before, ref current);
                            }
                            break;    
                    }
                    case PNGFilterType.Up: {
                            if(y > 0) {
                                RGBA upper = from[(u32)x, (u32)y - 1];
                                _encoder.PrimitiveFilter(channelCount: CHANNELS[from.ColorMode], upper, ref current);
                            }
                            break;    
                    }
                    case PNGFilterType.Avg: {
                            RGBA before = 0x000;
                            RGBA upper =  0x000;

                            if(x > 0) upper = from[(u32)x - 1, (u32)y];
                            if(y > 0) upper = from[(u32)x, (u32)y - 1];

                            _encoder.AvgFilter(channelCount: CHANNELS[from.ColorMode], upper, before, ref current);
                            break;    
                    }
                    case PNGFilterType.Paeth: {
                            RGBA before = 0x000;
                            RGBA upper = 0x000;
                            RGBA upper_before = 0x000;

                            if (x > 0) upper = from[(u32)x - 1, (u32)y];
                            if (y > 0) upper = from[(u32)x, (u32)y - 1];

                            if (y > 0 && x > 0) upper = from[(u32)x - 1, (u32)y - 1];
                            _encoder.PaethFilter(ch: CHANNELS[from.ColorMode], upper, before, upper_before, ref current);
                            break;    
                    }
                }

                u64 offset = (nextFilterByte + 1) + (u32)(x * CHANNELS[from.ColorMode]);
                current.CopyTo(to: buffer.AsSpan(from: offset, length: CHANNELS[from.ColorMode]));
            }
        }

        return buffer;
    }
}
