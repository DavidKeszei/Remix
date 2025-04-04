using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Effect;

/// <summary>
/// Represent a reusable collection of <see cref="IEffect"/> instances.
/// </summary>
public class EffectPreset: IEffect {
	private readonly List<IEffect> _effects = null!;
	private string _name = string.Empty;

	/// <summary>
	/// Name of the <see cref="EffectPreset"/> instance.
	/// </summary>
	public string Name { get => _name; set => _name = value; }

	/// <summary>
	/// Count of the <see cref="IEffect"/> instances in the pipeline.
	/// </summary>
	public u32 Count { get => (u32)_effects.Count; }

	/// <summary>
	/// Resolve effect from the pipeline.
	/// </summary>
	/// <param name="index">Position of the <see cref="IEffect"/> instance.</param>
	/// <returns>Return an <see cref="IEffect"/> instance.</returns>
	/// <exception cref="IndexOutOfRangeException"/>
	public IEffect this[u32 index] {
		get => _effects[(i32)index];
		set => _effects[(i32)index] = value;
	}

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

	/// <summary>
	/// Remove the specific <see cref="IEffect"/> instance at the given <paramref name="index"/>.
	/// </summary>
	/// <param name="index">Position of the <see cref="IEffect"/> instance.</param>
	/// <exception cref="IndexOutOfRangeException"/>
	public void Remove(u32 index)
		=> this._effects.RemoveAt(index: (i32)index);

	/// <summary>
	/// Get the specific <see cref="IEffect"/> as <typeparamref name="TEffect"/>. 
	/// </summary>
	/// <typeparam name="TEffect">Type of the given <see cref="IEffect"/>.</typeparam>
	/// <param name="index">Position of the <see cref="IEffect"/> instance.</param>
	/// <returns>
	/// Return an <see cref="IEffect"/> instance. If the instance index is invalid or the instance is NULL or type of the instance is not correct, 
	/// then return <see langword="null"/>.
	///</returns>
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
		if(_effects.Count == 0)
			return;

		foreach (IEffect effect in _effects)
			await effect.Apply(target);
	}
}
