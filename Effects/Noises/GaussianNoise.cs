
namespace Remix.Effect;

/// <summary>
/// Represent a noise filter/effect based on the Gaussian-distribution.
/// </summary>
public class GaussianNoise: Effect {
    private u32 _range = 0;
    private i32 _seed = -1;

    private f32 _threshold = .5f;
    private bool _grayScale = true;

    /// <summary>
    /// Indicates the noise is generated unqiely each color channel.
    /// </summary>
    public bool IsGrayScale { get => _grayScale; set => _grayScale = value; }

    /// <summary>
    /// Indicates "how many" noise appears on the target.
    /// </summary>
    public f32 Threshold { get => _threshold; set => _threshold = value; }

    /// <summary>
    /// Range of the Gaussian-distribution.
    /// </summary>
    public u32 Range { 
        get => _range;
        set {
            if((value & 1) == 0)
                ++value;

            this._range = value;
        }
    }

    /// <summary>
    /// Start seed of the underlying <see cref="Random"/> instance.
    /// </summary>
    public i32 Seed { 
        get => _seed; 
        set => _seed = value <= 0 ? (i32)(DateTime.Now.Ticks % i32.MaxValue) : value;
    }

    /// <summary>
    /// Create new <see cref="GaussianNoise"/> instance with specific <paramref name="range"/> and <paramref name="seed"/>.
    /// </summary>
    /// <param name="range">Range of the Gaussian-kernel.</param>
    /// <param name="seed">Start seed of the underlying <see cref="Random"/> instance.</param>
    public GaussianNoise(u32 range, i32 seed = -1): base(name: nameof(GaussianNoise)) {
        if((range & 1) == 0)
            ++range;

        this._range = range;
        this._seed = seed <= 0 ? (i32)(DateTime.Now.Ticks % i32.MaxValue) : seed;
    }

    public override Task Apply(Image target) {
        Random generator = new Random(Seed: _seed);

        Span<f32> channels = stackalloc f32[3];
        Span<f32> kernel = stackalloc f32[(i32)_range];

        channels.Clear();

        i32 half = kernel.Length / 2;
        kernel.Create1DGaussianKernel(range: (i32)_range, distribution: _range * .33f);

        for(u32 y = 0; y < target.Scale.Y; ++y) {
            for(u32 x = 0; x < target.Scale.X; ++x) {

                if(generator.NextSingle() < _threshold)
                    continue;

                i32 negative = generator.Next(0, 2) == 0 ? 1 : -1;

                for(i32 kernelIndex = -half; kernelIndex <= half; ++kernelIndex) {
                    RGBA px = 0x0000000;

                    if(kernelIndex + x < 0) px = target[(u32)(half + x + kernelIndex), y];
                    else if(kernelIndex + x > target.Scale.X - 1) {
                        u32 mirror = (u32)(target.Scale.X - ((kernelIndex + x) % (target.Scale.X - 1)));
                        px = target[mirror, y];
                    }
                    else {
                        px = target[x, y];
                    }

                    if(!_grayScale) {
                        channels[0] += kernel[kernelIndex + half] * (generator.NextSingle() * u8.MaxValue) * negative;
                        channels[1] += kernel[kernelIndex + half] * (generator.NextSingle() * u8.MaxValue) * negative;
                        channels[2] += kernel[kernelIndex + half] * (generator.NextSingle() * u8.MaxValue) * negative;
                    }
                    else {
                        f32 noiseVal = generator.NextSingle() * u8.MaxValue;
                        channels[0] += kernel[kernelIndex + half] * noiseVal * negative;
                        channels[1] += kernel[kernelIndex + half] * noiseVal * negative;
                        channels[2] += kernel[kernelIndex + half] * noiseVal * negative;
                    }
                }

                RGBA noise = new RGBA(red: (u8)channels[0], green: (u8)channels[1], blue: (u8)channels[2], 255);

                target[x, y].R = (u8)f32.Clamp(target[x, y].R + (noise.R * _strength), 0, 255);
                target[x, y].G = (u8)f32.Clamp(target[x, y].G + (noise.G * _strength), 0, 255);
                target[x, y].B = (u8)f32.Clamp(target[x, y].B + (noise.B * _strength), 0, 255);

                channels.Clear();
            }
        }

        return Task.CompletedTask;
    }
}
