using Remix.Effect;
namespace Remix.Effect;

/// <summary>
/// Represent a Salt and Pepper noise.
/// </summary>
public sealed class SaltAndPepper: Effect {
    private f32 _threshold = 1f;

    /// <summary>
    /// Indicates which pixel affected by the effect.
    /// </summary>
    public f32 Threshold { get => _threshold; set => _threshold = value; }

    /// <summary>
    /// Create a new <see cref="SaltAndPepper"/> noise.
    /// </summary>
    /// <param name="threshold">Indicates which pixel affected by the effect.</param>
    public SaltAndPepper(f32 threshold): base(name: $"{nameof(SaltAndPepper)} Noise")
        => this._threshold = f32.Clamp(threshold, .0f, 1f);

    public override Task Apply(Image target) {
        f32 pxStrength = 1f - _strength;

        for(u32 y = 0; y < target.Scale.Y; ++y) {
            for(u32 x = 0;x < target.Scale.X; ++x) {
                u8 noiseValue = (u8)Random.Shared.Next(minValue: 0, maxValue: 2) == 0 ? u8.MinValue : u8.MaxValue;

                if(Random.Shared.NextSingle() > _threshold)
                    continue;

                target[x, y].R = (u8)((target[x, y].R * pxStrength) + (noiseValue * _strength));
                target[x, y].G = (u8)((target[x, y].G * pxStrength) + (noiseValue * _strength));
                target[x, y].B = (u8)((target[x, y].B * pxStrength) + (noiseValue * _strength));
            }
        }

        return Task.CompletedTask;
    }
}