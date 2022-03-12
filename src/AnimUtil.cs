using System;

namespace GlassMaking
{
	public class AnimUtil
	{
		public static float Reach(float current, float to)
		{
			return Math.Sign(to) * Math.Min(Math.Abs(to), current);
		}

		public static float Reach(float current, float from, float to)
		{
			return from + Reach(current, to - from);
		}

		public static float Tri(float startValue, float midValue, float endValue, float midTime, float t)
		{
			if(t < midTime) return startValue + t / midTime * (midValue - startValue);
			return midValue + (t - midTime) / (1 - midTime) * (endValue - midValue);
		}

		public static float Tri(float midPoint, float t)
		{
			if(t < midPoint) return t / midPoint;
			return (t - midPoint) / (1 - midPoint);
		}

		public static float Quad(float p1, float p2, float p3, float p4, float p2Time, float p3Time, float t)
		{
			if(t < p2Time) return p1 + t / p2Time * (p2 - p1);
			if(t < p3Time) return p2 + (t - p2Time) / (p3Time - p2Time) * (p3 - p2);
			return p3 + (t - p3Time) / (1 - p3Time) * (p4 - p3);
		}
	}
}