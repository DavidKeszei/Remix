using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Layer;

/// <summary>
/// Represent a group of <see cref="Layer"/>s.
/// </summary>
public class LayerGroup {
	private readonly List<Layer> _layers = null!;
	private readonly (u32 X, u32 Y) _scale = (0, 0);

	/// <summary>
	/// Scale of the produced <see cref="Image"/> by the layers.
	/// </summary>
	public (u32 X, u32 Y) Scale { get => _scale; }

	/// <summary>
	/// Get/Set a <see cref="Layer"/> instance inside the <see cref="LayerGroup"/>.
	/// </summary>
	/// <param name="index">Position of the <see cref="Layer"/>.</param>
	/// <returns>Return a <see cref="Layer"/> instance.</returns>
	/// <exception cref="IndexOutOfRangeException"/>
	public Layer this[i32 index] {
		get => this._layers[index];
		set => this._layers[index] = value;
	}
	
	/// <summary>
	/// Create new <see cref="LayerGroup"/> with specific <paramref name="canvasScale"/> from the <paramref name="layers"/>.
	/// </summary>
	/// <param name="canvasScale">Scale of the produced <see cref="Image"/> by the <see cref="LayerGroup"/>.</param>
	/// <param name="layers">Start layers of the <see cref="LayerGroup"/>.</param>
	public LayerGroup((u32 X, u32 Y) canvasScale, params Layer[] layers) {
		this._scale = canvasScale;
		this._layers = new List<Layer>(collection: layers);
	}

	/// <summary>
	/// Create new <see cref="LayerGroup"/> with specific <paramref name="canvasScale"/>. 
	/// </summary>
	/// <param name="canvasScale">Scale of the produced <see cref="Image"/> by the <see cref="LayerGroup"/>.</param>
	public LayerGroup((u32 X, u32 Y) canvasScale) {
		this._scale = canvasScale;
		this._layers = new List<Layer>();
	}

	/// <summary>
	/// Add new <see cref="Layer"/> to the <see cref="LayerGroup"/>.
	/// </summary>
	/// <param name="image">Target <see cref="Image"/> of the <see cref="Layer"/>.</param>
	/// <param name="position">Start position of the <see cref="Layer"/>.</param>
	/// <param name="blendMode">Start blending mode of the <see cref="Layer"/>.</param>
	public void AddLayer(Image image, (i32 X, i32 Y) position, BlendMode blendMode = BlendMode.NORMAL)
		=> this._layers.Add(item: new Layer(image, position, blendMode));
	
	/// <summary>
	/// Remove the specific layer at the <paramref name="index"/>.
	/// </summary>
	/// <param name="index">Index of the <see cref="Layer"/>.</param>
	public void RemoveLayer(u32 index) => _layers.RemoveAt(index: (i32)index);

