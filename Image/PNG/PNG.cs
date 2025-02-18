using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Remix.IO;

namespace Remix;

/// <summary>
/// Represent a Portable Network Graphics (PNG) image in the memory.
/// </summary>
public class PNG: Image, IFile<PNG> {
    private PNGColorMode _colorMode = PNGColorMode.TRUECOLOR_WITH_ALPHA;

    /// <summary>
    /// Indicates how stored a color in the <see cref="PNG"/>.
    /// </summary>
    public PNGColorMode ColorMode { get => _colorMode; set => _colorMode = value; }

    /// <summary>
    /// Maximum possible scale of the <see cref="PNG"/> by the library.
    /// </summary>
    public static (u32 X, u32 Y) MaximumScale { get => (16535U, 16535U); }

    /// <summary>
    /// Create a <see cref="PNG"/> image with specific scale and color.
    /// </summary>
    /// <param name="x">Scale of the PNG in the X axis.</param>
    /// <param name="y">Scale of the PNG in the Y axis.</param>
    /// <param name="color">Background color of the <see cref="PNG"/>.</param>
    /// <param name="mode">Indicates how stored a color in the <see cref="PNG"/>.</param>
    public PNG(u32 x, u32 y, RGBA color, PNGColorMode mode = PNGColorMode.TRUECOLOR_WITH_ALPHA): base(x, y, color)
        => this._bitDepth = 8;

    public PNG(u32 x, u32 y): base(x, y) { }

	/// <summary>
	/// Copy a(n) <see cref="Image"/> to this <see cref="PNG"/> with all generic (and specific) properties.
	/// </summary>
	/// <param name="source">Source image.</param>
    /// <param name="owner">Indicates the current <see cref="PNG"/> image is owning the <paramref name="source"/> buffer.</param>
	/// <exception cref="ArgumentException"/>
	public PNG(Image source, bool owner = false): base(source, owner) {
		if (typeof(PNG) == source.GetType())
			this._colorMode = ((PNG)source)._colorMode;
	}

    public static async Task<PNG> Load(string path) {
        PNG result = null!;

        using (PNGReader reader = new PNGReader(path)) {
            if (!reader.IsPNG()) return null!;

            result = new PNG(
                x: reader.ReadHeaderEntry<u32>(name: PNGHeaderEntry.Scale_X),
                y: reader.ReadHeaderEntry<u32>(name: PNGHeaderEntry.Scale_Y)
            );

            result._colorMode = reader.ReadHeaderEntry<PNGColorMode>(name: PNGHeaderEntry.ColorMode);
            result._bitDepth = reader.ReadHeaderEntry<u8>(name: PNGHeaderEntry.Depth);

            await reader.ReadBuffer(target: result);
        }

        return result;
    }
    
    /// <summary>
    /// Save the current <see cref="PNG"/> image to the storage with "default setting(s)".
    /// </summary>
    /// <param name="path">Path of the output <see cref="PNG"/> image.</param>
    public async Task Save(string path) {
        using(PNGWriter writer = new PNGWriter(path)) {
            writer.WriteHeaderEntry<u32>(entry: PNGHeaderEntry.Scale_X, value: _buffer.Scale.X);
            writer.WriteHeaderEntry<u32>(PNGHeaderEntry.Scale_Y, value: _buffer.Scale.Y);

            writer.WriteHeaderEntry<u8>(entry: PNGHeaderEntry.Depth, value: _bitDepth);
            writer.WriteHeaderEntry<PNGColorMode>(entry: PNGHeaderEntry.ColorMode, value: _colorMode);

            await writer.WriteBuffer(from: this);
        }
    }

    /// <summary>
    /// Save the <see cref="PNG"/> to storage with very specific options.
    /// </summary>
    /// <param name="builderAction">Save action, which describes the save method.</param>
    public async Task Save(Action<PNGSaveBuilder> builderAction) {
        PNGSaveBuilder builder = new PNGSaveBuilder();
        builderAction.Invoke(obj: builder);

        using (PNGWriter writer = new PNGWriter(path: builder.OutputPath)) {
            writer.WriteHeaderEntry<u32>(entry: PNGHeaderEntry.Scale_X, value: _buffer.Scale.X);
            writer.WriteHeaderEntry<u32>(PNGHeaderEntry.Scale_Y, value: _buffer.Scale.Y);

            writer.WriteHeaderEntry<u8>(entry: PNGHeaderEntry.Depth, value: _bitDepth);
            writer.WriteHeaderEntry<PNGColorMode>(entry: PNGHeaderEntry.ColorMode, value: _colorMode);

            await writer.WriteBuffer(from: this);

            /* Write not required chunks into the file. (Example: textual data [iTXt Chunk]) */
            builder.Build(writer);
        }
    }
}
