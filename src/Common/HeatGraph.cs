using System;
using Vintagestory.API.MathTools;

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

        public double CalcTemperatureHoldTime(double timeOffset, float temperature)//TODO: time offset
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

        public float GetTemperature(double timeOffset)
        {
            if(transitionTime > 0 && timeOffset <= transitionTime)
            {
                return GameMath.Lerp(startTemperature, workingTemperature, (float)(timeOffset / transitionTime));
            }
            if(holdTime > 0 && (timeOffset - transitionTime) <= holdTime)
            {
                return workingTemperature;
            }
            if(coolingTime > 0)
            {
                return GameMath.Lerp(workingTemperature, endTemperature, (float)((timeOffset - (transitionTime + holdTime)) / coolingTime));
            }
            return endTemperature;
        }

        public double CalcHeatingTime(float startTemperature, float tempPerHour, float targetTemperature)
        {
            if(workingTemperature < targetTemperature || this.startTemperature < targetTemperature) return 0;
            var t = (targetTemperature - startTemperature) / tempPerHour;
            if(GetTemperature(t) < targetTemperature) return 0;
            return t;
        }

        public float CalcFinalTemperature(float currentTemperature, float increasePerHour, float decreasePerHour)
        {
            if(transitionTime > 0)
            {
                double delta = (workingTemperature - startTemperature) / transitionTime;
                double change = startTemperature > currentTemperature ? -decreasePerHour : increasePerHour;
                double t = (startTemperature - currentTemperature) / (change - delta);
                if(t > transitionTime) t = transitionTime;
                currentTemperature += (float)(change * t);
                if(t < transitionTime)
                {
                    change = delta < 0 ? -decreasePerHour : increasePerHour;
                    if(Math.Abs(change) >= Math.Abs(delta))
                    {
                        currentTemperature = workingTemperature;
                    }
                    else
                    {
                        currentTemperature += (float)((transitionTime - t) * change);
                    }
                }
            }
            if(holdTime > 0)
            {
                double change = workingTemperature < currentTemperature ? -decreasePerHour : increasePerHour;
                double t = (workingTemperature - currentTemperature) / change;
                if(t > holdTime)
                {
                    currentTemperature += (float)(holdTime * change);
                }
                else
                {
                    currentTemperature = workingTemperature;
                }
            }
            if(coolingTime > 0)
            {
                double delta = (workingTemperature - endTemperature) / coolingTime;
                double t = 0;
                if(currentTemperature < workingTemperature)
                {
                    t = (workingTemperature - currentTemperature) / (increasePerHour + delta);
                    if(t > coolingTime) t = coolingTime;
                    currentTemperature += (float)(increasePerHour * t);
                }
                if(t < coolingTime)
                {
                    if(decreasePerHour > delta)
                    {
                        currentTemperature = endTemperature;
                    }
                    else
                    {
                        currentTemperature += (float)((coolingTime - t) * -decreasePerHour);
                    }
                }
            }
            return currentTemperature;
        }
    }
}