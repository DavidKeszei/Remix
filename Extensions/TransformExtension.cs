using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Remix;

public static class TransformExtension {
	[ThreadStatic]
	private static Task[] _workers = null!;

	/// <summary>
	/// Flip the current <paramref name="image"/> in an axis.
	/// </summary>
	/// <param name="image">Target of the flip.</param>
	/// <param name="direction">Flip direction.</param>
	public static void Flip(this Image image, FlipDirection direction) {
		if(direction == FlipDirection.VERTICAL) {

			for(u32 x = 0; x < image.Scale.X; ++x) {
				for(u32 y = 0; y < image.Scale.Y / 2; ++y) {

					RGBA other = image[x, image.Scale.Y - y - 1];
					image[x, image.Scale.Y - y - 1] = image[x, y];

					image[x, y] = other;
				}
			}
			return;
		}

		for(u32 y = 0; y < image.Scale.Y; ++y) {
			for(u32 x = 0; x < image.Scale.X / 2; ++x) {

				RGBA other = image[image.Scale.X - x - 1, y];
				image[image.Scale.X - x - 1, y] = image[x, y];

				image[x, y] = other;
			}
		}
	}

	/// <summary>
	/// Create a new resized <see cref="Image"/> from the <paramref name="source"/> image.
	/// </summary>
	/// <param name="source">The image self.</param>
	/// <param name="x">New scale in the X axis.</param>
	/// <param name="y">New scale in the Y axis.</param>
	/// <returns>Return a new <see cref="Image"/> instance with the specific scale.</returns>
	/// <exception cref="InvalidOperationException"/>
	public static void Resize(this Image source, u32 x, u32 y, UpScaleMethod method = UpScaleMethod.BILINEAR) {
		if (source.Scale.X == x && source.Scale.Y == y) return;
		_workers ??= new Task[Environment.ProcessorCount];

		using UMem2D<RGBA> horizontal = new UMem2D<RGBA>((x, source.Scale.Y), 0x0);
		UMem2D<RGBA> vertical = new UMem2D<RGBA>((x, y), 0x0); /* This buffer may owned by the resized image. */

		if (x >= source.Scale.X) {
			switch(method) {
				case UpScaleMethod.NEAREST:
					NearestNeighborX(old: source, destination: horizontal);
					break;
				case UpScaleMethod.BILINEAR:
					BilinearX(old: source, destination: horizontal);
					break;
			}
		}
		else {
			GaussDownSizeX(source, horizontal);
		}

		if(y >= source.Scale.Y) {
			switch(method) {
				case UpScaleMethod.NEAREST:
					NearestNeighborY(old: horizontal, destination: vertical);
					break;
				case UpScaleMethod.BILINEAR:
					BilinearY(old: horizontal, destination: vertical);
					break;
			}
		}
		else {
			GaussDownSizeY(horizontal, vertical);
		}

		source.SwapBuffer(source: vertical);
	}

	/// <summary>
	/// Change the current <paramref name="gamma"/> of the <paramref name="image"/>.
	/// </summary>
	/// <param name="image">Target of the change.</param>
	/// <param name="gamma">New gamma value of the image.</param>
	/// <remarks><b>Remark: </b> If the <paramref name="gamma"/> value is less than 0, then the <paramref name="gamma"/> value is equal with 0.</remarks>
	public static void ChangeGamma(this Image image, f32 gamma) {
		if (gamma < 0) gamma = 0f;

		for (u32 y = 0; y < image.Scale.Y; ++y) {
			for (u32 x = 0; x < image.Scale.X; ++x) {
				ref RGBA rgb = ref image[x, y];

				rgb.R = (u8)f32.Clamp(f32.Pow(rgb.R / 255f, gamma) * 255f, u8.MinValue, u8.MaxValue);
				rgb.G = (u8)f32.Clamp(f32.Pow(rgb.G / 255f, gamma) * 255f, u8.MinValue, u8.MaxValue);
				rgb.B = (u8)f32.Clamp(f32.Pow(rgb.B / 255f, gamma) * 255f, u8.MinValue, u8.MaxValue);
            }
		}
	}

