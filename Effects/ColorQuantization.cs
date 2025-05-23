﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Effect;

/// <summary>
/// Represent a color quantization effect on an <see cref="Image"/>.
/// </summary>
public sealed class ColorQuantization: Effect {
    private static Task[] _workers = null!;

    private Palette _palette = default!;
    private bool _isPreGenerated = true;

    /// <summary>
    /// Underlying palette of the effect.
    /// </summary>
    public Palette Palette { get => _palette; set => _palette = value; }

    static ColorQuantization()
        => _workers = new Task[Environment.ProcessorCount];

    /// <summary>
    /// Create new <see cref="ColorQuantization"/> effect from a <paramref name="palette"/>.
    /// </summary>
    /// <param name="palette">Pregenerated palette of the effect.</param>
    public ColorQuantization(Palette palette): base(name: nameof(ColorQuantization)) {
        this._palette = palette;
        this._isPreGenerated = false;
    }

    /// <summary>
    /// Create new <see cref="ColorQuantization"/> effect with a specific <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">Capacity of the underlying palette.</param>
    public ColorQuantization(i32 capacity) : base(name: nameof(ColorQuantization)) {
        this._palette = new Palette(capacity: capacity);
        this._isPreGenerated = false;
    }

    public override async Task Apply(Image target) {
        if (_strength <= 0) return;
        if (!_isPreGenerated) _palette.Create(image: target);

        f32 pxStrength = 1f - base._strength;
        u32 rowCount = target.Scale.Y < Environment.ProcessorCount ? target.Scale.Y : (u32)Environment.ProcessorCount;

        for (u32 y = 0; y < target.Scale.Y; y += rowCount) {
            u32 yRef = y;
            rowCount = target.Scale.Y - y < Environment.ProcessorCount ? target.Scale.Y - y : (u32)Environment.ProcessorCount;

            for (u32 w = 0; w < rowCount; ++w) {
                u32 wRef = w;
                _workers[w] = Task.Run(action: () => {

                    for (u32 x = 0; x < target.Scale.X; ++x) {
                        f32 smallest = _palette[0].EuclidianDistance(target[x, yRef + wRef]);
                        i32 color = 0;

                        for (i32 i = 1; i < _palette.Count; ++i) {
                            f32 current = _palette[i].EuclidianDistance(target[x, yRef + wRef]);

                            if (current < smallest) {
                                smallest = current;
                                color = i;
                            }
                        }

                        RGBA paletteColor = _palette[color];
                        target[x, yRef + wRef] = target[x, yRef + wRef] * pxStrength + paletteColor * _strength;
                    }
                });
            }

            await Task.WhenAll(tasks: _workers);
        }

        return;
    }
}
