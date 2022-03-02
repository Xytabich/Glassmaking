using OpenTK;
using System;
using System.Collections.Generic;

namespace GlassMaking.Common
{
	/// <summary>
	/// A graph reflecting the change in value over a certain interval
	/// </summary>
	public struct ValueGraph
	{
		private const double EPSILON = 1e-200;

		public readonly Point[] points;

		public ValueGraph(params Point[] points)
		{
			this.points = points;
		}

		/// <summary>
		/// Calculates the interval during which the target value will be reached.
		/// May return null if it is impossible to reach the value during this graph.
		/// </summary>
		public double? ReachValue(double start, double target, double gain, double loss)
		{
			bool isFound = false;
			if(start > target)
			{
				for(int i = 0; i < points.Length; i++)
				{
					if(points[i].v >= target)
					{
						isFound = true;
						break;
					}
				}
			}
			else
			{
				for(int i = 0; i < points.Length; i++)
				{
					if(points[i].v <= target)
					{
						isFound = true;
						break;
					}
				}
			}
			if(isFound)
			{
				double t = 0;
				for(int i = 0; i < points.Length - 1; i++)
				{
					if(CalcSegmentReachValue(points[i], points[i + 1], ref start, target, gain, loss, out var ti))
					{
						return t + ti;
					}
					t += ti;
				}
			}
			return null;
		}

		/// <summary>
		/// Calculates the value that will be at the end of the graph.
		/// The initial value is set by the <paramref name="start"/> parameter, and how much the value will change by the <paramref name="gain"/> and <paramref name="loss"/> parameters.
		/// </summary>
		public double CalculateFinalValue(double start, double gain, double loss)
		{
			for(int i = 0; i < points.Length - 1; i++)
			{
				start = CalcSegmentFinalValue(points[i], points[i + 1], start, gain, loss);
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
					start = CalcSegmentFinalValue(prevPoint, points[i], start, gain, loss);
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

		public void MultiplyValue(double multiplier)
		{
			for(int i = points.Length - 1; i >= 0; i--)
			{
				points[i].v *= multiplier;
			}
		}

		private bool TryFindStartPoint(double tOffset, out int nextIndex, out Point point)
		{
			if(MathHelper.ApproximatelyEqualEpsilon(points[0].t, tOffset, EPSILON))
			{
				point = points[0];
				nextIndex = 1;
				return true;
			}
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
				point = new Point(Interpolate(points[nextIndex - 1], points[nextIndex], tOffset), tOffset);
				return true;
			}

			point = default;
			return false;
		}

		/// <summary>
		/// Calculates the average graph
		/// </summary>
		public static ValueGraph Avg(params ValueGraph[] graphs)
		{
			int[] indices = new int[graphs.Length];
			var points = new List<Point>();
			double tMin = double.PositiveInfinity;
			for(int i = graphs.Length - 1; i >= 0; i--)
			{
				tMin = Math.Min(graphs[i].points[0].t, tMin);
			}
			double mul = 1.0 / graphs.Length;
			while(true)
			{
				double value = 0;
				for(int i = graphs.Length - 1; i >= 0; i--)
				{
					int index = indices[i];
					var graphPoints = graphs[i].points;
					if(index < graphPoints.Length && MathHelper.ApproximatelyEqualEpsilon(graphPoints[index].t, tMin, EPSILON))
					{
						index++;
						indices[i] = index;
					}
					if(index == 0)
					{
						value += graphPoints[index].v;
					}
					else if(index >= graphPoints.Length)
					{
						value += graphPoints[graphPoints.Length - 1].v;
					}
					else
					{
						value += Interpolate(graphPoints[index - 1], graphPoints[index], tMin);
					}
				}
				value *= mul;
				points.Add(new Point(tMin, value));
				tMin = double.PositiveInfinity;
				for(int i = graphs.Length - 1; i >= 0; i--)
				{
					int index = indices[i];
					var graphPoints = graphs[i].points;
					if(index < graphPoints.Length)
					{
						tMin = Math.Min(tMin, graphPoints[index].t);
					}
				}
				if(double.IsInfinity(tMin)) break;
			}
			return new ValueGraph(points.ToArray());
		}

		private static double CalcSegmentFinalValue(Point a, Point b, double start, double gain, double loss)
		{
			if(MathHelper.ApproximatelyEqualEpsilon(a.t, b.t, EPSILON)) return start;

			if(MathHelper.ApproximatelyEqualEpsilon(a.v, start, EPSILON))
			{
				if(MathHelper.ApproximatelyEqualEpsilon(a.v, b.v, EPSILON))
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
				double t = b.t - a.t;
				double vd = (b.v - a.v) / t;
				double d = a.v > start ? gain : -loss;

				if(MathHelper.ApproximatelyEqualEpsilon(vd, d, EPSILON))
				{
					return start + d * t;
				}

				double it = (start - a.v) / (vd - d);
				if(it <= 0 || it > t)
				{
					return start + d * t;
				}
				double iv = vd * it + a.v;
				return CalcSegmentFinalValue(new Point(it + a.t, iv), b, iv, gain, loss);
			}
		}

		private static bool CalcSegmentReachValue(Point a, Point b, ref double value, double target, double gain, double loss, out double ti)
		{
			if(MathHelper.ApproximatelyEqualEpsilon(a.t, b.t, EPSILON))
			{
				ti = 0;
				return MathHelper.ApproximatelyEqualEpsilon(value, target, EPSILON);
			}

			if(MathHelper.ApproximatelyEqualEpsilon(a.v, value, EPSILON))
			{
				if(MathHelper.ApproximatelyEqualEpsilon(a.v, b.v, EPSILON))
				{
					ti = b.t - a.t;
					return false;
				}
				else
				{
					double t = b.t - a.t;
					double vd = (b.v - a.v) / t;
					double d = a.v > b.v ? -loss : gain;
					if(Math.Abs(vd) < Math.Abs(d)) d = vd;
					return TryReachValue(ref value, target, d, t, out ti);
				}
			}
			else
			{
				double t = b.t - a.t;
				double vd = (b.v - a.v) / t;
				double d = a.v > value ? gain : -loss;

				if(MathHelper.ApproximatelyEqualEpsilon(vd, d, EPSILON))
				{
					return TryReachValue(ref value, target, d, t, out ti);
				}

				double it = (value - a.v) / (vd - d);
				if(it <= 0 || it > t)
				{
					return TryReachValue(ref value, target, d, t, out ti);
				}
				if(TryReachValue(ref value, target, d, it, out ti))
				{
					return true;
				}
				bool isReached = CalcSegmentReachValue(new Point(it + a.t, value), b, ref value, target, gain, loss, out it);
				ti += it;
				return isReached;
			}
		}

		private static bool TryReachValue(ref double value, double target, double d, double t, out double ti)
		{
			double td = target - value;
			if((d < 0) == (td < 0) && Math.Abs(td) <= Math.Abs(d * t))
			{
				value = target;
				ti = td / d;
				return true;
			}
			value += d * t;
			ti = t;
			return false;
		}

		private static double Interpolate(Point a, Point b, double t)
		{
			return a.v + (t - a.t) / (b.t - a.t) * (b.v - a.v);
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