	/// <summary>
	/// Change the current <paramref name="brightness"/> of the <paramref name="image"/>.
	/// </summary>
	/// <param name="image">Target image.</param>
	/// <param name="brightness">The amount of the brightness value, which added to the <paramref name="image"/>.</param>
	public static void ChangeBrightness(this Image image, f32 brightness) {
		for (u32 y = 0; y < image.Scale.Y; ++y) {
			for (u32 x = 0; x < image.Scale.X; ++x) {
				ref RGBA px = ref image[x, y];

				px.R = (u8)f32.Clamp(px.R + brightness * 255f, u8.MinValue, u8.MaxValue);
				px.G = (u8)f32.Clamp(px.G + brightness * 255f, u8.MinValue, u8.MaxValue);
				px.B = (u8)f32.Clamp(px.B + brightness * 255f, u8.MinValue, u8.MaxValue);
            }
		}
	}

	/// <summary>
	/// Change the contrast of the <paramref name="image"/>.
	/// </summary>
	/// <param name="image">Target image.</param>
	/// <param name="contrast">Contrast value, which applied on the <paramref name="image"/>.</param>
	public static void ChangeContrast(this Image image, f32 contrast) {
		if (contrast == 0)
			return;

		contrast = f32.Clamp(contrast, -1, 1);

		for (u32 y = 0; y < image.Scale.Y; ++y) {
			for (u32 x = 0; x < image.Scale.X; ++x) {
				ref RGBA rgba = ref image[x, y];

				if (contrast < 0) {
					f32 strength = 0 - contrast;

                    rgba.R = (u8)(rgba.R * (1 - strength) + 128f * strength);
                    rgba.G = (u8)(rgba.G * (1 - strength) + 128f * strength);
                    rgba.B = (u8)(rgba.B * (1 - strength) + 128f * strength);

					continue;
                }

				/*
					Math for increase contrast of the image:
						channel = current_ch_vl + (change_vl * abs(norm_distance)) -> 0..255;
				 */

				f32 rCont = contrast * 255f * (rgba.R < 128 ? -1 : 1);
				f32 gCont = contrast * 255f * (rgba.G < 128 ? -1 : 1);
				f32 bCont = contrast * 255f * (rgba.B < 128 ? -1 : 1);

                rgba.R = (u8)f32.Clamp(rgba.R + rCont * f32.Abs((128 - rgba.R) / 128f), 0, 255f);
                rgba.G = (u8)f32.Clamp(rgba.G + gCont * f32.Abs((128 - rgba.G) / 128f), 0, 255f);
                rgba.B = (u8)f32.Clamp(rgba.B + bCont * f32.Abs((128 - rgba.B) / 128f), 0, 255f);
            }
		}
	}

	/// <summary>
	/// Create a <see cref="Dictionary{TKey, TValue}"/>, which contains the histogram entries of the image.
	/// </summary>
	/// <param name="image">Target image.</param>
	/// <param name="where">Selector function.</param>
	/// <returns>Return a new <see cref="Dictionary{TKey, TValue}"/> instance.</returns>
	/// <exception cref="ArgumentException"/>
	public static Dictionary<TNumber, i32> CreateHistogram<TNumber>(this Image image, Func<RGBA, TNumber> where) where TNumber: INumber<TNumber> {
		if (where == null!)
			throw new ArgumentException(message: $"The {nameof(where)} argument must be not NULL value.");

		Dictionary<TNumber, i32> hist = new Dictionary<TNumber, i32>(capacity: 255);

        for (u32 y = 0; y < image.Scale.Y; ++y) {
            for (u32 x = 0; x < image.Scale.X; ++x) {
                ref RGBA px = ref image[x, y];

				_ = hist.TryAdd(key: where(px), 0);
				++hist[where(px)];
            }
        }

		return hist;
    }

