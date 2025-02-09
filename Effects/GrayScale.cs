using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Effect;

/// <summary>
/// Represent a collection of grayscale effect on a <see cref="Image"/>.
/// </summary>
public class GrayScale: Effect {
    private GrayScaleMethod _method = GrayScaleMethod.AVG;
    private float _threshHold = 0.2f;

    /// <summary>
    /// Grayscale method of the effect.
    /// </summary>
    public GrayScaleMethod Method { get => _method; set => _method = value; }

    /// <summary>
    /// Threshold of the black and white effect per pixel.
    /// </summary>
    public float Threshold { get => _threshHold; set => _threshHold = value; }

    public GrayScale(GrayScaleMethod method): base(name: "Grayscale")
        => this._method = method;

    public override async Task Apply(Image target) {
        float _pxStrength = 1 - _strength;

        for(u32 y = 0; y < target.Scale.Y; ++y) {
            for(u32 x = 0; x < target.Scale.X; ++x) {

                ref RGBA px = ref target[x, y];
                u8 grayColor = 0;

                switch(_method) {
                    case GrayScaleMethod.AVG:
                        grayColor = (u8)((px.R + px.G + px.B) / 3);
                        break;

                    case GrayScaleMethod.WEIGTHED:
                        grayColor = (u8)((px.R * 0.299f) + (px.G * 0.587f) + (px.B * 0.114f));
                        break;

                    case GrayScaleMethod.BLACK_AND_WHITE:
                        u32 threshold = (u32)(px.R + px.G + px.B) / 2;

                        if ((765 * _threshHold) < threshold)
                            grayColor = 255;

                        break;
                }

                px.R = (u8)((grayColor * _strength) + (px.R * _pxStrength));
                px.G = (u8)((grayColor * _strength) + (px.G * _pxStrength));
                px.B = (u8)((grayColor * _strength) + (px.B * _pxStrength));
            }
        }
    }
}

public enum GrayScaleMethod: u8 {
    AVG,
    WEIGTHED,
    BLACK_AND_WHITE
}
