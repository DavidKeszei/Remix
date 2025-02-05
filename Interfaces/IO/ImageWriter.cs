using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.IO;

/// <summary>
/// Provides method for writing a <see cref="Image"/> to a fle format.
/// </summary>
/// <typeparam name="T">Type of the <see cref="Image"/>.</typeparam>
internal interface IImageWriter<T> where T: Image {

    /// <summary>
    /// Write to the <typeparamref name="T"/> buffer to the physical storage.
    /// </summary>
    /// <param name="from">Owner of the <see cref="RGBA"/> buffer.</param>
    public Task WriteBuffer(T from);
}