    #region < HELPERS >

    private static void NearestNeighborX(Image old, UMem2D<RGBA> destination) {
		f32 ratio = destination.Scale.X / (f32)old.Scale.X;

		for(u32 y = 0; y < destination.Scale.Y; ++y) {
			for(u32 x = 0; x < destination.Scale.X; ++x) {

				u32 neighbor = (u32)MathF.Floor(x / ratio);
				destination[x, y] = old[neighbor, y];
			}
		}
	}

	private static void NearestNeighborY(UMem2D<RGBA> old, UMem2D<RGBA> destination) {
		f32 ratio = destination.Scale.Y / (f32)old.Scale.Y;

		for(u32 x = 0; x < destination.Scale.X; ++x) {
			for(u32 y = 0; y < destination.Scale.Y; ++y) {

				u32 neighbor = (u32)MathF.Floor(y / ratio);
				destination[x, y] = old[x, neighbor];
			}
		}
	}

	private static void BilinearX(Image old, UMem2D<RGBA> destination) {
		f32 ratio = old.Scale.X == 1 ? 1 :
					old.Scale.X <= 2 ? destination.Scale.X - 1 : 
						(destination.Scale.X - 1) / (f32)old.Scale.X;

		u32 workerCount = (u32)_workers.Length;

		for(u32 y = 0; y < destination.Scale.Y; y += workerCount) {
			u32 yRef = y;
			workerCount = destination.Scale.Y - y > _workers.Length ? (u32)_workers.Length : destination.Scale.Y - y;

			for(i32 w = 0; w < workerCount; ++w) {
				u32 wRef = (u32)w;

				_workers[w] = Task.Run(action: () => {
					f32 barrier = ratio;
					f32 barrierBefore = .0f;

					RGBA from = old[0, yRef + wRef];
					RGBA to = old.Scale.X == 1 ? old[0, yRef + wRef] : old[1, yRef + wRef];

					for (u32 x = 0; x < destination.Scale.X; ++x) {
						if (x >= barrier && barrier / ratio < old.Scale.X - 1) {

							barrierBefore = barrier;
							barrier += ratio;

							from = to;
							to = old[(u32)(barrier / ratio), yRef + wRef];
						}

						f32 current = (x - barrierBefore) / ratio;
						destination[x, yRef + wRef] = RGBA.Lerp(from, to, current);
					}
				});
			}

			Task.WaitAll(tasks: _workers);
		}
	}

	private static void BilinearY(UMem2D<RGBA> old, UMem2D<RGBA> destination) {
		f32 ratio = old.Scale.Y == 1 ? 1 :
						old.Scale.Y <= 2 ? destination.Scale.Y - 1 :
										   (destination.Scale.Y - 1) / (f32)old.Scale.Y;

		u32 workerCount = (u32)_workers.Length;

		for(u32 x = 0; x < destination.Scale.X; x += workerCount) {
			u32 xRef = x;
			workerCount = destination.Scale.X - x > _workers.Length ? (u32)_workers.Length : destination.Scale.X - x;

			for(i32 w = 0; w < workerCount; ++w) {
				u32 wRef = (u32)w;

				_workers[w] = Task.Run(action: () => {
					f32 barrier = ratio;
					f32 barrierBefore = .0f;

					RGBA from = old[xRef + wRef, 0];
					RGBA to = old.Scale.Y == 1 ? old[xRef + wRef, 0] : old[xRef + wRef, 1];

					for (u32 y = 0; y < destination.Scale.Y; ++y) {
						if (y >= barrier && barrier / ratio < old.Scale.Y - 1) {

							barrierBefore = barrier;
							barrier += ratio;

							from = to;
							to = old[xRef + wRef, (u32)(barrier / ratio)];
						}

						f32 current = (y - barrierBefore) / ratio;
						destination[xRef + wRef, y] = RGBA.Lerp(from, to, current);
					}
				});
			}

			Task.WaitAll(tasks: _workers);
		}
	}

