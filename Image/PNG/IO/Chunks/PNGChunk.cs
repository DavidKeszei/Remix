using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Remix.IO;

namespace Remix;

/// <summary>
/// Represent a generic <see cref="PNG"/> chunk.
/// </summary>
internal class PNGChunk: ICopyTo<BinaryWriter>, IDisposable {
    protected UMem<u8> _buffer = UMem<u8>.Invalid;
    private string _name = string.Empty;

    public string Name { get => _name; }

    public PNGChunk() { }

    public PNGChunk(string name, UMem<u8> buffer) {
        this._name = name;
        this._buffer = buffer;
    }

    public PNGChunk(string name, Span<u8> buffer) {
        this._name = name;
        this._buffer = UMem<u8>.Create((u32)buffer.Length);

        buffer.CopyTo(destination: _buffer.AsSpan(0, (i32)_buffer.Length));
    }

    /// <summary>
    /// CopyTo the <see cref="PNGChunk"/> instance to the <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">Output (file)stream as <see cref="BinaryWriter"/>.</param>
    public virtual void CopyTo(BinaryWriter destination) {
        Span<u8> crc = stackalloc u8[4];
        Span<u8> st_name = stackalloc u8[4];
        Span<u8> st_length = stackalloc u8[4];

        Encoding.Latin1.TryGetBytes(_name, st_name, out i32 written);
        BitConverter.TryWriteBytes(st_length, (u32)_buffer.Length);

        st_length.Reverse<u8>();
        destination.Write(buffer: st_length);

        destination.Write(buffer: st_name);

        if(!_buffer.Equals(other: UMem<u8>.Invalid))
            destination.Write(buffer: _buffer.AsSpan(from: 0, length: (i32)_buffer.Length));

        BitConverter.TryWriteBytes(crc, CreateCRC(nameBuffer: st_name, dataBuffer: _buffer.AsSpan(from: 0, length: (i32)_buffer.Length)));

        crc.Reverse<u8>();
        destination.Write(buffer: crc);
    }

    public void Dispose() => _buffer.Dispose();

    private u32 CreateCRC(Span<u8> nameBuffer, Span<u8> dataBuffer) {
        u64 crc = 0xffffffffL;
        Span<u64> table = stackalloc u64[256];

        CreateCRCTable(tableRef: table);

        for(i32 i = 0; i < (nameBuffer.Length + dataBuffer.Length); ++i) {
            u8 value = i < nameBuffer.Length ? nameBuffer[i] : dataBuffer[i - nameBuffer.Length];

            crc = table[(i32)((crc ^ value) & 0xff)] ^ (crc >> 8);
        }

        return (u32)(crc ^ 0xffffffffL);
    }

    private void CreateCRCTable(Span<u64> tableRef) {
        u64 c = 0;

        for(i32 i = 0; i < tableRef.Length; ++i) {
            c = (u32)i;

            for(i32 j = 0; j < 8; ++j) {

                if ((c & 1) == 1) c = 0xedb88320L ^ (c >> 1);
                else c >>= 1;
            }

            tableRef[i] = c;
        }
    }
}
