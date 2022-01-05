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
        public int segments;
        [JsonProperty, JsonConverter(typeof(ShapePartConverter))]
        public ShapePart[] inner = null;
        [JsonProperty(Required = Required.Always), JsonConverter(typeof(ShapePartConverter))]
        public ShapePart[] outer;

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(segments);
            writer.Write(outer.Length);
            for(int i = 0; i < outer.Length; i++)
            {
                outer[i].ToBytes(writer);
            }
            if(inner == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(inner.Length);
                for(int i = 0; i < inner.Length; i++)
                {
                    inner[i].ToBytes(writer);
                }
            }
        }

        public void FromBytes(BinaryReader reader)
        {
            segments = reader.ReadInt32();
            outer = new ShapePart[reader.ReadInt32()];
            for(int i = 0; i < outer.Length; i++)
            {
                outer[i] = new ShapePart();
                outer[i].FromBytes(reader);
            }
            int innerCount = reader.ReadInt32();
            if(innerCount == 0) inner = null;
            else
            {
                inner = new ShapePart[innerCount];
                for(int i = 0; i < innerCount; i++)
                {
                    inner[i] = new ShapePart();
                    inner[i].FromBytes(reader);
                }
            }
        }

        public SmoothRadialShape Clone()
        {
            return new SmoothRadialShape() {
                segments = segments,
                outer = Array.ConvertAll(outer, x => x.Clone()),
                inner = inner == null ? null : Array.ConvertAll(inner, x => x.Clone())
            };
        }

        public static void BuildMesh(MeshData mesh, SmoothRadialShape shape, Func<MeshData, FastVec2f, bool, int> vecCallback, Action<MeshData, int, int, bool> triCallback)
        {
            if(shape.segments <= 0) return;

            var tmpList = new FastList<FastVec2f>();

            FastVec2f vec;
            int count, prevCount = 0;
            for(int i = 0; i < shape.segments; i++)
            {
                vec = LerpParts(shape.outer, tmpList, shape.segments, i);
                count = vecCallback.Invoke(mesh, vec, true);
                if(i != 0) triCallback.Invoke(mesh, prevCount, count, true);
                prevCount = count;
            }
            vec = LerpParts(shape.outer, tmpList, shape.segments, shape.segments);
            count = vecCallback.Invoke(mesh, vec, true);
            triCallback.Invoke(mesh, prevCount, count, true);

            if(shape.inner != null)
            {
                for(int i = 0; i < shape.segments; i++)
                {
                    vec = LerpParts(shape.inner, tmpList, shape.segments, i);
                    count = vecCallback.Invoke(mesh, vec, false);
                    if(i != 0) triCallback.Invoke(mesh, prevCount, count, false);
                    prevCount = count;
                }
                vec = LerpParts(shape.inner, tmpList, shape.segments, shape.segments);
                count = vecCallback.Invoke(mesh, vec, false);
                triCallback.Invoke(mesh, prevCount, count, false);
            }
        }

        public static void BuildLerpedMesh(MeshData mesh, SmoothRadialShape from, SmoothRadialShape to, float t, Func<MeshData, FastVec2f, bool, int> vecCallback, Action<MeshData, int, int, bool> triCallback)
        {
            int segments = (int)Math.Ceiling(GameMath.Lerp(from.segments, to.segments, t));
            if(segments <= 0) return;

            var tmpList = new FastList<FastVec2f>();
            float fromStep = (float)from.segments / segments;
            float toStep = (float)to.segments / segments;
            int count, prevCount = 0;
            for(int i = 0; i < segments; i++)
            {
                count = AddLerpedVertex(mesh, tmpList, vecCallback, true, from, to, i * fromStep, i * toStep, t);
                if(i != 0) triCallback.Invoke(mesh, prevCount, count, true);
                prevCount = count;
            }
            count = AddLerpedVertex(mesh, tmpList, vecCallback, true, from, to, from.segments, to.segments, t);
            triCallback.Invoke(mesh, prevCount, count, true);

            if(from.inner != null && to.inner != null)
            {
                for(int i = 0; i < segments; i++)
                {
                    count = AddLerpedVertex(mesh, tmpList, vecCallback, false, from, to, i * fromStep, i * toStep, t);
                    if(i != 0) triCallback.Invoke(mesh, prevCount, count, false);
                    prevCount = count;
                }
                count = AddLerpedVertex(mesh, tmpList, vecCallback, false, from, to, from.segments, to.segments, t);
                triCallback.Invoke(mesh, prevCount, count, false);
            }
        }

        private static int AddLerpedVertex(MeshData mesh, FastList<FastVec2f> tmpList, Func<MeshData, FastVec2f, bool, int> vecCallback, bool isOuter, SmoothRadialShape from, SmoothRadialShape to, float at, float bt, float t)
        {
            var a = LerpParts(isOuter ? from.outer : from.inner, tmpList, from.segments, at);
            var b = LerpParts(isOuter ? to.outer : to.inner, tmpList, to.segments, bt);
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
                    if(t <= parts[i].segments)
                    {
                        parts[0].Interpolate(tmpList, t / parts[i].segments);
                        return tmpList[0];
                    }
                    else
                    {
                        t -= parts[i].segments;
                    }
                }
                parts[parts.Length - 1].Interpolate(tmpList, 1);
                return tmpList[0];
            }
        }

        public class ShapePart
        {
            [JsonProperty(Required = Required.Always)]
            public int segments;
            [JsonProperty(Required = Required.Always)]
            public float[][] vertices;

            public void Interpolate(FastList<FastVec2f> tmpList, float t)
            {
                if(vertices.Length == 0) tmpList.Add(new FastVec2f(0, 0));
                if(vertices.Length == 1) tmpList.Add(new FastVec2f(vertices[0]));
                for(int i = 0; i < vertices.Length; i++)
                {
                    tmpList.Add(new FastVec2f(vertices[i]));
                }
                Utils.InterpolateBezier(tmpList, t);
            }

            public void ToBytes(BinaryWriter writer)
            {
                writer.Write(segments);
                writer.Write(vertices.Length);
                for(int i = 0; i < vertices.Length; i++)
                {
                    writer.Write(vertices[i][0]);
                    writer.Write(vertices[i][1]);
                }
            }

            public void FromBytes(BinaryReader reader)
            {
                segments = reader.ReadInt32();
                vertices = new float[reader.ReadInt32()][];
                for(int i = 0; i < vertices.Length; i++)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    vertices[i] = new float[2] { x, y };
                }
            }

            public ShapePart Clone()
            {
                return new ShapePart() {
                    segments = segments,
                    vertices = Array.ConvertAll(vertices, v => new float[] { v[0], v[1] })
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
                    return new ShapePart[] { new ShapePart() { vertices = arr.ToObject<float[][]>() } };
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