	/// <summary>
	/// Create a new <see cref="Image"/> from the <see cref="Layer"/>s.
	/// </summary>
	/// <returns>Return a new <see cref="Image"/> instance.</returns>
	public Task<Image> CreateImage(Action<f32> progress = null!, CancellationToken? token = null) {		
		if (_layers.Count == 0) {
			progress?.Invoke(obj: 100f);
			return Task.FromResult<Image>(result: null!);
		}

		Image image = new Image(x: _scale.X, y: _scale.Y, color: 0x00000000);
		Layer layer = null!;

		CopyTo(from: _layers[0].ReferenceImage, to: image);

		if (_layers.Count == 1) {
			progress?.Invoke(obj: 100f);
			return Task.FromResult<Image>(result: image);
		}

		for(u32 y = 0; y < _scale.Y; ++y) {
			for(u32 x = 0; x < _scale.X; ++x) {
				RGBA px = image[x, y];

				for(i32 i = 1; i < _layers.Count; ++i) {
					layer = _layers[i];

					if (layer.Strength == 0 || ((x <= layer.Position.X || x >= layer.Position.X + layer.ReferenceImage.Scale.X) || 
												(y <= layer.Position.Y || y >= layer.Position.Y + layer.ReferenceImage.Scale.Y)))
						continue;

					f32 pxStrength = 1f - layer.Strength;
					RGBA layerPX = layer.ReferenceImage[(u32)((i32)x - layer.Position.X), (u32)((i32)y - layer.Position.Y)];

					switch (layer.BlendMode) {
						case BlendMode.NORMAL: {
								px = (px * pxStrength) + (layerPX * layer.Strength);
								break;
						}
						case BlendMode.MULTIPLY: {
								f32 r = px.R / 255f;
								f32 g = px.G / 255f;
								f32 b = px.B / 255f;

								r = f32.Clamp(r * layerPX.R, 0, u8.MaxValue);
								g = f32.Clamp(g * layerPX.G, 0, u8.MaxValue);
								b = f32.Clamp(b * layerPX.B, 0, u8.MaxValue);

								px = (px * pxStrength) + (new RGBA(red: (u8)r, green: (u8)g, blue: (u8)b) * layer.Strength);
								break;
						}
						case BlendMode.SCREEN: {
								f32 r = (1f - (1f - (px.R / 255f)) * (1f - (layerPX.R / 255f))) * 255f;
								f32 g = (1f - (1f - (px.G / 255f)) * (1f - (layerPX.G / 255f))) * 255f;
								f32 b = (1f - (1f - (px.B / 255f)) * (1f - (layerPX.B / 255f))) * 255f;

								px = (px * pxStrength) + (new RGBA(red: (u8)r, green: (u8)g, blue: (u8)b) * layer.Strength);
								break;
						}
						case BlendMode.DIFFERENCE: {
								f32 r = f32.Abs(px.R - layerPX.R);
								f32 g = f32.Abs(px.G - layerPX.G);
								f32 b = f32.Abs(px.B - layerPX.B);

								px = (px * pxStrength) + (new RGBA(red: (u8)r, green: (u8)g, blue: (u8)b) * layer.Strength);
								break;	
						}
						case BlendMode.SUBTRACT: {
								f32 r = f32.Clamp(px.R - layerPX.R, 0, u8.MaxValue);
								f32 g = f32.Clamp(px.G - layerPX.G, 0, u8.MaxValue);
								f32 b = f32.Clamp(px.B - layerPX.B, 0, u8.MaxValue);

								px = (px * pxStrength) + (new RGBA(red: (u8)r, green: (u8)g, blue: (u8)b) * layer.Strength);
								break;	
						}
						case BlendMode.DARKEN: {
								u8 r = px.R > layerPX.R ? layerPX.R : px.R;
								u8 g = px.G > layerPX.G ? layerPX.G : px.G;
								u8 b = px.B > layerPX.B ? layerPX.B : px.B;

								px = (px * pxStrength) + (new RGBA(red: r, green: g, blue: b, alpha: px.A) * layer.Strength);
								break;	
						}
						case BlendMode.LIGHTEN: {
								u8 r = px.R < layerPX.R ? layerPX.R : px.R;
								u8 g = px.G < layerPX.G ? layerPX.G : px.G;
								u8 b = px.B < layerPX.B ? layerPX.B : px.B;

								px = (px * pxStrength) + (new RGBA(red: r, green: g, blue: b, alpha: px.A) * layer.Strength);
								break;	
						}
					}

					if (token.HasValue && token.Value.IsCancellationRequested) {
						image.Dispose();
						progress?.Invoke(obj: 100f);

						return Task.FromCanceled<Image>(cancellationToken: token.Value);
					}


					image[x, y] = px;
				}
			}

			progress?.Invoke(obj: y / (f32)_scale.Y * 100f);
		}

		progress?.Invoke(obj: 100f);
		return Task.FromResult<Image>(result: image);
	}

	private void CopyTo(Image from, Image to) {
		u32 yScale = from.Scale.Y < to.Scale.Y ? from.Scale.Y : to.Scale.Y;
		u32 xScale = from.Scale.X < to.Scale.X ? from.Scale.X : to.Scale.X;

		for(u32 y = 0; y < yScale; ++y) {
			for(u32 x = 0; x < xScale; ++x) {
				to[x, y] = from[x, y];
			} 
		}
	}
}
