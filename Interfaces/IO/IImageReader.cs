using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.IO;

internal interface IImageReader<T> where T : Image {
    public Task ReadBuffer(T target);
}
