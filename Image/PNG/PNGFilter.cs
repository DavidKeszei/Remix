namespace Remix.Helpers;

/// <summary>
/// Provides filter decode/encode functions for <see cref="PNG"/> objects.
/// </summary>
internal class PNGFilter {
    private readonly bool _isDecode = false;

    public PNGFilter(bool decode) => _isDecode = decode;

    public void PrimitiveFilter(u8 channelCount, RGBA before, ref RGBA filtered) {
        for (u8 i = 0; i < channelCount; ++i) {
            switch (i) {
                case 0:
                    filtered.R = (u8)(filtered.R + before.R * (_isDecode ? 1 : -1));
                    break;
                case 1:
                    filtered.G = (u8)(filtered.G + before.G * (_isDecode ? 1 : -1));
                    break;
                case 2:
                    filtered.B = (u8)(filtered.B + before.B * (_isDecode ? 1 : -1));
                    break;
                case 3:
                    filtered.A = (u8)(filtered.A + before.A * (_isDecode ? 1 : -1));
                    break;
            }
        }
    }

    public void AvgFilter(u8 channelCount, RGBA up, RGBA sub, ref RGBA filtered) {
        for (u8 i = 0; i < channelCount; ++i) {
            switch (i) {
                case 0:
                    filtered.R = (u8)(filtered.R + ((up.R + sub.R) / 2 * (_isDecode ? 1 : -1)));
                    break;
                case 1:
                    filtered.G = (u8)(filtered.G + ((up.G + sub.G) / 2 * (_isDecode ? 1 : -1)));
                    break;
                case 2:
                    filtered.B = (u8)(filtered.B + ((up.B + sub.B) / 2 * (_isDecode ? 1 : -1)));
                    break;
                case 3:
                    filtered.A = (u8)(filtered.A + ((up.A + sub.A) / 2 * (_isDecode ? 1 : -1)));
                    break;
            }
        }
    }

    public void PaethFilter(u8 ch, RGBA up, RGBA sub, RGBA sub_up, ref RGBA filtered) {
        for (u8 i = 0; i < ch; ++i) {
            switch (i) {
                case 0:
                    filtered.R = (u8)(filtered.R + PaethPredictor(up.R, sub.R, sub_up.R) * (_isDecode ? 1 : -1));
                    break;
                case 1:
                    filtered.G = (u8)(filtered.G + PaethPredictor(up.G, sub.G, sub_up.G) * (_isDecode ? 1 : -1));
                    break;
                case 2:
                    filtered.B = (u8)(filtered.B + PaethPredictor(up.B, sub.B, sub_up.B) * (_isDecode ? 1 : -1));
                    break;
                case 3:
                    filtered.A = (u8)(filtered.A + PaethPredictor(up.A, sub.A, sub_up.A) * (_isDecode ? 1 : -1));
                    break;
            }
        }
    }

    private u8 PaethPredictor(u8 a, u8 b, u8 c) {
        i32 p = a + b - c;
        i32 ap = Math.Abs(p - a);

        i32 ab = Math.Abs(p - b);
        i32 ac = Math.Abs(p - c);

        if (ap <= ab && ap <= ac) return a;
        else if (ab <= ac) return b;

        return c;
    }
}
