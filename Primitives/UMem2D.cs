using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Remix.IO;

namespace Remix;

/// <summary>
/// Represent unmanaged memory in the 2D scape.
/// </summary>
/// <typeparam name="TType">Underlying type of the <see cref="UMem2D{TType}"/>.</typeparam>
public class UMem2D<TType> : IDisposable, ICopyFrom<UMem2D<TType>>, IInvalid<UMem2D<TType>>, 
							 IEquatable<UMem2D<TType>> {

	private readonly UMem<TType> _buffer = UMem<TType>.Invalid;
	private (u32 X, u32 Y) _scale = (X: 0, Y: 0);

	private bool _disposedValue = false;

	/// <summary>
	/// Return a reference from the current <see cref="UMem2D{TType}"/> instance.
	/// </summary>
	/// <param name="x">Position of the reference object in the array in the X axis.</param>
	/// <param name="y">Position of the reference object in the array in the Y axis.</param>
	/// <returns>Return a reference from the array as <typeparamref name="TType"/>.</returns>
	/// <exception cref="IndexOutOfRangeException"/>
	public ref TType this[u32 x, u32 y] {
		get {
			if (x >= _scale.X || y >= _scale.Y)
				throw new IndexOutOfRangeException();

			return ref _buffer[(y * _scale.X) + x];
		}
	}

	/// <summary>
	/// Represent a unallocated/invalid memory in the 2D space.
	/// </summary>
	public static UMem2D<TType> Invalid { get => new UMem2D<TType>(); }

	/// <summary>
	/// Scale of the current <see cref="UMem2D{TType}"/> instance.
	/// </summary>
	public (u32 X, u32 Y) Scale { get => _scale; }

	/// <summary>
	/// Create and allocate new 2D memory with <typeparamref name="TType"/> elements.
	/// </summary>
	/// <param name="scale">Scale of the memory block.</param>
	public UMem2D((u32 X, u32 Y) scale) {
		if (scale.X == 0 || scale.Y == 0)
			throw new ArgumentException(message: $"Scale of the current UMem2D<{nameof(TType)}> can't be equal with (x: 0, y: 0).");

		_buffer = UMem<TType>.Create(allocationLength: scale.X * scale.Y);
		_scale = scale;
	}

	/// <summary>
	/// Create and allocate new 2D memory with <typeparamref name="TType"/> elements.
	/// </summary>
	/// <param name="scale">Scale of the memory block.</param>
	/// <exception cref="ArgumentException"/>
	public UMem2D((u32 X, u32 Y) scale, TType @default) {
		if (scale.X == 0 || scale.Y == 0)
			throw new ArgumentException(message: $"Scale of the current UMem2D<{nameof(TType)}> can't be equal with (x: 0, y: 0).");

		_buffer = UMem<TType>.Create(allocationLength: scale.X * scale.Y, @default);
		_scale = scale;
	}

	private UMem2D() {
		this._buffer = UMem<TType>.Invalid;
		this._scale = (0, 0);
	}

	~UMem2D() => Dispose(disposing: false);

	/// <summary>
	/// Return the current <see cref="UMem2D{TType}"/> instance as a <see cref="UnmanagedMemoryStream"/>.
	/// </summary>
	/// <returns>Return a new <see cref="UnmanagedMemoryStream"/> instance.</returns>
	public UnmanagedMemoryStream AsStream(FileAccess access) => _buffer.AsStream(access);

	public void CopyFrom(UMem2D<TType> from) {
		if (from.Scale.X > this._scale.X || from.Scale.Y > this._scale.Y)
			throw new ArgumentException(
				message: "The destination UMem2D<T> can't hold the buffer from the source, because the scale smaller than the source length. " +
						 $"(Current scale: {_scale}, Source/Copy buffer: {from.Scale})");

		for (u32 y = 0; y < from.Scale.Y; ++y) {
			for (u32 x = 0; x < from.Scale.X; ++x) {
				this[x, y] = from[x, y];
			}
		}
	}

	public void Dispose() {
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public bool Equals(UMem2D<TType> other)
		=> other != null && this._disposedValue == other._disposedValue && this._buffer.Equals(other._buffer);

	protected virtual void Dispose(bool disposing) {
		if (!_disposedValue) {
			_buffer.Dispose();

			_scale = (X: 0, Y: 0);
			_disposedValue = true;
		}
	}
}
