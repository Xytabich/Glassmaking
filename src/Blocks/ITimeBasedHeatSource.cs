using GlassMaking.Common;

namespace GlassMaking.Blocks
{
	public interface ITimeBasedHeatSource
	{
		/// <summary>
		/// Returns the World.Calendar.TotalHours time set on the previous tick
		/// </summary>
		double GetLastTickTime();

		/// <summary>
		/// Is the source heated to operating temperature
		/// </summary>
		bool IsHeatedUp();

		/// <summary>
		/// Does the fire burn at the source (if the source is powered by fuel)
		/// </summary>
		bool IsBurning();

		/// <summary>
		/// What tempature was calculated on the last tick
		/// </summary>
		float GetTemperature();

		/// <summary>
		/// Calculates the temperature for the calendarTotalHours (uses World.Calendar.TotalHours if the value is 0)
		/// </summary>
		float CalcCurrentTemperature(double calendarTotalHours = 0);

		/// <summary>
		/// Calculates the graph of temperature changes for the interval between the previous tick and the calendarTotalHours (uses World.Calendar.TotalHours if the value is 0)
		/// </summary>
		ValueGraph CalcHeatGraph(double calendarTotalHours = 0);
	}

	public interface ITimeBasedHeatSourceContainer : ITimeBasedHeatSource
	{
		void SetReceiver(ITimeBasedHeatReceiver receiver);
	}
}