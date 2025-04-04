using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Remix.IO;

namespace Remix;

/// <summary>
/// Represent an <see cref="Image"/> instance in the memory. 
/// </summary>
public class Image: IDisposable {
	protected UMem2D<RGBA> _buffer = UMem2D<RGBA>.Invalid;
	protected u8 _bitDepth = 0;

	private bool _disposedValue = false;

	/// <summary>
	/// Get/Set pixel based on the <paramref name="x"/> and the <paramref name="y"/> parameters.
	/// </summary>
	/// <param name="x">X axis coordinate.</param>
	/// <param name="y">Y axis coordinate.</param>
	/// <returns>Return a <see cref="RGBA"/> instance.</returns>
	public ref RGBA this[u32 x, u32 y] { get => ref _buffer[x, y]; }

	/// <summary>
	/// Scale of the <see cref="Image"/>.
	/// </summary>
	public (u32 X, u32 Y) Scale { get => _buffer.Scale; }

	/// <summary>
	/// Depth of the channels in the <see cref="Image"/>.
	/// </summary>
	public u8 BitDepth { get => _bitDepth; }

	/// <summary>
	/// Create new <see cref="Image"/> with specific scale and background <paramref name="color"/>.
	/// </summary>
	/// <param name="x">Size of the <see cref="Image"/> in the X axis.</param>
	/// <param name="y">Size of the <see cref="Image"/> in the Y axis.</param>
	/// <param name="color">Background color of the <see cref="Image"/>.</param>
	public Image(u32 x, u32 y, RGBA color) {
		_buffer = new UMem2D<RGBA>(scale: (x, y), color);
		_bitDepth = _bitDepth == 0 ? (u8)8 : _bitDepth;
	}

	/// <summary>
	/// Create an <see cref="Image"/> from the <paramref name="source"/> image.
	/// </summary>
	/// <param name="source">Other image as source of this image.</param>
	public Image(Image source, bool isOwner = false) {
		if (!isOwner) {
			this._buffer = new UMem2D<RGBA>(scale: source.Scale);
			this._buffer.CopyFrom(from: source._buffer);
		}
		else {
			/* Grab the 'source' buffer & give that to the current image. */
			this._buffer = source._buffer;
			source._buffer = UMem2D<RGBA>.Invalid;
		}

		this._bitDepth = source._bitDepth;
	}

	/// <summary>
	/// Create a new <see cref="Image"/> with specific scale.
	/// </summary>
	/// <param name="x">Scale in the X axis.</param>
	/// <param name="y">Scale in the Y axis.</param>
	public Image(u32 x, u32 y) {
		this._bitDepth = 8;
		this._buffer = new UMem2D<RGBA>(scale: (x, y));
	}

	~Image() => Dispose(disposing: false);

	/// <summary>
	/// Swap the underlying buffer with a new buffer after initialize the <see cref="Image"/> instance.
	/// </summary>
	/// <param name="source">The new buffer, which owned by the current <see cref="Image"/> by now.</param>
	/// <exception cref="InvalidOperationException"/>
	public void SwapBuffer(UMem2D<RGBA> source) {
		if (this._buffer.Equals(other: UMem2D<RGBA>.Invalid))
			throw new InvalidOperationException(message: "You can't swap the underlying buffer, if the image is not created or loaded into the memory.");

		this._buffer.Dispose();
		this._buffer = source;
	}

	public void Dispose() {
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing) {
		if (!_disposedValue) {
			if (disposing)
				_bitDepth = 0;
				
			_buffer.Dispose();
			_disposedValue = true;
		}
	}
}
