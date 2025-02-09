using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Effect;

/// <summary>
/// Represent a channel shifter on an <see cref="Image"/>.
/// </summary>
public class ChannelShift: Effect {
    private (i32 X, i32 Y) _shiftRed = (0, 0);
    private (i32 X, i32 Y) _shiftGreen = (0, 0);
    private (i32 X, i32 Y) _shiftBlue = (0, 0);

    /// <summary>
    /// Create new <see cref="ChannelShift"/> with specific shifting values in all axis.
    /// </summary>
    /// <param name="red">Shift values on the red channel.</param>
    /// <param name="green">Shift values on the green channel.</param>
    /// <param name="blue">Shift values on the blue channel.</param>
    /// <param name="before">This effect precedes this effect in effect pipeline.</param>
    public ChannelShift((i32 x, i32 y) red, (i32 x, i32 y) green, (i32 x, i32 y) blue) : base(name: nameof(ChannelShift)) {
        this._shiftRed = red;
        this._shiftGreen = green;
        this._shiftBlue = blue;
    }

    public override async Task Apply(Image target) {
        UMem2D<RGBA>[] split = SplitChannels(target);

        for(u32 y = 0; y < target.Scale.Y; ++y) { 
            for(u32 x = 0;  x < target.Scale.X; ++x) {

                if ((x - _shiftRed.X >= 0 && x - _shiftRed.X < target.Scale.X) && 
                    (y - _shiftRed.Y >= 0 && y - _shiftRed.Y < target.Scale.Y))
                    target[x, y].R = split[0][(u32)(x - _shiftRed.X), (u32)(y - _shiftRed.Y)].R;

                if ((x - _shiftGreen.X >= 0 && x - _shiftGreen.X < target.Scale.X) &&
                    (y - _shiftGreen.Y >= 0 && y - _shiftGreen.Y < target.Scale.Y))
                    target[x, y].G = split[1][(u32)(x - _shiftGreen.X), (u32)(y - _shiftGreen.Y)].G;

                if ((x - _shiftBlue.X >= 0 && x - _shiftBlue.X < target.Scale.X) &&
                    (y - _shiftBlue.Y >= 0 && y - _shiftBlue.Y < target.Scale.Y))
                    target[x, y].B = split[2][(u32)(x - _shiftBlue.X), (u32)(y - _shiftBlue.Y)].B;
            }
        }

        /* Free used splits */
        for (u8 i = 0; i < split.Length; ++i)
            split[i].Dispose();
    }

    private UMem2D<RGBA>[] SplitChannels(Image image) {
        UMem2D<RGBA>[] split = new UMem2D<RGBA>[3];

        for(u8 i = 0; i < split.Length; ++i)
            split[i] = new UMem2D<RGBA>(scale: image.Scale, 0x0);

        for (u32 y = 0; y < image.Scale.Y; ++y) {
            for(u32 x = 0; x < image.Scale.X; ++x) {

                split[0][x, y].R = image[x, y].R;
                split[1][x, y].G = image[x, y].G;
                split[2][x, y].B = image[x, y].B;
            }
        }

        return split;
    }
}
