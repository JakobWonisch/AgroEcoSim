namespace Agro.BehaviorGraph;

public readonly struct WireValue
{
	public readonly bool HasValue;
	readonly bool isBool;
	readonly bool boolValue;
	readonly float floatValue;

	WireValue(bool has, bool isB, bool b, float f)
	{
		HasValue = has;
		isBool = isB;
		boolValue = b;
		floatValue = f;
	}

	public static WireValue Missing => new(false, false, false, 0f);

	public static WireValue OfBool(bool b) => new(true, true, b, 0f);

	public static WireValue OfFloat(float f) => new(true, false, false, f);

	public bool TryGetBool(out bool b)
	{
		if (!HasValue)
		{
			b = false;
			return false;
		}

		if (isBool)
		{
			b = boolValue;
			return true;
		}

		b = floatValue != 0f;
		return true;
	}

	public bool TryGetFloat(out float f)
	{
		if (!HasValue)
		{
			f = 0f;
			return false;
		}

		if (isBool)
		{
			f = boolValue ? 1f : 0f;
			return true;
		}

		f = floatValue;
		return true;
	}

	public bool AsBool() => TryGetBool(out var b) && b;

	public float AsFloat() => TryGetFloat(out var f) ? f : 0f;
}
