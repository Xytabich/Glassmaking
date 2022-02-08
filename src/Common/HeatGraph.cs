﻿using System;
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

        public double CalcTemperatureHoldTime(double timeOffset, float temperature)
        {
            if(timeOffset >= totalTime) return 0;
            if(startTemperature < temperature && workingTemperature < temperature) return 0;
            if(startTemperature > temperature && endTemperature > temperature) return totalTime;

            double time = 0;
            if(workingTemperature < temperature)
            {
                if(transitionTime <= timeOffset) return 0;
                time += Math.Max(0, transitionTime * (startTemperature - temperature) / (startTemperature - workingTemperature) - timeOffset);
            }
            else
            {
                if(transitionTime > timeOffset)
                {
                    if(startTemperature >= temperature)
                    {
                        time += (transitionTime - timeOffset);
                    }
                    else
                    {
                        time = transitionTime * (workingTemperature - temperature) / (workingTemperature - startTemperature);
                        time = Math.Min(transitionTime - timeOffset, time);
                    }
                }
                timeOffset = Math.Max(0, timeOffset - transitionTime);
                if(holdTime > timeOffset)
                {
                    time += (holdTime - timeOffset);
                }
                timeOffset = Math.Max(0, timeOffset - holdTime);
                if(coolingTime > timeOffset)
                {
                    if(endTemperature >= temperature)
                    {
                        time += (coolingTime - timeOffset);
                    }
                    else
                    {
                        time += Math.Max(0, coolingTime * (workingTemperature - temperature) / (workingTemperature - endTemperature) - timeOffset);
                    }
                }
                if(endTemperature >= temperature)
                {
                    timeOffset = Math.Max(0, timeOffset - coolingTime);
                    time += totalTime - transitionTime - holdTime - coolingTime - timeOffset;
                }
            }
            return time;
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

        public double? CalcHeatingTime(float startTemperature, float tempPerHour, float targetTemperature)
        {
            if(startTemperature >= targetTemperature) return 0;
            if(workingTemperature < targetTemperature || this.startTemperature < targetTemperature) return null;
            var t = (targetTemperature - startTemperature) / tempPerHour;
            if(t > totalTime) return null;
            if(GetTemperature(t) < targetTemperature) return null;
            return t;
        }

        public float CalcFinalTemperature(float currentTemperature, float increasePerHour, float decreasePerHour)
        {
            if(transitionTime > 0)
            {
                double delta = (workingTemperature - startTemperature) / transitionTime;
                double change = startTemperature > currentTemperature ? increasePerHour : -decreasePerHour;
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
            if(currentTemperature > endTemperature)
            {
                var coldTime = totalTime - transitionTime - holdTime - coolingTime;
                if(coldTime > 0)
                {
                    currentTemperature = (float)Math.Max(endTemperature, currentTemperature - coldTime * decreasePerHour);
                }
            }
            return currentTemperature;
        }

        public HeatGraph MultiplyTemperature(float multiplier)
        {
            HeatGraph graph = this;
            graph.startTemperature *= multiplier;
            graph.workingTemperature *= multiplier;
            graph.endTemperature *= multiplier;
            return graph;
        }
    }
}