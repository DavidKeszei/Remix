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
public struct RGBA: ICopyFrom<Span<u8>>, ICopyTo<Span<u8>>, IMultiplyOperators<RGBA, f32, RGBA> {

	[FieldOffset(offset: 3)] private u8 _red = 0x00;
	[FieldOffset(offset: 2)] private u8 _green = 0x00;
	[FieldOffset(offset: 1)] private u8 _blue = 0x00;
	[FieldOffset(offset: 0)] private u8 _alpha = 0xff;

	[FieldOffset(offset: 0)] private u32 _integer = 0x000000ff;

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
	public readonly float Luminance { get => ((_red * .299f) + (_green * .587f) + (_blue * .114f)) / u8.MaxValue; }

#region < STATICS >

	public static RGBA Transparent { get => 0u; }

	public static RGBA Black { get => 0x000000ff; }

	public static RGBA White { get => u32.MaxValue; }

	public static RGBA Red { get => 0xff0000ff; }

	public static RGBA Green { get => 0x00ff00ff; }

	public static RGBA Blue { get => 0x0000ffff; }

#endregion

#region < OPERATORS >

	public static RGBA operator*(RGBA px, f32 fValue) {
		px.R = (u8)f32.Clamp(px._red * fValue, 0, u8.MaxValue);
		px.G = (u8)f32.Clamp(px._green * fValue, 0, u8.MaxValue);
		px.B = (u8)f32.Clamp(px._blue * fValue, 0, u8.MaxValue);
		px.A = (u8)f32.Clamp(px._alpha * fValue, 0, u8.MaxValue);

		return px;
	}

	public static RGBA operator +(RGBA a, RGBA b)
		=> new RGBA(red: (u8)(a.R + b.R), green: (u8)(a.G + b.G), blue: (u8)(a.B + b.B), alpha: (u8)(a.A + b.A));

	public static RGBA operator-(RGBA a, RGBA b) {
		a.R = (u8)i32.Clamp(a.R - b.R, u8.MinValue, u8.MaxValue);
		a.G = (u8)i32.Clamp(a.G - b.G, u8.MinValue, u8.MaxValue);
		a.B = (u8)i32.Clamp(a.B - b.B, u8.MinValue, u8.MaxValue);
		a.A = (u8)i32.Clamp(a.A - b.A, u8.MinValue, u8.MaxValue);

		return a;
	}

	public static implicit operator RGBA(u32 color) => new RGBA(color: color);

#endregion

	public RGBA(u8 red, u8 green, u8 blue, u8 alpha = 255) {
		this._red = red;
		this._green = green;
		this._blue = blue;

		this._alpha = alpha;
	}

	public RGBA(u32 color) => _integer = color;

	/// <summary>
	/// Apply linear interpolation between 2 <see cref="RGBA"/> instance.
	/// </summary>
	/// <param name="from">Minimum value.</param>
	/// <param name="to">Maximum value.</param>
	/// <param name="time">Distance between the 2 colors.</param>
	/// <returns>Return a new <see cref="RGBA"/> instance, which sits between the 2 <see cref="RGBA"/> colors.</returns>
	public static RGBA Lerp(RGBA from, RGBA to, f32 time) {
		RGBA lerp = 0x0;
		lerp.R = (u8)f32.Abs(from.R + (to.R - from.R) * time);
		lerp.G = (u8)f32.Abs(from.G + (to.G - from.G) * time);

		lerp.B = (u8)f32.Abs(from.B + (to.B - from.B) * time);
		lerp.A = (u8)f32.Abs(from.A + (to.A - from.A) * time);

		return lerp;
	}

	/// <summary>
	/// Create a new <see cref="RGBA"/> instance from a <paramref name="hex"/> string.
	/// </summary>
	/// <param name="hex">The color in hex format.</param>
	public RGBA(ReadOnlySpan<char> hex) {
		if (hex[0] == '#') hex = hex[1..];

		this._red = u8.Parse(s: hex.Slice(0, 2));
		this._green = u8.Parse(s: hex.Slice(2, 2));
		this._blue = u8.Parse(s: hex.Slice(4, 2));

		if (hex.Length == 6) this._alpha = 255;
		else this._alpha = u8.Parse(hex.Slice(6, 2));
	}

	public void CopyFrom(Span<u8> from) {
		switch(from.Length) {
			case 1:
				this._red = from[0];
				this._green = from[0];
				this._blue = from[0];
				
				this._alpha = u8.MaxValue;
				break;

			case 2:
				this._red = from[0];
				this._green = from[0];
				this._blue = from[0];
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

	public readonly override string ToString() => $"0x{_integer:X8}";
}
