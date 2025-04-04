using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Remix;

internal static class MathExtension {
    private const f32 TWO_PI_IN_SQRT = 2.5066282746f;

    public static void Create1DGaussianKernel(this Span<f32> line, i32 range, f32 distribution) {
        bool isEven = (range & 1) == 0;
        i32 half = range / 2;

        f32 sum = 0f;

        for (i32 i = -half; isEven ? i < half : i <= half; ++i) {
            line[i + half] = GaussianDistribution(i, distribution);
            sum += line[i + half];
        }

        /* Kernel normalization */
        for (i32 i = 0; i < range; ++i)
            line[i] /= sum;
    }

    private static f32 GaussianDistribution(i32 x, f32 sigma) {
        f32 _base = 1f / (TWO_PI_IN_SQRT * sigma);
        f32 _exp = MathF.Pow(x: MathF.E, y: -(MathF.Pow(x, 2) / (2f * MathF.Pow(sigma, 2f))));

        return _base * _exp;
    }

    public static bool IsCloseTo(this f32 num, f32 to, f32 threshold)
        => f32.Abs(to - num) < threshold;

    public static f32 EuclidianDistance(this RGBA from, RGBA to)
        => MathF.Sqrt(x: MathF.Pow(from.R - to.R, 2) + MathF.Pow(from.G - to.G, 2) + MathF.Pow(from.B - to.B, 2));

    public static (TOut Min, TOut Max) MinMax<TIn, TOut>(this ReadOnlySpan<TIn> buffer, Func<TIn, TOut> selector) where TOut: IComparisonOperators<TOut, TOut, bool> {
        if (buffer.Length == 0)
            return (Min: default!, Max: default!);

        TOut max = selector(buffer[0]);
        TOut min = selector(buffer[0]);

        if (buffer.Length == 1) return (min, max);

        for (i32 i = 1; i < buffer.Length; ++i) {
            TOut current = selector(buffer[i]);

            if (current > max) max = current;
            if (current < min) min = current;
        }

        return (min, max);
    }
}
