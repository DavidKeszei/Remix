using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix;

public static class Transform {

	/// <summary>
	/// Flip the current <paramref name="image"/> in an axis.
	/// </summary>
	/// <param name="image">Target of the flip.</param>
	/// <param name="direction">Flip direction.</param>
	public static void Flip(Image image, FlipDirection direction) {
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
	public static void Resize(Image source, u32 x, u32 y, UpScaleMethod method = UpScaleMethod.BILINEAR) {
		if (source.Scale.X == x && source.Scale.Y == y) 
			return;

		using UMem2D<RGBA> horizontal = new UMem2D<RGBA>((x, source.Scale.Y), 0x0);
		UMem2D<RGBA> vertical = new UMem2D<RGBA>((x, y), 0x0);

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

		for(u32 y = 0; y < destination.Scale.Y; ++y) {
			f32 barrier = ratio;
			f32 barrierBefore = .0f;

			RGBA from = old[0, y];
			RGBA to = old.Scale.X == 1 ? old[0, y] : old[1, y];

			for (u32 x = 0; x < destination.Scale.X; ++x) {

				if(x >= barrier && (barrier / ratio) < old.Scale.X - 1) {
					barrierBefore = barrier;
					barrier += ratio;

					from = to;
					to = old[(u32)(barrier / ratio), y];
				}

				f32 current = (x - barrierBefore) / ratio;
				destination[x, y] = RGBA.Lerp(from, to, current);
			}
		}
	}

	private static void BilinearY(UMem2D<RGBA> old, UMem2D<RGBA> destination) {
		f32 ratio = old.Scale.Y == 1 ? 1 :
						old.Scale.Y <= 2 ? destination.Scale.Y - 1 :
										   (destination.Scale.Y - 1) / (f32)old.Scale.Y;

		for(u32 x = 0; x < destination.Scale.X; ++x) {
			f32 barrier = ratio;
			f32 barrierBefore = .0f;

			RGBA from = old[x, 0];
			RGBA to = old.Scale.Y == 1 ? old[x, 0] : old[x, 1];

			for (u32 y = 0; y < destination.Scale.Y; ++y) {
				if(y >= barrier && (barrier / ratio) < old.Scale.Y - 1) {

					barrierBefore = barrier;
					barrier += ratio;

					from = to;
					to = old[x, (u32)(barrier / ratio)];
				}

				f32 current = (y - barrierBefore) / ratio;
				destination[x, y] = RGBA.Lerp(from, to, current);
			}
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
						u32 mirror = (u32)(old.Scale.X - (i % (old.Scale.X - 1)));
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
						u32 mirror = (u32)(old.Scale.Y - (i % (old.Scale.Y - 1)));
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
}

public enum FlipDirection: u8 {
	VERTICAL,
	HORIZONTAL
}

public enum UpScaleMethod: u8 {
	NEAREST,
	BILINEAR
}
