using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Effect;

/// <summary>
/// Represent simple bloom effect on an <see cref="Image"/>.
/// </summary>
public class Bloom: Effect {
	private u32 _range = 0;
	private bool _fastMode = true;

	/// <summary>
	/// Indicates the current bloom effect how applied on the <see cref="Image"/>. (If this is <see langword="true"/>, then bloom effect is fast as possible, 
	/// but the quality of the effect is decreased.)
	/// </summary>
	public bool FastMode { get => _fastMode; set => _fastMode = value; }

	public u32 Range { get => _range; set => _range = value; }

	public Bloom(u32 range, Effect before = null!): base(name: nameof(Bloom)) {
		if (range == 0)
			throw new ArgumentException(message: $"[{base.Name}] The bloom range must be more than 0.");

		this._range = range;
	}

	public override async Task Apply(Image target) {
		if (_strength == 0)
			return;

		/* 1. Create a temporary RGBA array. */
		using UMem2D<RGBA> temp = CreateTempRGBA(target);

		/* 2. Apply some blur effect on the image. */
		Effect gaussianBlur =	_fastMode ? 
								new BoxBlur(range: _range) : 
							    new GaussianBlur(range: _range, distribution: _range / 3f);

		await gaussianBlur.Apply(target);

		/* 3. Check every pixel, which brighter than the other, then we apply these pixels to the result. */
		float pxStrength = 1f - _strength;

		for(u32 y = 0; y < target.Scale.Y; ++y) {
			for(u32 x = 0; x < target.Scale.X; ++x) {
				ref RGBA tempRGBA = ref temp[x, y];
				ref RGBA blurRGBA = ref target[x, y];

				/* Check the each pixel in the blurred image is is brighter than the original pixel in the same position. */
				if (blurRGBA.R > tempRGBA.R)
					blurRGBA.R = (u8)f32.Clamp((tempRGBA.R * pxStrength) + (blurRGBA.R * _strength), 0, u8.MaxValue);
				else blurRGBA.R = tempRGBA.R;

				if (blurRGBA.G > tempRGBA.G)
					blurRGBA.G = (u8)f32.Clamp((tempRGBA.G * pxStrength) + (blurRGBA.G * _strength), 0, u8.MaxValue);
				else blurRGBA.G = tempRGBA.G;

				if (blurRGBA.B > tempRGBA.B)
					blurRGBA.B = (u8)f32.Clamp((tempRGBA.B * pxStrength) + (blurRGBA.B * _strength), 0, u8.MaxValue);
				else blurRGBA.B = tempRGBA.B;
			}
		}
	}

	private UMem2D<RGBA> CreateTempRGBA(Image source) {
		UMem2D<RGBA> res = new UMem2D<RGBA>(scale: source.Scale);

		for(u32 y = 0; y < source.Scale.Y; ++y) {
			for (u32 x = 0; x < source.Scale.X; ++x)
				res[x, y] = source[x, y];
		}

		return res;
	}
}
