using Utils;
using Xunit;

namespace Agro.Tests;

public class PcgTests
{
	[Fact]
	public void NextFloatAccum_FixedUnit0_AlwaysPasses_WhenProbabilityPositive()
	{
		var rng = new Pcg(42) { FixedUnitFloat = 0f };
		var p = 44f / 4032f;
		Assert.True(rng.NextFloatAccum(p * p, 1));
	}

	[Fact]
	public void NextFloatAccum_FixedUnit1_AlwaysFails_WhenProbabilityBelowOne()
	{
		var rng = new Pcg(42) { FixedUnitFloat = 1f };
		Assert.False(rng.NextFloatAccum(0.5f, 1));
	}
}
