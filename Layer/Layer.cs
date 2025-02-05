using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Layer;

/// <summary>
/// Represent a <see cref="Layer"/> in the <see cref="LayerGroup"/>.
/// </summary>
public class Layer {
	private Image _image = null!;
	private (u32 X, u32 Y) _position = (0, 0);

	private f32 _strength = 1f;
	private BlendMode _blendMode = BlendMode.NORMAL;

	/// <summary>
	/// Currently hold reference <see cref="Image"/> by the <see cref="Layer"/>.
	/// </summary>
	public Image ReferenceImage { get => _image; }

	/// <summary>
	/// Transparency/Strength/Opacity of the <see cref="Layer"/>.
	/// </summary>
	public f32 Strength { get => _strength; set => _strength = value; }

	/// <summary>
	/// Position of the <see cref="Layer"/> in the <see cref="LayerGroup"/>.
	/// </summary>
	public (u32 X, u32 Y) Position { get => _position; set => _position = value; }

	/// <summary>
	/// Indicates how blends this <see cref="Layer"/> to other <see cref="Layer"/>s. 
	/// </summary>
	public BlendMode BlendMode { get => _blendMode; set => _blendMode = value; }

	/// <summary>
	/// Create a new <see cref="Layer"/> from a <paramref name="reference"/> image on the specific <paramref name="position"/>.
	/// </summary>
	/// <param name="reference">The image self.</param>
	/// <param name="position">Start position on the <see cref="LayerGroup"/>.</param>
	/// <param name="mode">Init. blend mode of the <see cref="Layer"/>.</param>
	public Layer(Image reference, (u32 X, u32 Y) position, BlendMode mode = BlendMode.NORMAL) {
		this._image = reference;
		this._position = position;
		this._blendMode = mode;
	}

	public Image Blend((u32 X, u32 Y) canvasSize, Image topReference = null!) {
		if (topReference == null) return this._image;

		switch(_blendMode) {
			case BlendMode.NORMAL:
				NormalBlend(canvasSize, topReference);
				break;
			case BlendMode.MULTIPLY:
				MultiplyBlend(canvasSize, topReference);
				break;
			case BlendMode.SCREEN:
				SpaceBlend(canvasSize, topReference);
				break;
			case BlendMode.DIFFERENCE:
				DifferenceBlend(canvasSize, topReference);
				break;
			case BlendMode.SUBTRACT:
				SubtractBlend(canvasSize, topReference);
				break;
		}

		return topReference;
	}

	private void NormalBlend((u32 X, u32 Y) canvasSize, Image source) {
		f32 sourceStrength = 1f - _strength;

		for(u32 y = _position.Y; y < canvasSize.Y; ++y) {
			for(u32 x = _position.X; x < canvasSize.X; ++x) {
				if (source[x, y].A == 0f)
					continue;

				source[x, y] = (source[x, y] * sourceStrength) + (_image[(u32)((i32)x - _position.X), (u32)((i32)y - _position.Y)] * _strength);
			}
		}
	}

	private void MultiplyBlend((u32 X, u32 Y) canvasSize, Image source) {
		f32 sourceStrength = 1f - _strength;

		for(u32 y = _position.Y; y < canvasSize.Y; ++y) {
			for(u32 x = _position.X; x < canvasSize.X; ++x) {
				if (source[x, y].A == 0f)
					continue;
				RGBA multi = _image[(u32)((i32)x - _position.X), (u32)((i32)y - _position.Y)];

				f32 r = source[x, y].R / 255f;
				f32 b = source[x, y].B / 255f;
				f32 g = source[x, y].G / 255f;
				f32 a = source[x, y].A / 255f;

				multi.R = (u8)f32.Clamp(multi.R * r, 0, 255f);
				multi.G = (u8)f32.Clamp(multi.G * g, 0, 255f);

				multi.B = (u8)f32.Clamp(multi.B * b, 0, 255f);
				source[x, y] = (source[x, y] * sourceStrength) + (multi * _strength);
			}
		}
	}

	private void SpaceBlend((u32 X, u32 Y) canvasSize, Image source) {
		f32 sourceStrength = 1f - _strength;

		for(u32 y = _position.Y; y < canvasSize.Y; ++y) {
			for(u32 x = _position.X; x < canvasSize.X; ++x) {
				if (source[x, y].A == 0f)
					continue;

				RGBA screenPx = _image[x - _position.X, y - _position.Y];
				RGBA sourcePx = source[x, y];

				f32 r = f32.Clamp((1f - (1f - (screenPx.R / 255f)) * (1f - (sourcePx.R / 255f))) * 255f, 0, 255f);
				f32 g = f32.Clamp((1f - (1f - (screenPx.G / 255f)) * (1f - (sourcePx.B / 255f))) * 255f, 0, 255f);
				f32 b = f32.Clamp((1f - (1f - (screenPx.B / 255f)) * (1f - (sourcePx.B / 255f))) * 255f, 0, 255f);

				source[x, y] = (sourcePx * sourceStrength) + 
							   (new RGBA(red: (u8)r, green: (u8)g, blue: (u8)b) * _strength);
			}
		} 
	}

	private void DifferenceBlend((u32 X, u32 Y) canvasSize, Image source) {
		f32 sourceStrength = 1f - _strength;

		for(u32 y = _position.Y; y < canvasSize.Y; ++y) {
			for(u32 x = _position.X; x < canvasSize.X; ++x) {
				if (source[x, y].A == 0f)
					continue;

				RGBA screenPx = _image[x - _position.X, y - _position.Y];
				RGBA sourcePx = source[x, y];

				f32 r = f32.Abs(sourcePx.R - screenPx.R);
				f32 g = f32.Abs(sourcePx.G - screenPx.G);
				f32 b = f32.Abs(sourcePx.B - screenPx.B);

				source[x, y] = (sourcePx * sourceStrength) +
							   (new RGBA(red: (u8)r, green: (u8)g, blue: (u8)b) * _strength);
			}
		} 
	}

	private void SubtractBlend((u32 X, u32 Y) canvasSize, Image source) {
		f32 sourceStrength = 1f - _strength;

		for(u32 y = _position.Y; y < canvasSize.Y; ++y) {
			for(u32 x = _position.X; x < canvasSize.X; ++x) {
				if (source[x, y].A == 0f)
					continue;

				RGBA screenPx = _image[x - _position.X, y - _position.Y];
				RGBA sourcePx = source[x, y];

				f32 r = f32.Clamp(sourcePx.R - screenPx.R, u8.MinValue, u8.MaxValue);
				f32 g = f32.Clamp(sourcePx.G - screenPx.G, u8.MinValue, u8.MaxValue);
				f32 b = f32.Clamp(sourcePx.B - screenPx.B, u8.MinValue, u8.MaxValue);

				source[x, y] = (sourcePx * sourceStrength) +
							   (new RGBA(red: (u8)r, green: (u8)g, blue: (u8)b) * _strength);
			}
		} 
	}
}

/// <summary>
/// Simple collection of the available blend modes.
/// </summary>
public enum BlendMode: u8 {
	NORMAL,
	DARKEN,
	LIGHTEN,
	MULTIPLY,
	DIFFERENCE,
	SCREEN,
	SUBTRACT
}
