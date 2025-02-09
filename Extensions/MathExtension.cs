using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Remix;
public static class MathExtension {

    public static void Create1DGaussianKernel(this Span<f32> line, i32 range, f32 distribution) {
        bool isEven = (range & 1) == 0;
        i32 half = range / 2;

        f32 sum = 0f;

        for (i32 i = -half; isEven ? i < half : i <= half; ++i) {
            f32 _base = 1f / (MathF.Sqrt(2f * MathF.PI) * distribution);
            f32 e = MathF.Pow(MathF.E, -(MathF.Pow(i, 2) / (2 * MathF.Pow(distribution, 2))));

            _base *= e;

            sum += _base;
            line[i + half] = _base;
        }

        /* Kernel normalization */
        for (i32 i = 0; i < range; ++i)
            line[i] /= sum;
    }

    public static f32 CalculateGaussianValue(this f32 number, f32 distribution) {
        f32 _base = 1f / (MathF.Sqrt(2 * MathF.PI) / distribution);
        f32 e = MathF.Pow(MathF.E, -(MathF.Pow(number, 2) / (2 * MathF.Pow(distribution, 2))));

        return _base * e;
    }

    public static bool IsCloseTo(this f32 num, f32 to, f32 threshold)
        => f32.Abs(to - num) < threshold;

    public static f32 EuclidianDistance(this RGBA from, RGBA to)
        => MathF.Sqrt(x: MathF.Pow(from.R - to.R, 2) + MathF.Pow(from.G - to.G, 2) + MathF.Pow(from.B - to.B, 2));
}
