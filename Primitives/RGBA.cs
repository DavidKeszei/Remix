using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Remix.IO;

namespace Remix;

/// <summary>
/// Represent a RGBA pixel in the <see cref="Image"/>.
/// </summary>
[StructLayout(layoutKind: LayoutKind.Explicit)]
public struct RGBA : ICopyFrom<Span<u8>>, ICopyTo<Span<u8>>, IMultiplyOperators<RGBA, f32, RGBA> {
    [FieldOffset(offset: 3)] private u8 _red = 0x00;
    [FieldOffset(offset: 2)] private u8 _green = 0x00;
    [FieldOffset(offset: 1)] private u8 _blue = 0x00;
    [FieldOffset(offset: 0)] private u8 _alpha = 0xff;

    [FieldOffset(offset: 0)] private u32 _interger = 0x000000ff;

    /// <summary>
    /// Red channel.
    /// </summary>
    public u8 R { get => _red; set => _red = value; }

    /// <summary>
    /// Green channel.
    /// </summary>
    public u8 G { get => _green; set => _green = value; }

    /// <summary>
    /// Blue channel.
    /// </summary>
    public u8 B { get => _blue; set => _blue = value; }

    /// <summary>
    /// Transparency channel of the <see cref="RGBA"/>.
    /// </summary>
    public u8 A { get => _alpha; set => _alpha = value; }

    /// <summary>
    /// Intensity of the current <see cref="RGBA"/> instance.
    /// </summary>
    public float Luminance { get => ((_red * 0.299f) + (_green * 0.587f) + (_blue * 0.114f)) / u8.MaxValue; }

    public static RGBA operator*(RGBA px, f32 fValue) {
        px.R = (u8)f32.Clamp(px._red * fValue, 0, u8.MaxValue);
        px.G = (u8)f32.Clamp(px._green * fValue, 0, u8.MaxValue);
        px.B = (u8)f32.Clamp(px._blue * fValue, 0, u8.MaxValue);
        px.A = (u8)f32.Clamp(px._alpha * fValue, 0, u8.MaxValue);

        return px;
    }

	public static RGBA operator *(RGBA px, RGBA pxB) {
		px._red = (u8)f32.Clamp(px._red * pxB.R, 0, u8.MaxValue);
		px._green = (u8)f32.Clamp(px._green * pxB.G, 0, u8.MaxValue);
		px._blue = (u8)f32.Clamp(px._blue * pxB.B, 0, u8.MaxValue);
		px._alpha = (u8)f32.Clamp(px._alpha * pxB.A, 0, u8.MaxValue);

		return px;
	}

	public static RGBA operator +(RGBA a, RGBA b)
        => new RGBA(red: (u8)(a.R + b.R), green: (u8)(a.G + b.G), blue: (u8)(a.B + b.B), alpha: (u8)(a.A + b.A));


	public static implicit operator RGBA(u32 color) => new RGBA(color: color);

    public RGBA(u8 red, u8 green, u8 blue, u8 alpha = 255) {
        this._red = red;
        this._green = green;
        this._blue = blue;

        this._alpha = alpha;
    }

    public RGBA(ReadOnlySpan<u8> rgba) {
        this._red = rgba[0];
        this._green = rgba[1];
        this._blue = rgba[2];

        if (rgba.Length == 3) this._alpha = 255;
        else this._alpha = rgba[3];
    }

    public RGBA(u32 color) => _interger = color;

    /// <summary>
    /// Create a new <see cref="RGBA"/> instance from a <paramref name="hex"/> string.
    /// </summary>
    /// <param name="hex">The color in hex format.</param>
    public RGBA(ReadOnlySpan<char> hex) {
        if (hex[0] == '#') hex = hex[1..];

        this._red = u8.Parse(s: hex.Slice(0, 2));
        this._green = u8.Parse(s: hex.Slice(2, 2));
        this._blue = u8.Parse(s: hex.Slice(4, 2));

        if (hex.Length == 6)
            this._alpha = 255;
        else
            this._alpha = u8.Parse(hex.Slice(6, 2));
    }

    public void CopyFrom(Span<u8> from) {
        switch(from.Length) {
            case 1:
                this._red = from[0];
                break;

            case 2:
                this._red = from[0];
                this._alpha = from[1];
                break;

            case 3:
                this._red = from[0];
                this._green = from[1];
                this._blue = from[2];

                this._alpha = u8.MaxValue;
                break;

            case 4:
                this._red = from[0];
                this._green = from[1];

                this._blue = from[2];
                this._alpha = from[3];
                break;
        }
    }

    public void CopyTo(Span<u8> to) {
        switch(to.Length) {
            case 1:
                to[0] = _red;
                break;

            case 2:
                to[0] = _red;
                to[1] = _alpha;
                break;

            case 3:
                to[0] = _red;
                to[1] = _green;
                to[2] = _blue;
                break;

            case 4:
                to[0] = _red;
                to[1] = _green;
                to[2] = _blue;
                to[3] = _alpha;
                break;
        }
    }

    public readonly override string ToString() => $"0x{_interger:X8}";
}