	private static void GaussDownSizeX(Image old, UMem2D<RGBA> destination) {
		f32 ratio = old.Scale.X / (f32)destination.Scale.X;
		u32 oddRatio = (u32)ratio;

		oddRatio = (oddRatio & 1) == 0 ? ++oddRatio : oddRatio;
		i32 half = (i32)oddRatio / 2;

		Span<f32> gauss = stackalloc f32[(i32)oddRatio];
		Span<f32> ch = stackalloc f32[4];

		ch.Clear();
		gauss.Create1DGaussianKernel(range: (i32)oddRatio, distribution: oddRatio);

		for(u32 y = 0; y < old.Scale.Y; ++y) {
			u32 downXIndex = 0;

			for (f32 x = 0; x < old.Scale.X && !x.IsCloseTo(to: old.Scale.X, threshold: .5f); x += ratio) {
				RGBA color = 0x0;

				for(i32 i = (i32)x - half; i <= x + half; ++i) {

					if(i < 0) {
						color = old[(u32)i32.Abs(i), y];
					}
					else if(i >= old.Scale.X) {
						u32 mirror = (u32)(old.Scale.X - i % (old.Scale.X - 1));
						color = old[mirror, y];
					}
					else {
						color = old[(u32)i, y];
					}

					ch[0] += gauss[(i32)(i - x + half)] * color.R;
					ch[1] += gauss[(i32)(i - x + half)] * color.G;
					ch[2] += gauss[(i32)(i - x + half)] * color.B;
					ch[3] += gauss[(i32)(i - x + half)] * color.A;
				}

				destination[downXIndex++, y] = new RGBA((u8)ch[0], (u8)ch[1], (u8)ch[2], (u8)ch[3]);
				ch.Clear();
			}
		}
	}

	private static void GaussDownSizeY(UMem2D<RGBA> old, UMem2D<RGBA> destination) {
		f32 ratio = old.Scale.Y / (f32)destination.Scale.Y;
		u32 oddRatio = (u32)f32.Round(old.Scale.Y / destination.Scale.Y);

		oddRatio = (oddRatio & 1) == 0 ? ++oddRatio : oddRatio;
		i32 half = (i32)oddRatio / 2;

		Span<f32> ch = stackalloc f32[4];
		Span<f32> gauss = stackalloc f32[(i32)oddRatio];

		ch.Clear();
		gauss.Create1DGaussianKernel(range: (i32)oddRatio, distribution: oddRatio);

		for(u32 x = 0; x < old.Scale.X; ++x) {
			u32 downYIndex = 0;

			for (f32 y = 0; y < old.Scale.Y && !y.IsCloseTo(to: old.Scale.Y, threshold: .5f); y += ratio) {
				RGBA color = 0x0;

				for(i32 i = (i32)y - half; i <= y + half; ++i) {

					if(i < 0) {
						color = old[x, (u32)i32.Abs(i)];
					}
					else if(i >= old.Scale.Y) {
						u32 mirror = (u32)(old.Scale.Y - i % (old.Scale.Y - 1));
						color = old[x, mirror];
					}
					else {
						color = old[x, (u32)i];
					}

					ch[0] += gauss[(i32)(i - y + half)] * color.R;
					ch[1] += gauss[(i32)(i - y + half)] * color.G;
					ch[2] += gauss[(i32)(i - y + half)] * color.B;
					ch[3] += gauss[(i32)(i - y + half)] * color.A;
				}

				destination[x, downYIndex++] = new RGBA((u8)ch[0], (u8)ch[1], (u8)ch[2], (u8)ch[3]);
				ch.Clear();
			}
		}
	}

    #endregion
}

public enum FlipDirection: u8 {
	VERTICAL,
	HORIZONTAL
}

public enum UpScaleMethod: u8 {
	NEAREST,
	BILINEAR
}
