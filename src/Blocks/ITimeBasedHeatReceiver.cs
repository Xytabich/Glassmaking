namespace GlassMaking.Blocks
{
	public interface ITimeBasedHeatReceiver
	{
		void SetHeatSource(ITimeBasedHeatSource heatSource);

		void OnHeatSourceTick(float dt);
	}
}