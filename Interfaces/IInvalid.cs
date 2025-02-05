using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix;

/// <summary>
/// Provides a invalid state/value for the implementer.
/// </summary>
/// <typeparam name="TSelf">The type self.</typeparam>
internal interface IInvalid<TSelf> {

	/// <summary>
	/// Indicates the instance of the <typeparamref name="TSelf"/> is invalid, if holds this value.
	/// </summary>
	public abstract static TSelf Invalid { get; }
}
