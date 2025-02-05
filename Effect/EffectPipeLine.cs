using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Effect;

/// <summary>
/// Represent a reusable collection of <see cref="Effect"/>s.
/// </summary>
public class EffectPreset: IEffect {
	private readonly List<IEffect> _effects = null!;
	private string _name = string.Empty;

	/// <summary>
	/// Name of the <see cref="EffectPreset"/> instance.
	/// </summary>
	public string Name { get => _name; set => _name = value; }

	/// <summary>
	/// Create a new <see cref="EffectPreset"/> with <paramref name="presetName"/>.
	/// </summary>
	/// <param name="presetName">Name of the <see cref="EffectPreset"/>.</param>
	/// <param name="effects">[Optional] Starter effect(s) in the <see cref="EffectPreset"/>.</param>
	public EffectPreset(string presetName, params Effect[] effects) {
		this._effects = new List<IEffect>(collection: effects);
		this._name = presetName;
	}

	/// <summary>
	/// Add new effect to the preset as <typeparamref name="TEffect"/>.
	/// </summary>
	/// <typeparam name="TEffect">Implementer class of the <see cref="IEffect"/>.</typeparam>
	/// <param name="effect">The effect self.</param>
	public void Add<TEffect>(TEffect effect) where TEffect: class, IEffect
		=> _effects.Add(item: effect);

	public TEffect GetEffect<TEffect>(i32 index) where TEffect: class, IEffect {
		if (index > _effects.Count || _effects[index] == null! || typeof(IEffect) != _effects[index].GetType())
			return null!;

		return (TEffect)_effects[index];
	}

	/// <summary>
	/// Apply the underlying effects on the <paramref name="target"/>.
	/// </summary>
	/// <param name="target">The target <see cref="Image"/> instance.</param>
	public async Task Apply(Image target) {
		foreach (Effect effect in _effects)
			await effect.Apply(target);
	}
}
