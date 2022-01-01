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
        /// Calculates the temperature for the current World.Calendar.TotalHours
        /// </summary>
        float CalcCurrentTemperature();

        /// <summary>
        /// Returns the time during which the specified temperature was kept from the previous tick to the current World.Calendar.TotalHours value
        /// </summary>
        double CalcTempElapsedTime(double startTime, float temperature);
    }
}