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
	private (i32 X, i32 Y) _position = (0, 0);

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
	public (i32 X, i32 Y) Position { get => _position; set => _position = value; }

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
	public Layer(Image reference, (i32 X, i32 Y) position, BlendMode mode = BlendMode.NORMAL) {
		this._image = reference;
		this._position = position;
		this._blendMode = mode;
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
