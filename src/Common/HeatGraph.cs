namespace GlassMaking.Common
{
    public struct HeatGraph
    {
        /// <summary>
        /// Transition time to working temperature
        /// </summary>
        public double transitionTime;
        /// <summary>
        /// Working temperature holding time
        /// </summary>
        public double holdTime;
        public double coolingTime;
        /// <summary>
        /// The total time of the graph, the value may be greater than the sum of the time of all states
        /// </summary>
        public double totalTime;

        public float startTemperature;
        public float workingTemperature;
        public float endTemperature;

        public double CalcTemperatureHoldTime(double offset, float temperature)
        {
            if(startTemperature < temperature || workingTemperature < temperature) return 0;
            if(workingTemperature < temperature)
            {
                return transitionTime * (1f - (temperature - workingTemperature) / (startTemperature - workingTemperature));
            }
            else
            {
                if(endTemperature >= temperature) return totalTime;
                if(startTemperature < temperature)
                {
                    return coolingTime * (1f - (temperature - endTemperature) / (workingTemperature - endTemperature)) + holdTime + transitionTime * (1f - (temperature - startTemperature) / (workingTemperature - startTemperature));
                }
                return coolingTime * (1f - (temperature - endTemperature) / (workingTemperature - endTemperature)) + holdTime + transitionTime;
            }
        }
    }
}