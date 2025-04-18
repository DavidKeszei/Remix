using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Effect;

/// <summary>
/// Provides methods for manipulate buffer of an <see cref="Image"/> like object.
/// </summary>
public abstract class Effect: IEffect {
    private string _name = null!;
    protected float _strength = 1f;

    /// <summary>
    /// Name of the effect.
    /// </summary>
    public string Name { get => _name; }

    /// <summary>
    /// Strength of the effect on the <see cref="Image"/>.
    /// </summary>
    /// <remarks><b>Remark</b>: This property is can't be applied to the inverse effect.</remarks>
    public float Strength { get => _strength; set => _strength = value; }

    protected Effect(string name) => this._name = name;

	/// <summary>
	/// Apply the effect(s) on the <paramref name="target"/>.
	/// </summary>
	/// <param name="target">Target object, which child of the <see cref="Image"/>.</param>
	public abstract Task Apply(Image target);
}
