using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Effect;
public class Inverse: Effect {

    public Inverse(): base(name: "Inverse") { }

    public override async Task Apply(Image target) {
        float pxStrength = 1f - _strength;

        for(u32 y = 0; y < target.Scale.Y; ++y) {
            for(u32 x = 0; x < target.Scale.X; ++x) {

                RGBA current = target[x, y];
                current.R = (u8)(u8.MaxValue - current.R);
                current.G = (u8)(u8.MaxValue - current.G);
                current.B = (u8)(u8.MaxValue - current.B);

                target[x, y].R = (u8)((current.R * _strength) + (target[x, y].R * pxStrength));
                target[x, y].G = (u8)((current.G * _strength) + (target[x, y].G * pxStrength));
                target[x, y].B = (u8)((current.B * _strength) + (target[x, y].B * pxStrength));
            }
        }
    }
}
