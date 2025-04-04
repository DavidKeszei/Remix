using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Remix;

/// <summary>
/// Represent a <see cref="PNG"/> color palette chunk.
/// </summary>
internal sealed class PNGPalette: PNGChunk {
    private Palette _palette = default;

    /// <summary>
    /// Underlying color <see cref="Remix.Palette"/> of the chunk.
    /// </summary>
    public Palette Palette { get => _palette; }

    /// <summary>
    /// Create new palette with specific <paramref name="capacity"/>
    /// </summary>
    /// <param name="capacity">Capacity of the palette. (Power of 2)</param>
    public PNGPalette(u32 capacity): base(name: "PLTE", buffer: UMem<u8>.Invalid) {
        _palette = new Palette(capacity: (i32)capacity);
        _buffer = UMem<u8>.Create(allocationLength: (u32)(3 * _palette.Count));
    }

    public override void CopyTo(BinaryWriter destination) {
        for (i32 i = 0; i < _palette.Count; ++i) {
            _buffer[(u32)(i * 3)] = _palette[i].R;
            _buffer[(u32)(i * 3) + 1] = _palette[i].G;
            _buffer[(u32)(i * 3) + 2] = _palette[i].B;
        }

        base.CopyTo(destination);
    }
}
