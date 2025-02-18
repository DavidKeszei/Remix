using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Effect;

/// <summary>
/// Represent a simple pixel shift to vertical or horizontal direction.
/// </summary>
public class Shift: Effect {
	private i32 _seed = -1;
	private u32 _maxShiftValue = 0;

	private u32 _lineHeight = 1;
	private f32 _randomSelectedPercent = 1f;

	private ShiftDirection _direction = ShiftDirection.VERTICAL;

	/// <summary>
	/// Maximum value of the shifting backward or forward.
	/// </summary>
	public u32 MaximumShift { get => _maxShiftValue; set => _maxShiftValue = value; }

	/// <summary>
	/// Initial seed value of the <see cref="Random"/> instance.
	/// </summary>
	/// <remarks><b>Remarks:</b> If seed value is negative, then the seed value is equal to the actual tick value of the <see cref="DateTime.Now"/> mod by <see cref="i32.MaxValue"/>.</remarks>
	public i32 Seed { get => _seed; set => _seed = value; }

	/// <summary>
	/// Direction of the shifting.
	/// </summary>
	public ShiftDirection Direction { get => _direction; set => _direction = value; }

	/// <summary>
	/// Length of the shifting "lines" per iterations.
	/// </summary>
	public u32 LineHeight { get => _lineHeight; set => _lineHeight = value; }

	/// <summary>
	/// Indicates how many line shifted in a direction in normalized percent.
	/// </summary>
	public f32 Threshold { get => _randomSelectedPercent; set => _randomSelectedPercent = f32.Clamp(value, .0f, 1f); }

	public Shift(u32 maxShift, ShiftDirection direction, i32 seed = -1): base(name: nameof(Shift)) {
		this._maxShiftValue = maxShift;
		this._direction = direction;

		this._seed = seed;
	}

	public override Task Apply(Image target) {
		Random rnd = new Random(Seed: _seed == -1 ? (i32)(DateTime.Now.Ticks % i32.MaxValue) : _seed);
		using Image img = new Image(source: target, isOwner: false);

		if (_direction == ShiftDirection.VERTICAL) {
			ShiftVertical(generator: rnd, target, img);
		}
		else {
			ShiftHorizontal(generator: rnd, target, img);
		}

		return Task.CompletedTask;
	}

	private void ShiftHorizontal(Random generator, Image target, Image temp) {
		i32 currentShift = 0;
		bool isCurrentlySelected = true;

		f32 pxStrength = 1f - _strength;

		for (u32 y = 0; y < target.Scale.Y; y++) {
			if (y % _lineHeight == 0) {
				currentShift = generator.Next(minValue: -(i32)_maxShiftValue, maxValue: (i32)(_maxShiftValue + 1));
				isCurrentlySelected = 1f - generator.NextSingle() <= _randomSelectedPercent;
			}

			for (u32 x = 0; x < target.Scale.X; ++x) {
				if (!isCurrentlySelected) {
					target[x, y] = temp[x, y];
					continue;
				}

				if (x + currentShift < 0) {
					target[(u32)(target.Scale.X + ((i32)x + currentShift)), y] = (temp[x, y] * _strength) + (target[(u32)(target.Scale.X + ((i32)x + currentShift)), y] * pxStrength);
				}
				else if(x + currentShift >= target.Scale.X) {
					target[(u32)((x + currentShift) % target.Scale.X), y] = (temp[x, y] * _strength) + (target[(u32)((x + currentShift) % target.Scale.X), y] * pxStrength);
				}
				else {
					target[(u32)(x + currentShift), y] = (temp[x, y] * _strength) + (target[(u32)(x + currentShift), y] * pxStrength);
				}
			}
		}
	}

	private void ShiftVertical(Random generator, Image target, Image temp) {
		i32 currentShift = 0;
		bool isCurrentlySelected = true;

		for (u32 x = 0; x < target.Scale.X; x++) {
			if (x % _lineHeight == 0) {
				currentShift = generator.Next(minValue: -(i32)_maxShiftValue, maxValue: (i32)(_maxShiftValue + 1));
				isCurrentlySelected = 1f - generator.NextSingle() <= _randomSelectedPercent;
			}

			for (u32 y = 0; y < target.Scale.Y; ++y) {
				if (!isCurrentlySelected) {
					target[x, y] = temp[x, y];
					continue;
				}

				if (y + currentShift < 0) {
					target[x, (u32)(target.Scale.Y + ((i32)y + currentShift))] = temp[x, y];
				}
				else if(y + currentShift >= target.Scale.Y) {
					target[x, (u32)((y + currentShift) % target.Scale.Y)] = temp[x, y];
				}
				else {
					target[x, (u32)(y + currentShift)] = temp[x, y];
				}
			}
		}
	}
}

public enum ShiftDirection: u8 {
	VERTICAL,
	HORIZONTAL
}
