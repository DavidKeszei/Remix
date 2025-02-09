using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Remix.IO;

namespace Remix;

/// <summary>
/// Helping creating a save configuration for a <see cref="PNG"/> instance.
/// </summary>
public sealed class PNGSaveBuilder: IDisposable {
    private string _name = string.Empty;
    private string _directory = string.Empty;

    private List<PNGChunk> _chunks = null!;

    /// <summary>
    /// Combined output of the <see cref="PNG"/> image.
    /// </summary>
    internal string OutputPath { get => Path.Combine(_directory, $"{_name}.png"); }

    internal PNGSaveBuilder() 
        => _chunks = new List<PNGChunk>();

    /// <summary>
    /// Add name to the <see cref="PNG"/> image on the storage.
    /// </summary>
    /// <param name="name">Name of the output.</param>
    public PNGSaveBuilder AddName(string name) {
        this._name = name;
        return this;
    }

    /// <summary>
    /// Add a directory, where the output is founded.
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

    public void Dispose() {
        for (i32 i = 0; i < _chunks.Count; ++i)
            _chunks[i].Dispose();
    }

    /// <summary>
    /// Build and save the <see cref="PNG"/> image with the specific properties.
    /// </summary>
    /// <param name="writer">Output stream of the <see cref="PNG"/> file.</param>
    internal void Build(PNGWriter writer) {
        foreach (PNGChunk chunk in _chunks)
            writer.WriteChunk(chunk: chunk);
    }
}
