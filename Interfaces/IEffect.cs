using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Effect;

/// <summary>
/// Provides methods for manipulate an <see cref="Image"/> independently the instance self.
/// </summary>
public interface IEffect {

	/// <summary>
	/// Name of the <see cref="IEffect"/>.
	/// </summary>
	public string Name { get; }


	/// <summary>
	/// Apply the effect(s) on the <paramref name="target"/>.
	/// </summary>
	/// <param name="target">Target object, which child of the <see cref="Image"/>.</param>
	public Task Apply(Image target);
}
