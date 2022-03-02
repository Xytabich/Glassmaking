namespace GlassMaking.Blocks
{
	public interface IHeatSourceModifier
	{
		float FuelRateModifier { get; }

		float TemperatureModifier { get; }
	}
}