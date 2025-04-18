using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix;

/// <summary>
/// Represent a color palette from an <see cref="Image"/> or <see cref="RGBA"/> color.
/// </summary>
public readonly struct Palette {
    private readonly RGBA[] _colors = null!;
    private readonly i32 _count = 0;

    public RGBA this[i32 i] { get => _colors[i]; }

    /// <summary>
    /// Number of entries of the <see cref="Palette"/>.
    /// </summary>
    public i32 Count { get => _count; }

    /// <summary>
    /// Create a <see cref="Palette"/> with specific capacity.
    /// </summary>
    /// <param name="capacity">Maximum capacity of the <see cref="Palette"/>.</param>
    public Palette(i32 capacity) {
        capacity = capacity < 2 ? (u8)2u : capacity;

        if (!i32.IsPow2(capacity)) {
            f32 pow = .0f;

            for (u8 i = 2; pow <= 256; ++i) {
                pow = f32.Pow(2, i);

                if (pow > capacity) {
                    capacity = (i32)pow;
                    break;
                }
            }
        }

        this._colors = new RGBA[capacity];
        this._count = capacity;
    }

    /// <summary>
    /// Create a new <see cref="Palette"/> from an <paramref name="image"/>.
    /// </summary>
    /// <param name="image">Source image.</param>
    public void Create(Image image) {
        Span<(i32 min, i32 max)> buckets = stackalloc (i32 min, i32 max)[_count];
        using UMem<RGBA> arr = UMem<RGBA>.Create(allocationLength: image.Scale.X * image.Scale.Y);

        buckets[0] = (0, (i32)arr.Length);
        CopyTo(image, in arr);

        i32 bucketCount = 1;
        i32 pow = 1;

        while (bucketCount <= _count) {
            i32 unit = (i32)arr.Length / bucketCount;

            for (i32 i = 0; i < bucketCount; ++i) {
                buckets[i] = (unit * i, unit * (i + 1));

                Span<RGBA> bucket = arr.AsSpan(from: 0, length: (i32)arr.Length)
                                       .Slice(start: buckets[i].min, length: buckets[i].max - buckets[i].min);

                char channel = GetLargestChannel(bucket);

                switch (channel) {
                    case 'r': {
                        bucket.Sort<RGBA>(comparison: static (x, y) => {

                            if (x.R > y.R) return 1;
                            else if (x.R < y.R) return -1;
                            return 0;
                        });
                        break;
                    }
                    case 'g': {
                        bucket.Sort<RGBA>(comparison: static (x, y) => {

                            if (x.G > y.G) return 1;
                            else if (x.G < y.G) return -1;
                            return 0;
                        });
                        break;
                    }
                    case 'b': {
                        bucket.Sort<RGBA>(comparison: static (x, y) => {

                            if (x.B > y.B) return 1;
                            else if (x.B < y.B) return -1;
                            return 0;
                        });
                        break;
                    }
                }

            }

            bucketCount = (i32)f32.Pow(2, pow++);
        }

        for (i32 i = 0; i < _count; ++i) {
            (i32 r, i32 g, i32 b) = (0, 0, 0);

            for (u32 j = (u32)buckets[i].min; j < buckets[i].max; ++j) {
                r += arr[j].R;
                g += arr[j].G;
                b += arr[j].B;
            }

            _colors[i] = new RGBA((u8)(r / ((i32)arr.Length / _count)), (u8)(g / ((i32)arr.Length / _count)), (u8)(b / ((i32)arr.Length / _count)));
        }
    }

    private char GetLargestChannel(ReadOnlySpan<RGBA> bucket) {
        u32 r = Range(range: bucket.MinMax<RGBA, u8>(x => x.R));
        u32 g = Range(range: bucket.MinMax<RGBA, u8>(x => x.G));
        u32 b = Range(range: bucket.MinMax<RGBA, u8>(x => x.B));

        if (r > g && r > b) return 'r';
        else if (g > b) return 'g';
        else return 'b';
    }

    private void CopyTo(Image image, in UMem<RGBA> to) {
        for (u32 y = 0; y < image.Scale.Y; ++y)
            for (u32 x = 0; x < image.Scale.X; ++x)
                to[x + (y * image.Scale.X)] = image[x, y];
    }

    private u32 Range((u32 min, u32 max) range) => range.max - range.min;
}
