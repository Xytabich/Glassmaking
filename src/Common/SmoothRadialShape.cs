using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
	public class SmoothRadialShape
	{
		[JsonProperty(Required = Required.Always)]
		public int Segments;
		[JsonProperty, JsonConverter(typeof(ShapePartConverter))]
		public ShapePart[] Inner = null;
		[JsonProperty(Required = Required.Always), JsonConverter(typeof(ShapePartConverter))]
		public ShapePart[] Outer;

		public void ToBytes(BinaryWriter writer)
		{
			writer.Write(Segments);
			writer.Write(Outer.Length);
			for(int i = 0; i < Outer.Length; i++)
			{
				Outer[i].ToBytes(writer);
			}
			if(Inner == null)
			{
				writer.Write(0);
			}
			else
			{
				writer.Write(Inner.Length);
				for(int i = 0; i < Inner.Length; i++)
				{
					Inner[i].ToBytes(writer);
				}
			}
		}

		public void FromBytes(BinaryReader reader)
		{
			Segments = reader.ReadInt32();
			Outer = new ShapePart[reader.ReadInt32()];
			for(int i = 0; i < Outer.Length; i++)
			{
				Outer[i] = new ShapePart();
				Outer[i].FromBytes(reader);
			}
			int innerCount = reader.ReadInt32();
			if(innerCount == 0) Inner = null;
			else
			{
				Inner = new ShapePart[innerCount];
				for(int i = 0; i < innerCount; i++)
				{
					Inner[i] = new ShapePart();
					Inner[i].FromBytes(reader);
				}
			}
		}

		public SmoothRadialShape Clone()
		{
			return new SmoothRadialShape()
			{
				Segments = Segments,
				Outer = Array.ConvertAll(Outer, x => x.Clone()),
				Inner = Inner == null ? null : Array.ConvertAll(Inner, x => x.Clone())
			};
		}

		public static void BuildMesh(MeshData mesh, SmoothRadialShape shape, Func<MeshData, FastVec2f, bool, int> vecCallback, Action<MeshData, int, int, bool> triCallback)
		{
			if(shape.Segments <= 0) return;

			var tmpList = new FastList<FastVec2f>();

			FastVec2f vec;
			int count, prevCount;
			if(shape.Inner != null)
			{
				prevCount = 0;
				for(int i = 0; i < shape.Segments; i++)
				{
					vec = LerpParts(shape.Inner, tmpList, shape.Segments, i);
					count = vecCallback.Invoke(mesh, vec, false);
					if(i != 0) triCallback.Invoke(mesh, prevCount, count, false);
					prevCount = count;
				}
				vec = LerpParts(shape.Inner, tmpList, shape.Segments, shape.Segments);
				count = vecCallback.Invoke(mesh, vec, false);
				triCallback.Invoke(mesh, prevCount, count, false);
			}

			prevCount = 0;
			for(int i = 0; i < shape.Segments; i++)
			{
				vec = LerpParts(shape.Outer, tmpList, shape.Segments, i);
				count = vecCallback.Invoke(mesh, vec, true);
				if(i != 0) triCallback.Invoke(mesh, prevCount, count, true);
				prevCount = count;
			}
			vec = LerpParts(shape.Outer, tmpList, shape.Segments, shape.Segments);
			count = vecCallback.Invoke(mesh, vec, true);
			triCallback.Invoke(mesh, prevCount, count, true);
		}

		public static void BuildLerpedMesh(MeshData mesh, SmoothRadialShape from, SmoothRadialShape to, SmoothRadialShape defaultShape, float t, Func<MeshData, FastVec2f, bool, int> vecCallback, Action<MeshData, int, int, bool> triCallback)
		{
			int segments = (int)Math.Ceiling(GameMath.Lerp(from.Segments, to.Segments, t));
			if(segments <= 0) return;

			var tmpList = new FastList<FastVec2f>();
			float step = 1f / segments;
			int count, prevCount = 0;

			if(from.Inner != null || to.Inner != null)
			{
				var fs = from.Inner == null ? defaultShape : from;
				var ts = to.Inner == null ? defaultShape : to;
				for(int i = 0; i < segments; i++)
				{
					count = AddLerpedVertex(mesh, tmpList, vecCallback, false, fs, ts, i * step * fs.Segments, i * step * ts.Segments, t);
					if(i != 0) triCallback.Invoke(mesh, prevCount, count, false);
					prevCount = count;
				}
				count = AddLerpedVertex(mesh, tmpList, vecCallback, false, fs, ts, fs.Segments, ts.Segments, t);
				triCallback.Invoke(mesh, prevCount, count, false);
			}

			for(int i = 0; i < segments; i++)
			{
				count = AddLerpedVertex(mesh, tmpList, vecCallback, true, from, to, i * step * from.Segments, i * step * to.Segments, t);
				if(i != 0) triCallback.Invoke(mesh, prevCount, count, true);
				prevCount = count;
			}
			count = AddLerpedVertex(mesh, tmpList, vecCallback, true, from, to, from.Segments, to.Segments, t);
			triCallback.Invoke(mesh, prevCount, count, true);
		}

		private static int AddLerpedVertex(MeshData mesh, FastList<FastVec2f> tmpList, Func<MeshData, FastVec2f, bool, int> vecCallback, bool isOuter, SmoothRadialShape from, SmoothRadialShape to, float at, float bt, float t)
		{
			var a = LerpParts(isOuter ? from.Outer : from.Inner, tmpList, from.Segments, at);
			var b = LerpParts(isOuter ? to.Outer : to.Inner, tmpList, to.Segments, bt);
			return vecCallback.Invoke(mesh, FastVec2f.Lerp(a, b, t), isOuter);
		}

		private static FastVec2f LerpParts(ShapePart[] parts, FastList<FastVec2f> tmpList, float full, float t)
		{
			if(parts == null || parts.Length == 0) return new FastVec2f(0, 0);
			tmpList.Clear();
			if(parts.Length == 1)
			{
				parts[0].Interpolate(tmpList, t / full);
				return tmpList[0];
			}
			else
			{
				for(int i = 0; i < parts.Length; i++)
				{
					if(t <= parts[i].Segments)
					{
						parts[i].Interpolate(tmpList, t / parts[i].Segments);
						return tmpList[0];
					}
					else
					{
						t -= parts[i].Segments;
					}
				}
				parts[parts.Length - 1].Interpolate(tmpList, 1);
				return tmpList[0];
			}
		}

		public class ShapePart
		{
			[JsonProperty(Required = Required.Always)]
			public int Segments;
			[JsonProperty(Required = Required.Always)]
			public float[][] Vertices;

			public void Interpolate(FastList<FastVec2f> tmpList, float t)
			{
				if(Vertices.Length == 0) tmpList.Add(new FastVec2f(0, 0));
				if(Vertices.Length == 1) tmpList.Add(new FastVec2f(Vertices[0]));
				for(int i = 0; i < Vertices.Length; i++)
				{
					tmpList.Add(new FastVec2f(Vertices[i]));
				}
				Utils.InterpolateBezier(tmpList, t);
			}

			public void ToBytes(BinaryWriter writer)
			{
				writer.Write(Segments);
				writer.Write(Vertices.Length);
				for(int i = 0; i < Vertices.Length; i++)
				{
					writer.Write(Vertices[i][0]);
					writer.Write(Vertices[i][1]);
				}
			}

			public void FromBytes(BinaryReader reader)
			{
				Segments = reader.ReadInt32();
				Vertices = new float[reader.ReadInt32()][];
				for(int i = 0; i < Vertices.Length; i++)
				{
					float x = reader.ReadSingle();
					float y = reader.ReadSingle();
					Vertices[i] = new float[2] { x, y };
				}
			}

			public ShapePart Clone()
			{
				return new ShapePart()
				{
					Segments = Segments,
					Vertices = Array.ConvertAll(Vertices, v => new float[] { v[0], v[1] })
				};
			}
		}

		private class ShapePartConverter : JsonConverter
		{
			public override bool CanWrite => false;

			public override bool CanConvert(Type objectType)
			{
				return objectType == typeof(ShapePart[]);
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				if(reader.TokenType == JsonToken.Null) return null;
				var arr = JArray.Load(reader);
				if(arr.Count > 0)
				{
					if(arr[0].Type == JTokenType.Object)
					{
						return arr.ToObject<ShapePart[]>();
					}
					return new ShapePart[] { new ShapePart() { Vertices = arr.ToObject<float[][]>() } };
				}
				return new ShapePart[0];
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				throw new NotImplementedException();
			}
		}
	}
}