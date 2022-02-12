namespace GlassMaking.Blocks
{
	public interface IBurnerModifier
	{
		float DurationModifier { get; }
		float TemperatureModifier { get; }
	}
}