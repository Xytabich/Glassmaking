using OpenTK;
using System;

namespace GlassMaking.Common
{
	/// <summary>
	/// A graph reflecting the change in value over a certain interval
	/// </summary>
	public struct ValueGraph
	{
		public readonly Point[] points;

		public ValueGraph(params Point[] points)
		{
			this.points = points;
		}

		/// <summary>
		/// Calculates the value that will be at the end of the graph.
		/// The initial value is set by the <paramref name="start"/> parameter, and how much the value will change by the <paramref name="gain"/> and <paramref name="loss"/> parameters.
		/// </summary>
		public double CalculateFinalValue(double start, double gain, double loss)
		{
			for(int i = 0; i < points.Length - 1; i++)
			{
				start = CalcSegmentEndValue(points[i], points[i + 1], start, gain, loss);
			}
			return start;
		}

		/// <summary>
		/// Calculates the value that will be at the end of the graph.
		/// The initial value is set by the <paramref name="start"/> parameter, and how much the value will change by the <paramref name="gain"/> and <paramref name="loss"/> parameters.
		/// </summary>
		/// <param name="tOffset">The offset of the <see cref="Point.t"/> value from which the calculation starts</param>
		public double CalculateFinalValue(double tOffset, double start, double gain, double loss)
		{
			if(TryFindStartPoint(tOffset, out var index, out var prevPoint))
			{
				for(int i = index; i < points.Length; i++)
				{
					start = CalcSegmentEndValue(prevPoint, points[i], start, gain, loss);
					prevPoint = points[i];
				}
			}
			return start;
		}

		/// <summary>
		/// Calculates the total interval of the value <see cref="Point.t"/> during which the value <see cref="Point.v"/> was greater than or equal to the specified <paramref name="value"/>
		/// </summary>
		public double CalculateValueRetention(double value)
		{
			double t = 0;
			for(int i = 0; i < points.Length - 1; i++)
			{
				t += CalcRetention(points[i], points[i + 1], value);
			}
			return t;
		}

		/// <summary>
		/// Calculates the total interval of the value <see cref="Point.t"/> during which the value <see cref="Point.v"/> was greater than or equal to the specified <paramref name="value"/>
		/// </summary>
		/// <param name="tOffset">The offset of the <see cref="Point.t"/> value from which the calculation starts</param>
		public double CalculateValueRetention(double tOffset, double value)
		{
			double t = 0;
			if(TryFindStartPoint(tOffset, out var index, out var prevPoint))
			{
				for(int i = index; i < points.Length; i++)
				{
					t += CalcRetention(prevPoint, points[i], value);
					prevPoint = points[i];
				}
			}
			return t;
		}

		private bool TryFindStartPoint(double tOffset, out int nextIndex, out Point point)
		{
			nextIndex = -1;
			for(int i = points.Length - 1; i > 0; i--)
			{
				if(tOffset < points[i].t)
				{
					nextIndex = i;
					break;
				}
			}
			if(nextIndex >= 0)
			{
				point = Interpolate(points[nextIndex - 1], points[nextIndex], tOffset);
				return true;
			}

			point = default;
			return false;
		}

		private static double CalcSegmentEndValue(Point a, Point b, double start, double gain, double loss)
		{
			if(MathHelper.ApproximatelyEqualEpsilon(a.t, b.t, float.Epsilon)) return start;

			if(MathHelper.ApproximatelyEqualEpsilon(a.v, start, float.Epsilon))
			{
				if(MathHelper.ApproximatelyEqualEpsilon(a.v, b.v, float.Epsilon))
				{
					return b.v;
				}
				else
				{
					double t = b.t - a.t;
					double d = a.v > b.v ? -loss : gain;
					if(Math.Abs((b.v - a.v) / t) <= Math.Abs(d))
					{
						return b.v;
					}
					else
					{
						return start + d * t;
					}
				}
			}
			else
			{
				double v = b.v - a.v;
				double t = b.t - a.t;
				double d = a.v > start ? gain : -loss;

				if(MathHelper.ApproximatelyEqualEpsilon(v, d, float.Epsilon))
				{
					return start + d * t;
				}

				double it = (start - a.v) / (v - d);
				if(it <= 0 || it > t)
				{
					return start + d * t;
				}
				double iv = v * it + a.v;
				return CalcSegmentEndValue(new Point(it + a.t, iv), b, iv, gain, loss);
			}
		}

		private static Point Interpolate(Point a, Point b, double t)
		{
			return new Point(t, a.v + (t - a.t) / (b.t - a.t) * (b.v - a.v));
		}

		private static double CalcRetention(Point a, Point b, double value)
		{
			if(a.v >= value)
			{
				if(b.v >= value) return b.t - a.t;
				return (value - a.v) / (b.v - a.v) * (b.t - a.t);
			}
			else
			{
				if(b.v < value) return 0;
				return (1 - (value - a.v) / (b.v - a.v)) * (b.t - a.t);
			}
		}

		public struct Point
		{
			/// <summary>
			/// X-axis
			/// </summary>
			public double t;
			/// <summary>
			/// Y-axis
			/// </summary>
			public double v;

			public Point(double t, double v)
			{
				this.t = t;
				this.v = v;
			}
		}
	}
}