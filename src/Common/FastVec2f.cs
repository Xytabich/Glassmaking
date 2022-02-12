using System;
using System.IO;
using System.Runtime.CompilerServices;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
	/// <summary>
	/// Represents a vector of 2 floats. Go bug Tyron of you need more utility methods in this class.
	/// </summary>
	public struct FastVec2f
	{
		/// <summary>
		/// The X-Component of the vector
		/// </summary>
		public float X;
		/// <summary>
		/// The Y-Component of the vector
		/// </summary>
		public float Y;


		/// <summary>
		/// Create a new vector with given coordinates
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public FastVec2f(float x, float y)
		{
			this.X = x;
			this.Y = y;
		}

		/// <summary>
		/// Create a new vector with given coordinates
		/// </summary>
		/// <param name="values"></param>
		public FastVec2f(float[] values)
		{
			this.X = values[0];
			this.Y = values[1];
		}

		/// <summary>
		/// Returns the n-th coordinate
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public float this[int index]
		{
			get { return index == 0 ? X : Y; }
			set { if(index == 0) X = value; else Y = value; }
		}


		/// <summary>
		/// Returns the length of this vector
		/// </summary>
		/// <returns></returns>
		public float Length()
		{
			return (float)Math.Sqrt(X * X + Y * Y);
		}

		public void Negate()
		{
			this.X = -X;
			this.Y = -Y;
		}



		/// <summary>
		/// Returns the dot product with given vector
		/// </summary>
		/// <param name="a"></param>
		/// <returns></returns>
		public float Dot(Vec2f a)
		{
			return X * a.X + Y * a.Y;
		}

		/// <summary>
		/// Returns the dot product with given vector
		/// </summary>
		/// <param name="a"></param>
		/// <returns></returns>
		public float Dot(Vec2d a)
		{
			return (float)(X * a.X + Y * a.Y);
		}

		/// <summary>
		/// Returns the dot product with given vector
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public double Dot(float[] pos)
		{
			return X * pos[0] + Y * pos[1];
		}

		/// <summary>
		/// Returns the dot product with given vector
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public double Dot(double[] pos)
		{
			return (float)(X * pos[0] + Y * pos[1]);
		}

		public double[] ToDoubleArray()
		{
			return new double[] { X, Y };
		}

		/// <summary>
		/// Adds given x/y coordinates to the vector
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public FastVec2f Add(float x, float y)
		{
			this.X += x;
			this.Y += y;
			return this;
		}

		/// <summary>
		/// Multiplies each coordinate with given multiplier
		/// </summary>
		/// <param name="multiplier"></param>
		/// <returns></returns>
		public FastVec2f Mul(float multiplier)
		{
			this.X *= multiplier;
			this.Y *= multiplier;
			return this;
		}

		/// <summary>
		/// Creates a copy of the vetor
		/// </summary>
		/// <returns></returns>
		public FastVec2f Clone()
		{
			return (FastVec2f)MemberwiseClone();
		}

		/// <summary>
		/// Turns the vector into a unit vector with length 1, but only if length is non-zero
		/// </summary>
		/// <returns></returns>
		public FastVec2f Normalize()
		{
			float length = Length();

			if(length > 0)
			{
				X /= length;
				Y /= length;
			}

			return this;
		}

		/// <summary>
		/// Calculates the distance the two endpoints
		/// </summary>
		/// <param name="vec"></param>
		/// <returns></returns>
		public float Distance(FastVec2f vec)
		{
			return (float)Math.Sqrt(
				(X - vec.X) * (X - vec.X) +
				(Y - vec.Y) * (Y - vec.Y)
			);
		}


		/// <summary>
		/// Calculates the square distance the two endpoints
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public double DistanceSq(double x, double y)
		{
			return
				(X - x) * (X - x) +
				(Y - y) * (Y - y)
			;
		}


		/// <summary>
		/// Calculates the distance the two endpoints
		/// </summary>
		/// <param name="vec"></param>
		/// <returns></returns>
		public float Distance(Vec2d vec)
		{
			return (float)Math.Sqrt(
				(X - vec.X) * (X - vec.X) +
				(Y - vec.Y) * (Y - vec.Y)
			);
		}

		/// <summary>
		/// Adds given coordinates to a new vectors and returns it. The original calling vector remains unchanged
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public FastVec2f AddCopy(float x, float y)
		{
			return new FastVec2f(X + x, Y + y);
		}

		/// <summary>
		/// Adds both vectors into a new vector. Both source vectors remain unchanged.
		/// </summary>
		/// <param name="vec"></param>
		/// <returns></returns>
		public FastVec2f AddCopy(FastVec2f vec)
		{
			return new FastVec2f(X + vec.X, Y + vec.Y);
		}


		/// <summary>
		/// Substracts val from each coordinate if the coordinate if positive, otherwise it is added. If 0, the value is unchanged. The value must be a positive number
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public void ReduceBy(float val)
		{
			X = X > 0f ? Math.Max(0f, X - val) : Math.Min(0f, X + val);
			Y = Y > 0f ? Math.Max(0f, Y - val) : Math.Min(0f, Y + val);
		}

		/// <summary>
		/// Creates a new vectors that is the normalized version of this vector. 
		/// </summary>
		/// <returns></returns>
		public FastVec2f NormalizedCopy()
		{
			float length = Length();
			return new FastVec2f(
				  X / length,
				  Y / length
			);
		}

		/// <summary>
		/// Creates a new double precision vector with the same coordinates
		/// </summary>
		/// <returns></returns>
		public Vec2d ToVec2d()
		{
			return new Vec2d(X, Y);
		}

		/// <summary>
		/// Sets the vector to this coordinates
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public FastVec2f Set(float x, float y)
		{
			this.X = x;
			this.Y = y;
			return this;
		}

		/// <summary>
		/// Sets the vector to the coordinates of given vector
		/// </summary>
		/// <param name="vec"></param>
		public FastVec2f Set(Vec2d vec)
		{
			this.X = (float)vec.X;
			this.Y = (float)vec.Y;
			return this;
		}

		public FastVec2f Set(float[] vec)
		{
			this.X = vec[0];
			this.Y = vec[1];
			return this;
		}

		/// <summary>
		/// Sets the vector to the coordinates of given vector
		/// </summary>
		/// <param name="vec"></param>
		public void Set(FastVec2f vec)
		{
			this.X = (float)vec.X;
			this.Y = (float)vec.Y;
		}

		/// <summary>
		/// Simple string represenation of the x/y components
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "x=" + X + ", y=" + Y;
		}


		public void Write(BinaryWriter writer)
		{
			writer.Write(X);
			writer.Write(Y);
		}

		public static FastVec2f CreateFromBytes(BinaryReader reader)
		{
			return new FastVec2f(reader.ReadSingle(), reader.ReadSingle());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FastVec2f Lerp(FastVec2f from, FastVec2f to, float t)
		{
			return new FastVec2f(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);
		}
	}
}