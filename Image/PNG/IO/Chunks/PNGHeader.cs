using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix;

internal class PNGHeader: PNGChunk {
    private (u32 X, u32 Y) _scale = (0, 0);
    private u8 _depth = 0;

    private PNGColorMode _colorMode = 0;

    public (u32 X, u32 Y) Scale { get => _scale; set => _scale = value; }

    public u8 Depth { get => _depth; set => _depth = value; }

    public PNGColorMode ColorMode { get => _colorMode; set => _colorMode = value; }

    public PNGHeader(): base(name: "IHDR", buffer: UMem<u8>.Create(allocationLength: 13)) { }

    public override void CopyTo(BinaryWriter destination) {
        Span<u8> stackAlloc = stackalloc u8[(i32)base._buffer.Length];
        stackAlloc.Fill(value: 0);

        BitConverter.TryWriteBytes(stackAlloc[..4], Scale.X);
        BitConverter.TryWriteBytes(stackAlloc[4..8], Scale.Y);

        stackAlloc[..4].Reverse();
        stackAlloc[4..8].Reverse();

        stackAlloc[8] = _depth;
        stackAlloc[9] = (u8)_colorMode;

        stackAlloc.CopyTo(destination: _buffer.AsSpan(from: 0, length: (i32)_buffer.Length));
        base.CopyTo(destination);
    }
}
