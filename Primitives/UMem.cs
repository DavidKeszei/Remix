using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Remix;

/// <summary>
/// Represent a C-like allocated memory as unmanaged memory.
/// </summary>
/// <typeparam name="TType">Underlying type of array.</typeparam>
internal unsafe struct UMem<TType> : IDisposable, IEquatable<UMem<TType>>, IInvalid<UMem<TType>> {
    private readonly void* _ptr = null!;
    private u64 _length = 0;

    /// <summary>
    /// Length of the current <see cref="UMem{TType}"/> instance.
    /// </summary>
    public u64 Length { get => _length; }

    /// <summary>
    /// Represent a unallocated memory with zero length.
    /// </summary>
    public static UMem<TType> Invalid { get => new UMem<TType>(source: null!, length: 0); }

    /// <summary>
    /// Return a reference from the current <see cref="UMem{TType}"/> instance.
    /// </summary>
    /// <param name="index">Position of the reference object in the array.</param>
    /// <returns>Return a reference from the array as <typeparamref name="TType"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"/>
    public ref TType this[u64 index] {
        get {
            if (index >= _length)
                throw new IndexOutOfRangeException(message: $"The {nameof(index)} parameter must be larger than 0 and must be less than {_length}.");

            return ref *(((TType*)_ptr) + index);
        }
    }

	public UMem(void* source, u64 length) {
        this._ptr = source;
        this._length = length;
    }

    /// <summary>
    /// Create a new <see cref="UMem{TType}"/> instance with specific <paramref name="allocationLength"/>.
    /// </summary>
    /// <param name="allocationLength">Length of the allocation. (Not in bytes)</param>
    /// <returns>Return a new <see cref="UMem{TType}"/> instance.</returns>
    public static UMem<TType> Create(u64 allocationLength) {
        if (allocationLength == 0)
            return UMem<TType>.Invalid;
        
        return new UMem<TType>(
				source: NativeMemory.Alloc((nuint)allocationLength, elementSize: (nuint)Unsafe.SizeOf<TType>()),
				allocationLength
		);
	}

    /// <summary>
    /// Create a new <see cref="UMem{TType}"/> instance with specific <paramref name="allocationLength"/> and default value.
    /// </summary>
    /// <param name="allocationLength">Length of the allocation. (Not in bytes)</param>
    /// <returns>Return a new <see cref="UMem{TType}"/> instance.</returns>
    public static UMem<TType> Create(u64 allocationLength, TType @default) {
        UMem<TType> mem = new UMem<TType>(
                                source: NativeMemory.Alloc((nuint)allocationLength, elementSize: (nuint)Unsafe.SizeOf<TType>()),
                                allocationLength
                          );

        for (u64 i = 0; i < mem._length; ++i)
            mem[i] = @default;

        return mem;
    }

    public void Clear(TType fill = default!) {
        for(u64 i = 0; i < _length; ++i) {
            *((TType*)_ptr + i) = fill;
        }
    }

    /// <summary>
    /// Return the current <see cref="UMem{TType}"/> instance as a <see cref="UnmanagedMemoryStream"/>.
    /// </summary>
    /// <returns>Return a new <see cref="UnmanagedMemoryStream"/> instance.</returns>
    public UnmanagedMemoryStream AsStream(FileAccess access) => new UnmanagedMemoryStream(
        pointer: (byte*)_ptr,
        length: (i64)_length * Unsafe.SizeOf<TType>(),
        capacity: (i64)_length * Unsafe.SizeOf<TType>(),
        access: access
    );

    public Span<TType> AsSpan(u64 from, i32 length) {
        if (from + (u32)length > _length)
            length = i32.Clamp((i32)_length - (i32)from, 0, (i32)_length);

        return new Span<TType>((TType*)_ptr + from, length);
    }

    public bool Equals(UMem<TType> other)
        => other.Length == this._length && other._ptr == this._ptr;

    public void Dispose() {
        if(_length > 0) {
            NativeMemory.Free(ptr: _ptr);
            _length = 0;
        }
    }
}