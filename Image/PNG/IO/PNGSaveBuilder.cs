using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Remix.IO;

namespace Remix;

/// <summary>
/// Helps to create save configuration for a <see cref="PNG"/> instance.
/// </summary>
public sealed class PNGSaveBuilder: IDisposable {
    private string _name = string.Empty;
    private string _directory = string.Empty;

    private readonly List<PNGChunk> _chunks = null!;
    private readonly List<PNGChunk> _preChunks = null!;

    /// <summary>
    /// Combined output of the <see cref="PNG"/> image.
    /// </summary>
    internal string OutputPath { get => Path.Combine(_directory, $"{_name}.png"); }

    internal PNGSaveBuilder() {
        _chunks = new List<PNGChunk>();
        _preChunks = new List<PNGChunk>(capacity: 4);
    }

    /// <summary>
    /// Add name to the <see cref="PNG"/> image on the storage.
    /// </summary>
    /// <param name="name">Name of the output.</param>
    public PNGSaveBuilder AddName(string name) {
        this._name = name;
        return this;
    }

    /// <summary>
    /// Add a directory, where the output image is can be found.
    /// </summary>
    /// <param name="dir">Output directory of the <see cref="PNG"/>.</param>
    public PNGSaveBuilder AddOutputDir(string dir) {
        this._directory = dir;
        return this;
    }

    /// <summary>
    /// Add textual data to the <see cref="PNG"/> image.
    /// </summary>
    /// <param name="text">Text inside of the <see cref="PNG"/> image.</param>
    public PNGSaveBuilder AddText(TextKeyword keyword, string text) {
        _chunks.Add(new TextChunk(keyword, text, false));
        return this;
    }

    /// <summary>
    /// Save a <see cref="PNG"/> image with indexed palette with specific color <paramref name="count"/>.
    /// </summary>
    /// <param name="count">Capacity of the color palette.</param>
    /// <returns>Return the builder itself.</returns>
    public PNGSaveBuilder AsIndexed(u32 count) {
        _preChunks.Add(item: new PNGPalette(capacity: count));
        return this;
    }

    public void Dispose() {
        for (i32 i = 0; i < _chunks.Count; ++i)
            _chunks[i].Dispose();

        for (i32 i = 0; i < _preChunks.Count; ++i)
            _preChunks[i].Dispose();
    }

    /// <summary>
    /// Build and save the <see cref="PNG"/> image with the specific properties.
    /// </summary>
    /// <param name="writer">Output stream of the <see cref="PNG"/> file.</param>
    /// <param name="isPreBuild">Indicates which chunks written to the <paramref name="writer"/>, based on position from the IDAT chunk(s).</param>
    internal void Build(PNGWriter writer, bool isPreBuild) {
        IEnumerable<PNGChunk> source = isPreBuild ? _preChunks : _chunks;

        foreach (PNGChunk chunk in source)
            writer.WriteChunk(chunk: chunk);
    }

    /// <summary>
    /// Get the underlying chunk from the builder.
    /// </summary>
    /// <typeparam name="TChunk">Type of the chunk.</typeparam>
    /// <param name="chunk">Returning reference of the chunk.</param>
    /// <returns>If the chunk is exists, return <see langword="true"/>. Otherwise return <see langword="false"/>.</returns>
    internal bool TryGetChunk<TChunk>(out TChunk chunk) where TChunk: PNGChunk {
        foreach (PNGChunk _chunk in _preChunks) {

            if (_chunk as TChunk != null) {
                chunk = (TChunk)_chunk;
                return true;
            }
        }

        chunk = null!;
        return false;
    }
}
