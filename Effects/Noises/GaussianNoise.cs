
namespace Remix.Effect;

public class GaussianNoise: Effect {
    private u32 _range = 0;
    private f32 _threshold = 1f;
    private bool _grayScale = true;

    public bool IsGrayScale { get => _grayScale; set => _grayScale = value; }

    public f32 Treshold { get => _threshold; set => _threshold = value; }

    public u32 Range { 
        get => _range;
        set {
            if((value & 1) == 0)
                ++value;

            this._range = value;
        }
    }

    public GaussianNoise(u32 range): base(name: nameof(GaussianNoise)) {
        if((range & 1) == 0)
            ++range;

        this._range = range;
    }

    public override Task Apply(Image target) {
        Span<f32> channels = stackalloc f32[3];
        Span<f32> kernel = stackalloc f32[(i32)_range];

        channels.Clear();

        i32 half = kernel.Length / 2;
        kernel.Create1DGaussianKernel(range: (i32)_range, distribution: _range * .33f);

        for(u32 y = 0; y < target.Scale.Y; ++y) {
            for(u32 x = 0; x < target.Scale.X; ++x) {

                if(Random.Shared.NextSingle() < _threshold)
                    continue;

                for(i32 kernelIndex = -half; kernelIndex <= half; ++kernelIndex) {
                    RGBA px = 0x0000000;

                    if(kernelIndex + x < 0) px = target[(u32)(half + x + kernelIndex), y];
                    else if(kernelIndex + x > target.Scale.X - 1) {
                        u32 mirror = (u32)(target.Scale.X - ((kernelIndex + x) % (target.Scale.X - 1)));
                    }
                    else {
                        px = target[x, y];
                    }

                    if(!_grayScale) {
                        channels[0] += kernel[kernelIndex + half] * (Random.Shared.NextSingle() * u8.MaxValue);
                        channels[1] += kernel[kernelIndex + half] * (Random.Shared.NextSingle() * u8.MaxValue);
                        channels[2] += kernel[kernelIndex + half] * (Random.Shared.NextSingle() * u8.MaxValue);
                    }
                    else {
                        f32 noiseVal = Random.Shared.NextSingle() * u8.MaxValue;
                        channels[0] += kernel[kernelIndex + half] * noiseVal;
                        channels[1] += kernel[kernelIndex + half] * noiseVal;
                        channels[2] += kernel[kernelIndex + half] * noiseVal;
                    }
                }

                RGBA noise = new RGBA(red: (u8)channels[0], green: (u8)channels[1], blue: (u8)channels[2], 255);

                target[x, y].R = (u8)f32.Clamp(target[x, y].R + (noise.R * _strength), 0, 255);
                target[x, y].G = (u8)f32.Clamp(target[x, y].G + (noise.G * _strength), 0, 255);
                target[x, y].B = (u8)f32.Clamp(target[x, y].B + (noise.B * _strength), 0, 255);
                target[x, y].A = u8.MaxValue;

                channels.Clear();
            }
        }

        return Task.CompletedTask;
    }
}
