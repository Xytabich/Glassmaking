namespace GlassMaking.Blocks
{
	public interface ITimeBasedHeatReceiver
	{
		void SetHeatSource(ITimeBasedHeatSourceControl heatSource);
	}
}