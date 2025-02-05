using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix;

/// <summary>
/// Provides simple load and save mechanism for <see cref="Image"/> files.
/// </summary>
/// <typeparam name="TImage">The implementer class, which is child of the <see cref="Image"/> class.</typeparam>
public interface IFile<TImage> where TImage: Image {

    /// <summary>
    /// Load a(n) <typeparamref name="TImage"/> from the instance.
    /// </summary>
    /// <param name="path">Path of the image.</param>
    /// <returns>Return a loaded image as <typeparamref name="TImage"/>.</returns>
    public static abstract Task<TImage> Load(string path);

    /// <summary>
    /// Save the <typeparamref name="TImage"/> to the storage.
    /// </summary>
    /// <param name="path">Output path of the image.</param>
    public Task Save(string path);
}
