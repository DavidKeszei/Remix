using System.IO.Compression;
using System.Runtime.CompilerServices;
using Remix.Helpers;

namespace Remix.IO;

internal class PNGWriter: IDisposable, IImageWriter<PNG> {
    private static Task[] _workers = new Task[Environment.ProcessorCount];
    private static object _lock = new object();

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
    private PNGPalette _palette = null!;

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
            default:
                throw new ArgumentException(message: "The given header entry is not exists in the PNG specs.");
        }
    }

    public async Task WriteBuffer(PNG from) {
        if (from.ColorMode == PNGColorMode.INDEXED)
            await ConvertToIndexed(from);

        using UMem<u8> filtered = CreateFilteredBuffer(from);
        using UMem<u8> zipped = UMem<u8>.Create(allocationLength: (u64)(filtered.Length * 1.75f + 6), @default: 0);

        i64 written = 0;

        using (UnmanagedMemoryStream ustream = zipped.AsStream(access: FileAccess.Write))
        using (ZLibStream zlib = new ZLibStream(stream: ustream, compressionLevel: CompressionLevel.SmallestSize)) {

            zlib.Write(buffer: filtered.AsSpan(from: 0, length: (i32)filtered.Length));
            zlib.Flush();

            /* Compressed data + 6 bytes of zlib header & CRC bytes (2 & 4 bytes) */
            written = ustream.Position + 6;
        }

        if (_header != null) {
            _header.CopyTo(_writer);
            _header.Dispose();

            _header = null!;
        }

        using PNGChunk IDAT_Container = new PNGChunk(name: "IDAT", buffer: zipped.AsSpan(0, (i32)written));
        IDAT_Container.CopyTo(destination: _writer);
    }

    /// <summary>
    /// Write a <see cref="PNGChunk"/> to the <see cref="PNG"/> file.
    /// </summary>
    /// <param name="chunk">Generic or specified <see cref="PNG"/> chunk.</param>
    public void WriteChunk(PNGChunk chunk) {
        if (_header != null!) {
            _header.CopyTo(destination: _writer);
            _header.Dispose();

            _header = null!;
        }

        if (chunk as PNGPalette != null)
            _palette = (PNGPalette)chunk;

        chunk.CopyTo(destination: _writer);
    }

    public void Dispose() {
        using PNGChunk IEND_Container = new PNGChunk(name: "IEND", buffer: UMem<u8>.Invalid);
        IEND_Container.CopyTo(destination: _writer);

        _writer.Dispose();
    }

    private UMem<u8> CreateFilteredBuffer(PNG from) {
        UMem<u8> buffer = UMem<u8>.Create(allocationLength: from.Scale.X * from.Scale.Y * CHANNELS[from.ColorMode] + from.Scale.Y);

        for(i32 y = 0; y < from.Scale.Y; ++y) {
            u64 nextFilterByte = (from.Scale.X * CHANNELS[from.ColorMode] + 1) * (u32)y;

            /*
                [TODO]
                    Check every filter method & select the smallest sized result. (Source: PNGv3 specification [https://www.w3.org/TR/png-3/])
                        -> Make it as parallel.
             */
            buffer[nextFilterByte] = from.ColorMode == PNGColorMode.INDEXED ? (u8)PNGFilterType.None : (u8)PNGFilterType.Up;

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
                    case PNGFilterType.None: {
                        break;
                    }
                }

                u64 offset = (nextFilterByte + 1) + (u32)(x * CHANNELS[from.ColorMode]);
                current.CopyTo(to: buffer.AsSpan(from: offset, length: CHANNELS[from.ColorMode]));
            }
        }

        return buffer;
    }

    private async Task ConvertToIndexed(PNG from) {
        u32 rowCount = from.Scale.Y < Environment.ProcessorCount ? from.Scale.Y : (u32)Environment.ProcessorCount;

        for (u32 y = 0; y < from.Scale.Y; y += rowCount) {
            u32 yRef = y;
            rowCount = from.Scale.Y - y < Environment.ProcessorCount ? from.Scale.Y - y : (u32)Environment.ProcessorCount;

            for (u32 w = 0; w < rowCount; ++w) {
                u32 wRef = w;

                lock (_lock) {
                    _workers[w] = Task.Run(action: () => {

                        for (u32 x = 0; x < from.Scale.X; ++x) {
                            f32 smallest = _palette.Palette[0].EuclidianDistance(from[x, yRef + wRef]);
                            i32 color = 0;

                            for (i32 i = 1; i < _palette.Palette.Count; ++i) {
                                f32 current = _palette.Palette[i].EuclidianDistance(from[x, yRef + wRef]);

                                if (current < smallest) {
                                    smallest = current;
                                    color = i;
                                }
                            }

                            from[x, yRef + wRef].R = (u8)color;
                        }
                    });
                }
            }

            await Task.WhenAll(tasks: _workers);
        }
    }
}
