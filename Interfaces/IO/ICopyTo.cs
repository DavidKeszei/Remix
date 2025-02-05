using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix;

internal interface ICopyTo<TTo> where TTo: allows ref struct {
    public void CopyTo(TTo to);
}
