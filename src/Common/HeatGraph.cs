using System;
using Vintagestory.API.MathTools;

namespace GlassMaking.Common
{
	public struct HeatGraph
	{
		/// <summary>
		/// Transition time to working temperature
		/// </summary>
		public double TransitionTime;
		/// <summary>
		/// Working temperature holding time
		/// </summary>
		public double HoldTime;
		public double CoolingTime;
		/// <summary>
		/// The total time of the graph, the value may be greater than the sum of the time of all states
		/// </summary>
		public double TotalTime;

		public float StartTemperature;
		public float WorkingTemperature;
		public float EndTemperature;

		public double CalcTemperatureHoldTime(double timeOffset, float temperature)
		{
			if(timeOffset >= TotalTime) return 0;
			if(StartTemperature < temperature && WorkingTemperature < temperature) return 0;
			if(StartTemperature > temperature && EndTemperature > temperature) return TotalTime;

			double time = 0;
			if(WorkingTemperature < temperature)
			{
				if(TransitionTime <= timeOffset) return 0;
				time += Math.Max(0, TransitionTime * (StartTemperature - temperature) / (StartTemperature - WorkingTemperature) - timeOffset);
			}
			else
			{
				if(TransitionTime > timeOffset)
				{
					if(StartTemperature >= temperature)
					{
						time += (TransitionTime - timeOffset);
					}
					else
					{
						time = TransitionTime * (WorkingTemperature - temperature) / (WorkingTemperature - StartTemperature);
						time = Math.Min(TransitionTime - timeOffset, time);
					}
				}
				timeOffset = Math.Max(0, timeOffset - TransitionTime);
				if(HoldTime > timeOffset)
				{
					time += (HoldTime - timeOffset);
				}
				timeOffset = Math.Max(0, timeOffset - HoldTime);
				if(CoolingTime > timeOffset)
				{
					if(EndTemperature >= temperature)
					{
						time += (CoolingTime - timeOffset);
					}
					else
					{
						time += Math.Max(0, CoolingTime * (WorkingTemperature - temperature) / (WorkingTemperature - EndTemperature) - timeOffset);
					}
				}
				if(EndTemperature >= temperature)
				{
					timeOffset = Math.Max(0, timeOffset - CoolingTime);
					time += TotalTime - TransitionTime - HoldTime - CoolingTime - timeOffset;
				}
			}
			return time;
		}

		public float GetTemperature(double timeOffset)
		{
			if(TransitionTime > 0 && timeOffset <= TransitionTime)
			{
				return GameMath.Lerp(StartTemperature, WorkingTemperature, (float)(timeOffset / TransitionTime));
			}
			if(HoldTime > 0 && (timeOffset - TransitionTime) <= HoldTime)
			{
				return WorkingTemperature;
			}
			if(CoolingTime > 0)
			{
				return GameMath.Lerp(WorkingTemperature, EndTemperature, (float)((timeOffset - (TransitionTime + HoldTime)) / CoolingTime));
			}
			return EndTemperature;
		}

		public double? CalcHeatingTime(float startTemperature, float tempPerHour, float targetTemperature)
		{
			if(startTemperature >= targetTemperature) return 0;
			if(WorkingTemperature < targetTemperature || StartTemperature < targetTemperature) return null;
			var t = (targetTemperature - startTemperature) / tempPerHour;
			if(t > TotalTime) return null;
			if(GetTemperature(t) < targetTemperature) return null;
			return t;
		}

		public float CalcFinalTemperature(float currentTemperature, float increasePerHour, float decreasePerHour)
		{
			if(TransitionTime > 0)
			{
				double delta = (WorkingTemperature - StartTemperature) / TransitionTime;
				double change = StartTemperature > currentTemperature ? increasePerHour : -decreasePerHour;
				double t = (StartTemperature - currentTemperature) / (change - delta);
				if(t > TransitionTime) t = TransitionTime;
				currentTemperature += (float)(change * t);
				if(t < TransitionTime)
				{
					change = delta < 0 ? -decreasePerHour : increasePerHour;
					if(Math.Abs(change) >= Math.Abs(delta))
					{
						currentTemperature = WorkingTemperature;
					}
					else
					{
						currentTemperature += (float)((TransitionTime - t) * change);
					}
				}
			}
			if(HoldTime > 0)
			{
				double change = WorkingTemperature < currentTemperature ? -decreasePerHour : increasePerHour;
				double t = (WorkingTemperature - currentTemperature) / change;
				if(t > HoldTime)
				{
					currentTemperature += (float)(HoldTime * change);
				}
				else
				{
					currentTemperature = WorkingTemperature;
				}
			}
			if(CoolingTime > 0)
			{
				double delta = (WorkingTemperature - EndTemperature) / CoolingTime;
				double t = 0;
				if(currentTemperature < WorkingTemperature)
				{
					t = (WorkingTemperature - currentTemperature) / (increasePerHour + delta);
					if(t > CoolingTime) t = CoolingTime;
					currentTemperature += (float)(increasePerHour * t);
				}
				if(t < CoolingTime)
				{
					if(decreasePerHour > delta)
					{
						currentTemperature = EndTemperature;
					}
					else
					{
						currentTemperature += (float)((CoolingTime - t) * -decreasePerHour);
					}
				}
			}
			if(currentTemperature > EndTemperature)
			{
				var coldTime = TotalTime - TransitionTime - HoldTime - CoolingTime;
				if(coldTime > 0)
				{
					currentTemperature = (float)Math.Max(EndTemperature, currentTemperature - coldTime * decreasePerHour);
				}
			}
			return currentTemperature;
		}

		public HeatGraph MultiplyTemperature(float multiplier)
		{
			HeatGraph graph = this;
			graph.StartTemperature *= multiplier;
			graph.WorkingTemperature *= multiplier;
			graph.EndTemperature *= multiplier;
			return graph;
		}
	}
}