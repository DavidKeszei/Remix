using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.IO;

/// <summary>
/// Provides simple copy mechanisms from/to a(n) <typeparamref name="TFrom"/> source/destination. 
/// </summary>
/// <typeparam name="TFrom">Type of the source.</typeparam>
internal interface ICopyFrom<TFrom> where TFrom : allows ref struct {
    public void CopyFrom(TFrom from);
}
