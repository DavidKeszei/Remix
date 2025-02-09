using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix;

/// <summary>
/// Indicates color modes of a <see cref="PNG"/> image.
/// </summary>
public enum PNGColorMode : u8 {
    /// <summary>
    /// In this case the <see cref="PNG"/> image only has 1 channel.
    /// </summary>
    GRAYSCALE = 0,
    /// <summary>
    /// In this case the <see cref="PNG"/> image only has 3 channel.
    /// </summary>
    TRUECOLOR = 2,
    /// <summary>
    /// In this case the colors of the <see cref="PNG"/> image is encoded in the PLTE chunk.
    /// </summary>
    INDEXED = 3,
    /// <summary>
    /// In this case the <see cref="PNG"/> image only has 2 channel.
    /// </summary>
    GRAYSCALE_WITH_ALPHA = 4,
    /// <summary>
    /// In this case the <see cref="PNG"/> image only has 4 channel.
    /// </summary>
    TRUECOLOR_WITH_ALPHA = 6
}
