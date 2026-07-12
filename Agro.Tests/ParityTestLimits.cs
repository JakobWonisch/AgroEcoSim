namespace Agro.Tests;

/// <summary>
/// Shared window for legacy vs node parity tests. Increase once the current window is green.
/// </summary>
static class ParityTestLimits
{
	/// <summary>Hours simulated in parity comparisons (legacy vs node).</summary>
	public const int MaxHours = 10;

	/// <summary>Next expansion target after <see cref="MaxHours"/> matches.</summary>
	public const int NextExpansionHours = 12;
}
