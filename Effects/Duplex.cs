using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Effect;

/// <summary>
/// Represent halftone/duotone effect on an <see cref="Image"/> with 2 color.
/// </summary>
public sealed class Duplex: Effect {
    private RGBA _low = RGBA.Transparent;
    private RGBA _high = RGBA.Transparent;

    private bool _isReverse = false;

    /// <summary>
    /// Applied color in the low-color range of the <see cref="Image"/>.
    /// </summary>
    public RGBA Low { get => _low; set => _low = value; }

    /// <summary>
    /// Applied color in the high-color range of the <see cref="Image"/>.
    /// </summary>
    public RGBA High { get => _high; set => _high = value; }

    public bool IsReverse { get => _isReverse; set => _isReverse = value; }

    /// <summary>
    /// Create a new <see cref="Duplex"/> effect with <paramref name="low"/> and <paramref name="high"/> color.
    /// </summary>
    /// <param name="low">Color of the result <see cref="Image"/> in the low color range.</param>
    /// <param name="high">Color of the result <see cref="Image"/> in the high color range.</param>
    public Duplex(RGBA low, RGBA high): base(name: nameof(Duplex)) {
        this._low = low;
        this._high = high;
    }

    public override Task Apply(Image target) {
        f32 pxStrength = 1f - base._strength;
        
        RGBA low = _isReverse ? _high : _low;
        RGBA high = _isReverse ? _low : _high;

        for (u32 y = 0; y < target.Scale.Y; ++y) {
            for (u32 x = 0; x < target.Scale.X; ++x) {

                f32 lowLum = 1f - target[x, y].Luminance;
                RGBA tone = (low * lowLum) + (high * (1f - lowLum));

                target[x, y] = tone * _strength + target[x, y] * pxStrength;
            }
        }

        return Task.CompletedTask;
    }
}
